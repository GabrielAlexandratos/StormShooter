using System;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace StormShooter;

public enum EnemyState
{
    Patrol,
    Alert,
    Engage,
    Reposition,
}

public class EnemyAI
{
    private const float DetectRadius = 200f;
    private const float PreferredRange = 100f;
    private const float RangeDeadzone = 20f;
    private const float OrbitSpeed = 55f;
    private const float PatrolSpeed = 28f;
    private const float AlertSpeed = 50f;
    private const float RepositionSpeed = 60f;
    private const float ShootInterval = 0.9f;
    private const float ShootIntervalRand = 0.6f;
    private const float AccuracySpread = 0.08f;

    // burst fallback values
    private int GunBurstCount => _enemy.Gun.BurstCount > 1 ? _enemy.Gun.BurstCount : 3;
    private float GunBurstDelay => _enemy.Gun.BurstDelay > 0f ? _enemy.Gun.BurstDelay : 0.12f;
    private const float OrbitFlipInterval = 1.4f;
    private const float PatrolWaypointDist = 8f;
    private const float AlertLoseTime = 6f;
    private const float LowHealthThreshold = 2f;

    public EnemyState State { get; private set; } = EnemyState.Patrol;

    private readonly Enemy _enemy;
    private readonly Random _rng;
    private readonly Func<Vector2, bool> _isWall;
    private readonly Func<Vector2, Vector2, bool> _hasLOS;

    private Vector2 _patrolWaypoint;
    private float _patrolWaypointTimer;
    private Vector2 _lastKnownPlayerPos;
    private float _losLostTimer;
    private int _orbitSign = 1;
    private float _orbitFlipTimer;
    private float _shootTimer;
    private int _burstRemaining;
    private float _burstTimer;
    private Vector2 _repositionTarget;
    private float _repositionTimeout;

    // automatic firing gun vars
    private float _autoFireTimeRemaining;
    private float _autoFireShotTimer;
    private const float AutoFireDuration = 0.6f;
    private const float AutoFireDurationRand = 0.1f;
    private const float BetweenBurstPause = 1.2f;

    public float AimAngle { get; private set; }

    public EnemyAI(Enemy enemy, Random rng, Func<Vector2, bool> isWall, Func<Vector2, Vector2, bool> hasLOS)
    {
        _enemy = enemy;
        _rng = rng;
        _isWall = isWall;
        _hasLOS = hasLOS;
        _shootTimer = ShootInterval + (float)(_rng.NextDouble() * ShootIntervalRand);
        _patrolWaypoint = enemy.Position;
    }

    public void Update(float dt, Vector2 playerPos, Action<Vector2, Vector2, float> fireCallback)
    {
        float distToPlayer = Vector2.Distance(_enemy.Position, playerPos);
        bool inRadius = distToPlayer < DetectRadius;
        
        bool hasLOS = inRadius && _hasLOS(_enemy.Position, playerPos);

        if (State != EnemyState.Patrol)
        {
            Vector2 toPlayer = playerPos - _enemy.Position;
            if (toPlayer.LengthSquared() > 0.01f)
                AimAngle = LerpAngle(AimAngle, MathF.Atan2(toPlayer.Y, toPlayer.X), dt * 10f);
        }

        switch (State)
        {
            case EnemyState.Patrol:
                if (hasLOS) { _lastKnownPlayerPos = playerPos; TransitionTo(EnemyState.Alert); }
                break;

            case EnemyState.Alert:
                if (hasLOS) { _lastKnownPlayerPos = playerPos; TransitionTo(EnemyState.Engage); }
                else { _losLostTimer += dt; if (_losLostTimer > AlertLoseTime) TransitionTo(EnemyState.Patrol); }
                break;

            case EnemyState.Engage:
                if (hasLOS) { _lastKnownPlayerPos = playerPos; _losLostTimer = 0f; }
                else { _losLostTimer += dt; if (_losLostTimer > 0.3f) TransitionTo(EnemyState.Reposition); }
                break;

            case EnemyState.Reposition:
                if (hasLOS) TransitionTo(EnemyState.Engage);
                else if (_repositionTimeout <= 0f) TransitionTo(EnemyState.Alert);
                break;
        }

        switch (State)
        {
            case EnemyState.Patrol: UpdatePatrol(dt); break;
            case EnemyState.Alert: UpdateAlert(dt, playerPos); break;
            case EnemyState.Engage: UpdateEngage(dt, playerPos, fireCallback, hasLOS); break;
            case EnemyState.Reposition: UpdateReposition(dt, playerPos, fireCallback, hasLOS); break;
        }
    }

    private void UpdatePatrol(float dt)
    {
        _patrolWaypointTimer -= dt;
        if (Vector2.Distance(_enemy.Position, _patrolWaypoint) < PatrolWaypointDist || _patrolWaypointTimer <= 0f)
            PickNewPatrolWaypoint();
        MoveToward(_patrolWaypoint, PatrolSpeed, dt);
    }

    private void PickNewPatrolWaypoint()
    {
        for (int i = 0; i < 20; i++)
        {
            float angle = (float)(_rng.NextDouble() * MathF.Tau);
            float radius = 32f + (float)(_rng.NextDouble() * 64f);
            Vector2 candidate = _enemy.Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;

            if (!_isWall(candidate) && _hasLOS(_enemy.Position, candidate))
            {
                _patrolWaypoint = candidate;
                _patrolWaypointTimer = 3f + (float)_rng.NextDouble() * 2f;
                return;
            }
        }
        _patrolWaypointTimer = 1.5f;
    }

    private void UpdateAlert(float dt, Vector2 playerPos)
    {
        if (Vector2.Distance(_enemy.Position, _lastKnownPlayerPos) > PatrolWaypointDist)
            MoveToward(_lastKnownPlayerPos, AlertSpeed, dt);
    }

    private void UpdateEngage(float dt, Vector2 playerPos, Action<Vector2, Vector2, float> fireCallback, bool hasLOS)
    {
        float dist = Vector2.Distance(_enemy.Position, playerPos);

        _orbitFlipTimer -= dt;
        if (_orbitFlipTimer <= 0f)
        {
            _orbitSign = _rng.Next(2) == 0 ? 1 : -1;
            _orbitFlipTimer = OrbitFlipInterval + (float)(_rng.NextDouble() * 1.5f);
        }

        Vector2 toPlayer = playerPos - _enemy.Position;
        if (toPlayer.LengthSquared() > 0.001f) toPlayer.Normalize();

        Vector2 tangent = new Vector2(-toPlayer.Y, toPlayer.X) * _orbitSign;
        float radialBias = dist < PreferredRange - RangeDeadzone ? -1f : dist > PreferredRange + RangeDeadzone ? 1f : 0f;

        Vector2 moveDir = tangent + toPlayer * (radialBias * 0.6f);
        if (moveDir.LengthSquared() > 0.001f) moveDir.Normalize();

        MoveWithDir(moveDir, OrbitSpeed * (_enemy.Health <= LowHealthThreshold ? 1.35f : 1f), dt);
        UpdateShooting(dt, playerPos, fireCallback, hasLOS);
    }

    private void UpdateReposition(float dt, Vector2 playerPos, Action<Vector2, Vector2, float> fireCallback, bool hasLOS)
    {
        _repositionTimeout -= dt;
        if (Vector2.Distance(_enemy.Position, _repositionTarget) > PatrolWaypointDist)
            MoveToward(_repositionTarget, RepositionSpeed, dt);
        else
            PickRepositionTarget(playerPos);
        
        bool suppressFire = Vector2.Distance(_enemy.Position, _lastKnownPlayerPos) < DetectRadius * 0.7f;
        UpdateShooting(dt, _lastKnownPlayerPos, fireCallback, suppressFire);
    }

    private void PickRepositionTarget(Vector2 playerPos)
    {
        Vector2 toPlayer = playerPos - _enemy.Position;
        float baseAngle = MathF.Atan2(toPlayer.Y, toPlayer.X);

        for (int i = 0; i < 16; i++)
        {
            float angle = baseAngle + MathF.PI + (float)((_rng.NextDouble() * MathF.PI) - MathF.PI * 0.5f);
            float radius = 70f + (float)(_rng.NextDouble() * 50f);
            Vector2 candidate = playerPos + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;

            if (!_isWall(candidate) && _hasLOS(_enemy.Position, candidate)) 
            { 
                _repositionTarget = candidate; 
                _repositionTimeout = 3f; 
                return; 
            }
        }

        Vector2 away = -toPlayer;
        if (away.LengthSquared() > 0.001f) away.Normalize();
        _repositionTarget = _enemy.Position + away * 60f;
        _repositionTimeout = 2f;
    }

    private void UpdateShooting(float dt, Vector2 playerPos, Action<Vector2, Vector2, float> fireCallback, bool hasLOS)
    {
        if (!hasLOS) return;

        bool aimed = IsAimedAtPlayer(playerPos);

        if (_enemy.Gun.Automatic)
        {
            UpdateAutoShooting(dt, playerPos, fireCallback, aimed);
        }
        else
        {
            UpdateBurstShooting(dt, playerPos, fireCallback, aimed);
        }
    }

    private bool IsAimedAtPlayer(Vector2 playerPos)
    {
        Vector2 toPlayer = playerPos - _enemy.Position;
        float angleToPlayer = MathF.Atan2(toPlayer.Y, toPlayer.X);
        float angleDiff = Math.Abs(((angleToPlayer - AimAngle + MathF.PI * 3) % (MathF.PI * 2)) - MathF.PI);
        return angleDiff < 0.4f;
    }

    private void UpdateAutoShooting(float dt, Vector2 playerPos, Action<Vector2, Vector2, float> fireCallback, bool aimed)
    {
        if (_autoFireTimeRemaining > 0f)
        {
            // Currently holding the trigger
            _autoFireShotTimer -= dt;
            if (_autoFireShotTimer <= 0f && aimed)
            {
                FireOneShot(playerPos, fireCallback);
                float fireInterval = _enemy.Gun.FireRate > 0f ? 1f / _enemy.Gun.FireRate : 0.1f;
                _autoFireShotTimer = fireInterval;
            }

            _autoFireTimeRemaining -= dt;
            return;
        }

        _shootTimer -= dt;
        if (_shootTimer <= 0f)
        {
            float mult = _enemy.Health <= LowHealthThreshold ? 0.65f : 1f;
            float pause = (BetweenBurstPause + (float)((_rng.NextDouble() - 0.5) * ShootIntervalRand)) * mult;
            _shootTimer = pause;
            _autoFireTimeRemaining = AutoFireDuration + (float)(_rng.NextDouble() * AutoFireDurationRand);
            _autoFireShotTimer = 0f; // fire immediately on first frame
        }
    }

    private void UpdateBurstShooting(float dt, Vector2 playerPos, Action<Vector2, Vector2, float> fireCallback, bool aimed)
    {
        if (_burstRemaining > 0)
        {
            _burstTimer -= dt;
            if (_burstTimer <= 0f && aimed)
            {
                FireOneShot(playerPos, fireCallback);
                _burstRemaining--;
                _burstTimer = GunBurstDelay;
            }
            return;
        }

        _shootTimer -= dt;
        if (_shootTimer <= 0f)
        {
            float mult = _enemy.Health <= LowHealthThreshold ? 0.65f : 1f;
            _shootTimer = (ShootInterval + (float)((_rng.NextDouble() - 0.5) * ShootIntervalRand)) * mult;
            _burstRemaining = GunBurstCount + (_enemy.Health <= LowHealthThreshold ? 1 : 0);
            _burstTimer = GunBurstDelay;
        }
    }

    private void FireOneShot(Vector2 playerPos, Action<Vector2, Vector2, float> fireCallback)
    {
        Vector2 dir = new Vector2(MathF.Cos(AimAngle), MathF.Sin(AimAngle));

        Gun gun = _enemy.Gun;
        int pellets = Math.Max(1, gun.BulletsPerShot);
        
        float aimJitter = (_rng.NextSingle() - 0.5f) * AccuracySpread * 2f;

        for (int i = 0; i < pellets; i++)
        {
            float t = pellets == 1 ? 0.5f : (float)i / (pellets - 1);
            float pelletSpread = MathHelper.Lerp(-gun.SpreadAngle / 2f, gun.SpreadAngle / 2f, t)
                               + (_rng.NextSingle() - 0.5f) * gun.SpreadAngle
                               + aimJitter;
            fireCallback?.Invoke(_enemy.Position, dir, pelletSpread);
        }
    }

    private void TransitionTo(EnemyState next)
    {
        if (next == EnemyState.Reposition) PickRepositionTarget(_lastKnownPlayerPos);
        if (next == EnemyState.Patrol) { _losLostTimer = 0f; PickNewPatrolWaypoint(); }
        if (next == EnemyState.Alert) _losLostTimer = 0f;
        State = next;
    }

    private void MoveToward(Vector2 target, float speed, float dt)
    {
        Vector2 dir = target - _enemy.Position;
        if (dir.LengthSquared() < 0.001f) return;
        dir.Normalize();
        MoveWithDir(dir, speed, dt);
    }

    private void MoveWithDir(Vector2 dir, float speed, float dt)
    {
        Vector2 avoidAmount = GetAvoidVector(dir);

        Vector2 finalDirection = dir + avoidAmount * 1.5f;
        if (finalDirection != Vector2.Zero) finalDirection.Normalize();

        Vector2 delta = finalDirection * speed * dt;

        float radius = _enemy.Radius;
        Vector2 checkPos = _enemy.Position + Vector2.Normalize(delta) * (delta.Length() + radius);

        if (!_isWall(new Vector2(checkPos.X, _enemy.Position.Y))) _enemy.Position.X += delta.X;
        if (!_isWall(new Vector2(_enemy.Position.X, checkPos.Y))) _enemy.Position.Y += delta.Y;
    }

    private Vector2 GetAvoidVector(Vector2 checkDir)
    {
        Vector2 avoidAmount = Vector2.Zero;
        float checkDistance = 25f;

        float angle = MathF.Atan2(checkDir.Y, checkDir.X);
        Vector2 leftCheck = new Vector2(MathF.Cos(angle - 0.7f), MathF.Sin(angle - 0.7f)) * checkDistance;
        Vector2 rightCheck = new Vector2(MathF.Cos(angle + 0.7f), MathF.Sin(angle + 0.7f)) * checkDistance;

        bool leftHit = _isWall(_enemy.Position + leftCheck);
        bool rightHit = _isWall(_enemy.Position + rightCheck);

        if (leftHit) avoidAmount += new Vector2(checkDir.Y, -checkDir.X);
        if (rightHit) avoidAmount += new Vector2(-checkDir.Y, checkDir.X);

        return avoidAmount;
    }

    private static float LerpAngle(float from, float to, float t)
    {
        float diff = ((to - from + MathF.PI * 3) % (MathF.PI * 2)) - MathF.PI;
        return from + diff * Math.Clamp(t, 0f, 1f);
    }
}