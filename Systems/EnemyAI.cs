using System;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace StormShooter;

public enum EnemyState
{
    Patrol, // hasn't seen the player
    Alert, // noticed the player
    Engage, // moving to position to fight the player
    WindUp, // stop and telegraph before shooting
    Recover, // retreat to reposition
    Reposition, // reposition
}

public class EnemyAI
{
    // Movement
    private const float DetectRadius       = 250f;
    private const float PreferredRange = 90f;
    private const float RangeDeadzone = 18f;
    private const float OrbitSpeed = 40f;
    private const float PatrolSpeed = 25f;
    private const float AlertSpeed = 38f;
    private const float RepositionSpeed = 45f;
    private const float OrbitFlipInterval = 2f;
    private const float PatrolWaypointDist = 8f;
    private const float AlertLoseTime      = 5f;
    private const float LowHealthThreshold = 2f;

    // Shooting
    private const float ShootInterval    = 1.8f;   // pause between attacks while orbiting
    private const float ShootIntervalRand= 0.5f;
    private const float WindUpDuration   = 0.55f;  // how long the telegraph lasts
    private const float RecoverDuration  = 1.0f;   // how long the post-burst retreat lasts
    private const int   BurstShotCount   = 3;      // shots per attack
    private const float BurstShotDelay   = 0.10f;  // delay between shots in the burst
    private const float AccuracySpread   = 0.20f;  // aim jitter — higher = less accurate at range

    // Aim tracking speeds
    private const float AimLerpOrbit  = 5f;   // slow tracking while orbiting
    private const float AimLerpWindUp = 14f;  // fast lock-on during wind-up

    public EnemyState State { get; private set; } = EnemyState.Patrol;
    public float AimAngle { get; private set; }

    private readonly Enemy _enemy;
    private readonly Random _rng;
    private readonly Func<Vector2, bool> _isWall;
    private readonly Func<Vector2, Vector2, bool> _hasLOS;

    private Vector2 _patrolWaypoint;
    private float   _patrolWaypointTimer;
    private Vector2 _lastKnownPlayerPos;
    private float   _losLostTimer;
    private int     _orbitSign = 1;
    private float   _orbitFlipTimer;
    private float   _shootTimer;
    private float   _windUpTimer;
    private float   _recoverTimer;
    private int     _burstRemaining;
    private float   _burstShotTimer;
    private Vector2 _repositionTarget;
    private float   _repositionTimeout;
    private Vector2 _lastCheckedPos;
    private float   _stuckTimer;

    public EnemyAI(Enemy enemy, Random rng, Func<Vector2, bool> isWall, Func<Vector2, Vector2, bool> hasLOS)
    {
        _enemy = enemy;
        _rng = rng;
        _isWall = isWall;
        _hasLOS = hasLOS;
        _patrolWaypoint = enemy.Position;
        _shootTimer = ShootInterval + (float)(_rng.NextDouble() * ShootIntervalRand);
    }

    public void Update(float dt, Vector2 playerPos, Action<Vector2, Vector2, float> fireCallback)
    {
        float distToPlayer = Vector2.Distance(_enemy.Position, playerPos);
        bool inRadius = distToPlayer < DetectRadius;
        bool hasLineOfSight = inRadius && _hasLOS(_enemy.Position, playerPos);

        // aim at the player as long as they are not patrolling
        if (State != EnemyState.Patrol)
        {
            Vector2 toPlayer = playerPos - _enemy.Position;
            if (toPlayer.LengthSquared() > 0.01f)
            {
                float lerpSpeed = State == EnemyState.WindUp ? AimLerpWindUp : AimLerpOrbit;
                AimAngle = LerpAngle(AimAngle, MathF.Atan2(toPlayer.Y, toPlayer.X), dt * lerpSpeed);
            }
        }

        UpdateTransitions(dt, playerPos, hasLineOfSight, fireCallback);
        UpdateBehavior(dt, playerPos, fireCallback, hasLineOfSight);
    }

    private void UpdateTransitions(float dt, Vector2 playerPos, bool hasLineOfSight, Action<Vector2, Vector2, float> fireCallback)
    {
        switch (State)
        {
            case EnemyState.Patrol:
                if (hasLineOfSight) { _lastKnownPlayerPos = playerPos; TransitionTo(EnemyState.Alert); }
                break;

            case EnemyState.Alert:
                if (hasLineOfSight) { _lastKnownPlayerPos = playerPos; TransitionTo(EnemyState.Engage); }
                else { _losLostTimer += dt; if (_losLostTimer > AlertLoseTime) TransitionTo(EnemyState.Patrol); }
                break;

            case EnemyState.Engage:
                if (hasLineOfSight) { _lastKnownPlayerPos = playerPos; _losLostTimer = 0f; }
                else { _losLostTimer += dt; if (_losLostTimer > 0.4f) TransitionTo(EnemyState.Reposition); break; }
                _shootTimer -= dt;
                if (_shootTimer <= 0f) TransitionTo(EnemyState.WindUp);
                break;

            case EnemyState.WindUp:
                if (!hasLineOfSight) { TransitionTo(EnemyState.Reposition); break; }
                _lastKnownPlayerPos = playerPos;
                _windUpTimer -= dt;
                if (_windUpTimer <= 0f)
                {
                    FireOneShot(playerPos, fireCallback);
                    _burstRemaining = BurstShotCount - 1;
                    _burstShotTimer = BurstShotDelay;
                    TransitionTo(EnemyState.Recover);
                }
                break;

            case EnemyState.Recover:
                if (hasLineOfSight) _lastKnownPlayerPos = playerPos;
                if (_burstRemaining > 0)
                {
                    _burstShotTimer -= dt;
                    if (_burstShotTimer <= 0f)
                    {
                        FireOneShot(playerPos, fireCallback);
                        _burstRemaining--;
                        _burstShotTimer = BurstShotDelay;
                    }
                }
                _recoverTimer -= dt;
                if (_recoverTimer <= 0f)
                {
                    _shootTimer = ShootInterval + (float)((_rng.NextDouble() - 0.5) * ShootIntervalRand);
                    TransitionTo(EnemyState.Engage);
                }
                break;

            case EnemyState.Reposition:
                if (hasLineOfSight) { TransitionTo(EnemyState.Engage); break; }
                _repositionTimeout -= dt;
                if (_repositionTimeout <= 0f) TransitionTo(EnemyState.Alert);
                break;
        }
    }

    private void UpdateBehavior(float dt, Vector2 playerPos, Action<Vector2, Vector2, float> fireCallback, bool hasLOS)
    {
        switch (State)
        {
            case EnemyState.Patrol: UpdatePatrol(dt); break;
            case EnemyState.Alert: UpdateAlert(dt); break;
            case EnemyState.Engage: UpdateEngage(dt, playerPos); break;
            case EnemyState.WindUp: UpdateWindUp(dt, playerPos); break;
            case EnemyState.Recover: UpdateRecover(dt, playerPos); break;
            case EnemyState.Reposition: UpdateReposition(dt); break;
        }
    }

    private void UpdatePatrol(float dt)
    {
        _patrolWaypointTimer -= dt;
        if (Vector2.Distance(_enemy.Position, _patrolWaypoint) < PatrolWaypointDist || _patrolWaypointTimer <= 0f)
            PickNewPatrolWaypoint();
        MoveToward(_patrolWaypoint, PatrolSpeed, dt);
    }

    private void UpdateAlert(float dt)
    {
        if (Vector2.Distance(_enemy.Position, _lastKnownPlayerPos) > PatrolWaypointDist)
            MoveToward(_lastKnownPlayerPos, AlertSpeed, dt);
    }

    private void UpdateEngage(float dt, Vector2 playerPos)
    {
        _orbitFlipTimer -= dt;
        if (_orbitFlipTimer <= 0f)
        {
            _orbitSign = _rng.Next(2) == 0 ? 1 : -1;
            _orbitFlipTimer = OrbitFlipInterval + (float)(_rng.NextDouble() * 1.2f);
        }

        Vector2 toPlayer = playerPos - _enemy.Position;
        if (toPlayer.LengthSquared() > 0.001f) toPlayer.Normalize();

        Vector2 tangent = new Vector2(-toPlayer.Y, toPlayer.X) * _orbitSign;
        float dist = Vector2.Distance(_enemy.Position, playerPos);
        float radialBias = dist < PreferredRange - RangeDeadzone ? -1f
                         : dist > PreferredRange + RangeDeadzone ?  1f : 0f;

        Vector2 moveDir = tangent + toPlayer * (radialBias * 0.6f);
        if (moveDir.LengthSquared() > 0.001f) moveDir.Normalize();

        MoveWithDir(moveDir, OrbitSpeed * (_enemy.Health <= LowHealthThreshold ? 1.35f : 1f), dt);
    }

    private void UpdateWindUp(float dt, Vector2 playerPos)
    {
        // slow down mostly when shooting 
        float dist = Vector2.Distance(_enemy.Position, playerPos);
        if (dist > PreferredRange + RangeDeadzone)
        {
            Vector2 toPlayer = playerPos - _enemy.Position;
            if (toPlayer.LengthSquared() > 0.001f) toPlayer.Normalize();
            MoveWithDir(toPlayer, OrbitSpeed * 0.2f, dt);
        }
    }

    private void UpdateRecover(float dt, Vector2 playerPos)
    {
        // Retreat away from player after shooting
        Vector2 toPlayer = playerPos - _enemy.Position;
        if (toPlayer.LengthSquared() > 0.001f) toPlayer.Normalize();
        Vector2 tangent = new Vector2(-toPlayer.Y, toPlayer.X) * _orbitSign;
        Vector2 retreat = -toPlayer * 0.7f + tangent;
        if (retreat.LengthSquared() > 0.001f) retreat.Normalize();
        MoveWithDir(retreat, OrbitSpeed * 0.9f, dt);
    }

    private void UpdateReposition(float dt)
    {
        if (Vector2.Distance(_enemy.Position, _repositionTarget) > PatrolWaypointDist)
            MoveToward(_repositionTarget, RepositionSpeed, dt);
        else
            PickRepositionTarget(_lastKnownPlayerPos);
    }

    private void PickNewPatrolWaypoint()
    {
        for (int i = 0; i < 20; i++)
        {
            float angle  = (float)(_rng.NextDouble() * MathF.Tau);
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

    private void PickRepositionTarget(Vector2 playerPos)
    {
        Vector2 toPlayer = playerPos - _enemy.Position;
        float baseAngle = MathF.Atan2(toPlayer.Y, toPlayer.X);

        for (int i = 0; i < 16; i++)
        {
            float angle  = baseAngle + MathF.PI + (float)((_rng.NextDouble() * MathF.PI) - MathF.PI * 0.5f);
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

    private void FireOneShot(Vector2 playerPos, Action<Vector2, Vector2, float> fireCallback)
    {
        Gun gun = _enemy.Gun;
        int pellets = Math.Max(1, gun.BulletsPerShot);
        float aimJitter = (_rng.NextSingle() - 0.5f) * AccuracySpread * 2f;

        for (int i = 0; i < pellets; i++)
        {
            float t = pellets == 1 ? 0.5f : (float)i / (pellets - 1);
            float spread = MathHelper.Lerp(-gun.SpreadAngle / 2f, gun.SpreadAngle / 2f, t)
                         + (_rng.NextSingle() - 0.5f) * gun.SpreadAngle
                         + aimJitter;
            fireCallback?.Invoke(_enemy.Position, new Vector2(MathF.Cos(AimAngle), MathF.Sin(AimAngle)), spread);
        }
    }

    private void TransitionTo(EnemyState next)
    {
        switch (next)
        {
            case EnemyState.WindUp:     _windUpTimer = WindUpDuration; break;
            case EnemyState.Recover:    _recoverTimer = RecoverDuration; break;
            case EnemyState.Reposition: PickRepositionTarget(_lastKnownPlayerPos); break;
            case EnemyState.Patrol:     _losLostTimer = 0f; PickNewPatrolWaypoint(); break;
            case EnemyState.Alert:      _losLostTimer = 0f; break;
            case EnemyState.Engage:     _losLostTimer = 0f; break;
        }
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
        Vector2 avoid = GetAvoidVector(dir);
        Vector2 finalDir = dir + avoid * 1.5f;
        if (finalDir != Vector2.Zero) finalDir.Normalize();

        Vector2 delta = finalDir * speed * dt;
        float radius = _enemy.Radius;
        Vector2 checkPos = _enemy.Position + Vector2.Normalize(delta) * (delta.Length() + radius);

        if (!_isWall(new Vector2(checkPos.X, _enemy.Position.Y))) _enemy.Position.X += delta.X;
        if (!_isWall(new Vector2(_enemy.Position.X, checkPos.Y))) _enemy.Position.Y += delta.Y;
    }

    private Vector2 GetAvoidVector(Vector2 checkDir)
    {
        Vector2 avoid = Vector2.Zero;
        float angle = MathF.Atan2(checkDir.Y, checkDir.X);

        // use whiskers to try and avoid walls
        bool leftHit  = _isWall(_enemy.Position + new Vector2(MathF.Cos(angle - 0.65f), MathF.Sin(angle - 0.65f)) * 32f);
        bool rightHit = _isWall(_enemy.Position + new Vector2(MathF.Cos(angle + 0.65f), MathF.Sin(angle + 0.65f)) * 32f);
        bool frontHit = _isWall(_enemy.Position + new Vector2(MathF.Cos(angle),         MathF.Sin(angle))         * 28f);

        if (leftHit)  avoid += new Vector2( checkDir.Y, -checkDir.X);
        if (rightHit) avoid += new Vector2(-checkDir.Y,  checkDir.X);
        if (frontHit && !leftHit && !rightHit) // dont run straight into a wall
            avoid += _orbitSign > 0 ? new Vector2(-checkDir.Y, checkDir.X) : new Vector2(checkDir.Y, -checkDir.X);

        // prefer to move away from walls
        Vector2[] cardinals = { Vector2.UnitX, -Vector2.UnitX, Vector2.UnitY, -Vector2.UnitY };
        foreach (var c in cardinals)
        {
            if (_isWall(_enemy.Position + c * 18f))
                avoid -= c * 1.2f;
        }

        return avoid;
    }

    private static float LerpAngle(float from, float to, float t)
    {
        float diff = ((to - from + MathF.PI * 3) % (MathF.PI * 2)) - MathF.PI;
        return from + diff * Math.Clamp(t, 0f, 1f);
    }
}
