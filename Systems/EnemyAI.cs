using System;
using Microsoft.Xna.Framework;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace StormShooter;

public enum EnemyState
{
    Patrol,  // wandering, hasn't spotted the player
    Engage,  // orbiting at preferred range, counting down to shoot
    WindUp,  // stopped and telegraphing before firing
    Recover, // retreating after burst, firing remaining shots
}

public class EnemyAI
{
    // Detection / range
    private const float DetectRadius    = 250f;
    private const float PreferredRange  = 90f;
    private const float RangeDeadzone  = 18f;
    private const float OrbitSpeed     = 40f;
    private const float PatrolSpeed    = 25f;
    private const float OrbitFlipInterval = 2f;
    private const float PatrolWaypointDist = 8f;

    // Shooting
    private const float ShootInterval    = 1.8f;
    private const float ShootIntervalRand = 0.5f;
    private const float WindUpDuration   = 0.55f;
    private const float RecoverDuration  = 1.0f;
    private const int   BurstShotCount   = 4;
    private const float BurstShotDelay   = 0.10f;

    // Aim tracking
    private const float AimLerpOrbit  = 5f;
    private const float AimLerpWindUp = 14f;

    // Context steering
    private const int   NumContextDirs = 16;
    private const float LookAhead      = 22f;
    private const int   TileSize       = 16;

    // Stuck detection
    private const float StuckCheckInterval = 2f;
    private const float StuckThreshold     = 20f;  // pixels over StuckCheckInterval
    private const float UnstuckDuration    = 0.7f;
    private static readonly Vector2[] ContextDirs = BuildContextDirs();

    public EnemyState State     { get; private set; } = EnemyState.Patrol;
    public float      AimAngle  { get; private set; }

    private readonly Enemy                          _enemy;
    private readonly Random                         _rng;
    private readonly Func<Vector2, bool>            _isWall;
    private readonly Func<Vector2, Vector2, bool>   _hasLOS;

    private Vector2 _patrolWaypoint;
    private float   _patrolWaypointTimer;
    private Vector2 _lastKnownPlayerPos;
    private int     _orbitSign = 1;
    private float   _orbitFlipTimer;
    private float   _shootTimer;
    private float   _windUpTimer;
    private float   _recoverTimer;
    private int     _burstRemaining;
    private float   _burstShotTimer;
    private Vector2 _steerDir = Vector2.UnitX;
    private float   _stuckCheckTimer;
    private Vector2 _stuckCheckPos;
    private float   _unstuckTimer;
    private Vector2 _unstuckDir;

    public EnemyAI(Enemy enemy, Random rng, Func<Vector2, bool> isWall, Func<Vector2, Vector2, bool> hasLOS)
    {
        _enemy = enemy;
        _rng   = rng;
        _isWall = isWall;
        _hasLOS = hasLOS;
        _patrolWaypoint = enemy.Position;
        _shootTimer = ShootInterval + (float)(_rng.NextDouble() * ShootIntervalRand);
    }

    public void Update(float dt, Vector2 playerPos, Action<Vector2, Vector2, float> fireCallback)
    {
        UpdateStuckDetection(dt);

        float dist   = Vector2.Distance(_enemy.Position, playerPos);
        bool  hasLOS = dist < DetectRadius && _hasLOS(_enemy.Position, playerPos);

        if (State != EnemyState.Patrol)
        {
            if (hasLOS) _lastKnownPlayerPos = playerPos;
            Vector2 toPlayer = playerPos - _enemy.Position;
            if (toPlayer.LengthSquared() > 0.01f)
            {
                float lerpSpeed = State == EnemyState.WindUp ? AimLerpWindUp : AimLerpOrbit;
                AimAngle = LerpAngle(AimAngle, MathF.Atan2(toPlayer.Y, toPlayer.X), dt * lerpSpeed);
            }
        }

        switch (State)
        {
            case EnemyState.Patrol:
            {
                if (hasLOS) { _lastKnownPlayerPos = playerPos; TransitionTo(EnemyState.Engage); break; }
                _patrolWaypointTimer -= dt;
                if (Vector2.Distance(_enemy.Position, _patrolWaypoint) < PatrolWaypointDist || _patrolWaypointTimer <= 0f)
                    PickNewPatrolWaypoint();
                MoveToward(_patrolWaypoint, PatrolSpeed, dt);
                break;
            }

            case EnemyState.Engage:
            {
                _shootTimer -= dt;
                if (_shootTimer <= 0f) { TransitionTo(EnemyState.WindUp); break; }

                _orbitFlipTimer -= dt;
                if (_orbitFlipTimer <= 0f)
                {
                    _orbitSign      = _rng.Next(2) == 0 ? 1 : -1;
                    _orbitFlipTimer = OrbitFlipInterval + (float)(_rng.NextDouble() * 1.2f);
                }

                if (!hasLOS)
                {
                    MoveToward(_lastKnownPlayerPos, OrbitSpeed, dt);
                    break;
                }

                Vector2 toPlayer = playerPos - _enemy.Position;
                if (toPlayer.LengthSquared() > 0.001f) toPlayer.Normalize();
                Vector2 tangent = new Vector2(-toPlayer.Y, toPlayer.X) * _orbitSign;
                float radialBias = dist < PreferredRange - RangeDeadzone ? -1f
                                 : dist > PreferredRange + RangeDeadzone ?  1f : 0f;
                Vector2 moveDir = tangent + toPlayer * (radialBias * 0.6f);
                if (moveDir.LengthSquared() > 0.001f) moveDir.Normalize();
                MoveWithDir(moveDir, OrbitSpeed, dt);
                break;
            }

            case EnemyState.WindUp:
            {
                if (!hasLOS) { TransitionTo(EnemyState.Engage); break; }
                _windUpTimer -= dt;
                if (_windUpTimer <= 0f)
                {
                    FireOneShot(playerPos, fireCallback);
                    _burstRemaining = GetBurstShotCount(_enemy.Gun) - 1;
                    _burstShotTimer = BurstShotDelay;
                    TransitionTo(EnemyState.Recover);
                }
                break;
            }

            case EnemyState.Recover:
            {
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
                if (_recoverTimer <= 0f) { TransitionTo(EnemyState.Engage); break; }

                Vector2 toPlayer = playerPos - _enemy.Position;
                if (toPlayer.LengthSquared() > 0.001f) toPlayer.Normalize();
                Vector2 tangent = new Vector2(-toPlayer.Y, toPlayer.X) * _orbitSign;
                Vector2 retreat = -toPlayer * 0.7f + tangent;
                if (retreat.LengthSquared() > 0.001f) retreat.Normalize();
                MoveWithDir(retreat, OrbitSpeed * 0.9f, dt);
                break;
            }
        }

        SeparateFromWalls();
    }

    //movement
    private void MoveToward(Vector2 target, float speed, float dt)
    {
        Vector2 dir = target - _enemy.Position;
        if (dir.LengthSquared() < 0.001f) return;
        dir.Normalize();
        MoveWithDir(dir, speed, dt);
    }

    private void MoveWithDir(Vector2 desiredDir, float speed, float dt)
    {
        Vector2 target = _unstuckTimer > 0f ? _unstuckDir : desiredDir;
        Vector2 chosen = EnemySteer(target);
        _steerDir = Vector2.Lerp(_steerDir, chosen, dt * 10f);
        if (_steerDir.LengthSquared() > 0.001f) _steerDir.Normalize();
        _enemy.Position += _steerDir * speed * dt;
    }

    private Vector2 EnemySteer(Vector2 desiredDir)
    {
        float sideR = _enemy.Radius * 0.85f;

        float   bestScore = float.MinValue;
        Vector2 bestDir   = desiredDir;

        for (int i = 0; i < NumContextDirs; i++)
        {
            Vector2 d     = ContextDirs[i];
            float   score = Vector2.Dot(d, desiredDir);

            if (score <= bestScore) continue;

            Vector2 perp  = new Vector2(-d.Y, d.X);
            Vector2 ahead = _enemy.Position + d * LookAhead;

            if (_isWall(ahead) || _isWall(ahead + perp * sideR) || _isWall(ahead - perp * sideR))
                continue;

            bestScore = score;
            bestDir   = d;
        }

        return bestDir;
    }

    // Circle-vs-AABB separation: pushes the enemy out of any wall tile it overlaps.
    // Handles corners correctly — at a protruding corner the nearest tile point is
    // the vertex, so the push is automatically diagonal.
    private void SeparateFromWalls()
    {
        float r   = _enemy.Radius;
        int   etx = (int)(_enemy.Position.X / TileSize);
        int   ety = (int)(_enemy.Position.Y / TileSize);

        for (int ox = -1; ox <= 1; ox++)
        for (int oy = -1; oy <= 1; oy++)
        {
            int tx = etx + ox, ty = ety + oy;
            if (!_isWall(new Vector2(tx * TileSize + TileSize * 0.5f, ty * TileSize + TileSize * 0.5f)))
                continue;

            float nearX  = Math.Clamp(_enemy.Position.X, tx * TileSize, (tx + 1) * TileSize);
            float nearY  = Math.Clamp(_enemy.Position.Y, ty * TileSize, (ty + 1) * TileSize);
            float sepX   = _enemy.Position.X - nearX;
            float sepY   = _enemy.Position.Y - nearY;
            float distSq = sepX * sepX + sepY * sepY;

            if (distSq >= r * r) continue;

            if (distSq < 0.0001f)
            {
                Vector2 away = _enemy.Position - new Vector2((tx + 0.5f) * TileSize, (ty + 0.5f) * TileSize);
                if (away.LengthSquared() < 0.001f) away = Vector2.UnitX;
                else away.Normalize();
                _enemy.Position += away * r;
            }
            else
            {
                float dist = MathF.Sqrt(distSq);
                _enemy.Position.X += sepX / dist * (r - dist);
                _enemy.Position.Y += sepY / dist * (r - dist);
            }
        }
    }

    // ── Stuck detection ──────────────────────────────────────────────────────

    private void UpdateStuckDetection(float dt)
    {
        _stuckCheckTimer += dt;

        if (_unstuckTimer > 0f)
        {
            _unstuckTimer -= dt;
            if (_unstuckTimer <= 0f)
                _stuckCheckPos = _enemy.Position;
            return;
        }

        if (_stuckCheckTimer < StuckCheckInterval) return;
        _stuckCheckTimer = 0f;

        float moved = Vector2.Distance(_enemy.Position, _stuckCheckPos);
        _stuckCheckPos = _enemy.Position;

        if (State == EnemyState.WindUp || moved >= StuckThreshold) return;

        TryBeginUnstuck();
    }

    private void TryBeginUnstuck()
    {
        float sideR = _enemy.Radius * 0.85f;
        int   start = _rng.Next(NumContextDirs);

        for (int i = 0; i < NumContextDirs; i++)
        {
            Vector2 d    = ContextDirs[(start + i) % NumContextDirs];
            Vector2 perp = new Vector2(-d.Y, d.X);
            Vector2 ahead = _enemy.Position + d * LookAhead;
            if (_isWall(ahead) || _isWall(ahead + perp * sideR) || _isWall(ahead - perp * sideR))
                continue;

            _unstuckDir   = d;
            _unstuckTimer = UnstuckDuration;
            return;
        }
    }

    // ── Shooting / helpers ───────────────────────────────────────────────────

    private void PickNewPatrolWaypoint()
    {
        for (int i = 0; i < 20; i++)
        {
            float   angle     = (float)(_rng.NextDouble() * MathF.Tau);
            float   radius    = 32f + (float)(_rng.NextDouble() * 64f);
            Vector2 candidate = _enemy.Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            if (!_isWall(candidate) && _hasLOS(_enemy.Position, candidate))
            {
                _patrolWaypoint      = candidate;
                _patrolWaypointTimer = 3f + (float)_rng.NextDouble() * 2f;
                return;
            }
        }
        _patrolWaypointTimer = 1.5f;
    }

    private static int GetBurstShotCount(Gun gun) =>
        gun.BulletsPerShot > 1 ? 1 : BurstShotCount;

    private void FireOneShot(Vector2 playerPos, Action<Vector2, Vector2, float> fireCallback)
    {
        Gun gun     = _enemy.Gun;
        int pellets = Math.Max(1, gun.BulletsPerShot);
        SoundManager.Play(gun.ShotSound, 0.16f, (_rng.NextSingle() + 1.5f) * 0.4f);

        for (int i = 0; i < pellets; i++)
        {
            float t      = pellets == 1 ? 0.5f : (float)i / (pellets - 1);
            float spread = MathHelper.Lerp(-gun.SpreadAngle / 2f, gun.SpreadAngle / 2f, t)
                         + (_rng.NextSingle() - 0.5f) * gun.SpreadAngle;
            fireCallback?.Invoke(_enemy.Position, new Vector2(MathF.Cos(AimAngle), MathF.Sin(AimAngle)), spread);
        }
    }

    private void TransitionTo(EnemyState next)
    {
        switch (next)
        {
            case EnemyState.Engage:  _shootTimer   = ShootInterval + (float)(_rng.NextDouble() * ShootIntervalRand); break;
            case EnemyState.WindUp:  _windUpTimer  = WindUpDuration; break;
            case EnemyState.Recover: _recoverTimer = RecoverDuration; break;
            case EnemyState.Patrol:  PickNewPatrolWaypoint(); break;
        }
        State = next;
    }

    private static float LerpAngle(float from, float to, float t)
    {
        float diff = ((to - from + MathF.PI * 3) % (MathF.PI * 2)) - MathF.PI;
        return from + diff * Math.Clamp(t, 0f, 1f);
    }

    private static Vector2[] BuildContextDirs()
    {
        var dirs = new Vector2[NumContextDirs];
        for (int i = 0; i < NumContextDirs; i++)
        {
            float a = i * MathF.Tau / NumContextDirs;
            dirs[i] = new Vector2(MathF.Cos(a), MathF.Sin(a));
        }
        return dirs;
    }
}
