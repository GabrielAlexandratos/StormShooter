using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StormShooter;

public class BulletManager
{
    private readonly Random _rng = new();

    private readonly List<Bullet> _bullets = new();

    private const int TileSize = Settings.TileSize;

    public void Spawn(Vector2 position, Vector2 velocity, float decay = 0f, float minSpeed = 0f, float scale = 1f, int bounces = 0)
    {
        _bullets.Add(new Bullet(position, velocity, decay, minSpeed, scale, bounces) { IsEnemy = false });
    }

    public void FireEnemyBullet(Vector2 origin, Vector2 direction, Gun gun, Random rng)
    {
        float speedMultiplier = 1f;
        float decayVariation = 0f;

        if (gun.UseSpeedVariation)
        {
            speedMultiplier = 0.85f + rng.NextSingle() * 0.3f;
            decayVariation = (rng.NextSingle() * 0.8f) - 0.4f;
        }

        _bullets.Add(new Bullet(
            origin,
            direction * gun.BulletSpeed * speedMultiplier,
            gun.VelocityDecay + decayVariation,
            gun.MinBulletSpeed,
            gun.BulletScale,
            gun.CanBounce ? gun.MaxBounces : 0)
        {
            IsEnemy = true,
            Damage = gun.Damage,
        });
    }

    public void Update(
        float dt,
        List<Enemy> enemies,
        ParticleSystem particles,
        PersistentParticleSystem persistentParticles,
        LightingRenderer lighting,
        Gun currentGun,
        ref ScreenFeedback feedback,
        Func<Vector2, bool> isWall,
        Player player = null)
    {
        for (int i = _bullets.Count - 1; i >= 0; i--)
        {
            var b = _bullets[i];
            Vector2 frameStart = b.Position;

            b.Update(dt);

            if (!b.IsAlive || b.IsBulletOffScreen(Settings.VirtualWidth * 20, Settings.VirtualHeight * 20))
            {
                _bullets.RemoveAt(i);
                continue;
            }

            HitResult wallHit = TraceRay(frameStart, b.Position, isWall);
            float wallT = wallHit.DidHit ? wallHit.T : float.MaxValue;
            bool bulletHit = false;

            if (b.IsEnemy && player != null)
            {
                float combinedRadius = 5f + b.HitRadius + b.Scale * 2.5f;
                if (SweptCircleHit(frameStart, b.Position, player.Position, combinedRadius))
                {
                    player.Hit(b.Damage * 10f);
                    SpawnHitParticles(particles, _rng, player.Position,
                        b.Velocity.LengthSquared() > 0f ? Vector2.Normalize(b.Velocity) : Vector2.UnitX,
                        new Color(255, 200, 80));
                    for (int part = 0; part < 12; part++)
                        persistentParticles.Spawn(player.Position, PersistentParticleConfig.Blood, _rng);
                    bulletHit = true;
                }
            }

            if (!b.IsEnemy && !bulletHit)
            {
                float bulletDamage = b.Damage > 0f ? b.Damage : currentGun.Damage;

                foreach (var e in enemies)
                {
                    float graze = b.Scale * 2.5f;
                    float combinedRadius = e.Radius + b.HitRadius + graze;

                    Vector2 travelDir = b.Position - frameStart;
                    float travelDist = travelDir.Length();
                    Vector2 adjustedTip = b.Position;
                    if (travelDist > 0.001f)
                        adjustedTip -= (travelDir / travelDist) * (combinedRadius * 0.6f);

                    float enemyT = SweptCircleHitT(frameStart, adjustedTip, e.Position, combinedRadius);
                    if (enemyT >= wallT)
                        continue;

                    Vector2 dir = b.Velocity.LengthSquared() > 0f ? Vector2.Normalize(b.Velocity) : Vector2.UnitX;
                    float impact = b.Velocity.Length();

                    float shakeTime = 0f, shakeStrength = 0f;
                    e.Hit(dir * 15f, bulletDamage, impact, (t, s) => { shakeTime = t; shakeStrength = s; }, currentGun.HitShakeStrength);
                    feedback.ShakeTime = shakeTime;
                    feedback.ShakeStrength = shakeStrength;

                    if (e.IsDead())
                    {
                        feedback.HitStopTime = currentGun.HitStop;
                        SpawnDeathParticles(particles, _rng, e.Position);
                        for (int part = 0; part < 14; part++)
                            persistentParticles.Spawn(e.Position, PersistentParticleConfig.Blood, _rng);
                    }
                    else
                    {
                        for (int part = 0; part < 7; part++)
                            persistentParticles.Spawn(e.Position, PersistentParticleConfig.Blood, _rng);
                    }

                    SpawnHitParticles(particles, _rng, e.Position, dir, new Color(220, 30, 30));
                    SoundManager.Play("humanhit1", 0.5f, (float)(_rng.NextDouble()) * 0.3f);

                    bulletHit = true;
                    break;
                }
            }

            if (!bulletHit && wallHit.DidHit)
            {
                SpawnDustParticles(particles, _rng, wallHit.SafePosition - wallHit.Normal * 2f, wallHit.Normal, new Color(140, 140, 140));
                float debrisAngle = MathF.Atan2(wallHit.Normal.Y, wallHit.Normal.X);
                Vector2 debrisOrigin = wallHit.SafePosition + wallHit.Normal * 3f;
                int debrisCount = 1 + _rng.Next(2);
                for (int d = 0; d < debrisCount; d++)
                    persistentParticles.Spawn(debrisOrigin, PersistentParticleConfig.WallDebris, _rng, debrisAngle);
                SoundManager.PlayRandom(0.1f, (_rng.NextSingle() - 0.5f) * 0.08f, "snowimpact1", "snowimpact2");
                if (b.BouncesRemaining > 0)
                {
                    b.Velocity = ReflectVelocity(b.Velocity, wallHit.Normal);
                    b.Position = wallHit.SafePosition;
                    b.BouncesRemaining--;
                }
                else
                {
                    bulletHit = true;
                }
            }

            if (bulletHit)
                _bullets.RemoveAt(i);
            else
            {
                _bullets[i] = b;
                lighting?.AddLight(new LightSource(b.Position, 20f, Color.White, 0.04f));
            }
        }
    }

    private static void SpawnDeathParticles(ParticleSystem particles, Random random, Vector2 pos)
    {
        for (int j = 0; j < 14; j++)
        {
            float angle = random.NextSingle() * MathF.PI * 1.4f;
            float speed = 250f + random.NextSingle() * 400f;
            particles.Add(new Particle
            {
                Position = pos,
                Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                Life = 0.3f,
                MaxLife = 0.45f,
                Size = j < 4 ? 10f : 5f,
                Rotation = angle,
                Color = j % 3 == 0 ? new Color(255, 60, 60) : new Color(180, 20, 20),
                Drag = 10f,
                Gravity = 55f
            });
        }
    }

    private static void SpawnHitParticles(ParticleSystem particles, Random random, Vector2 pos, Vector2 dir, Color baseColor)
    {
        for (int particle = 0; particle < 8 + random.Next(4); particle++)
        {
            float angle = MathF.Atan2(dir.Y, dir.X) + (random.NextSingle() - 0.5f) * 1.6f;
            float speed = 190f + random.NextSingle() * 380f;
            particles.Add(new Particle
            {
                Position = pos,
                Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                Life = 0.18f,
                MaxLife = 0.28f,
                Size = particle < 3 ? 8f : 5f,
                Rotation = angle,
                Color = particle % 2 == 0 ? baseColor : new Color(255, 80, 80),
                Drag = 15f,
                Gravity = 70f
            });
        }
    }
    
    private static void SpawnDustParticles(ParticleSystem particles, Random random, Vector2 pos, Vector2 dir, Color baseColor)
    {
        for (int particle = 0; particle < 4 + random.Next(2); particle++)
        {
            float angle = MathF.Atan2(dir.Y, dir.X) + (random.NextSingle() - 0.5f) * 1.6f;
            float speed = 120f + random.NextSingle() * 380f;
            particles.Add(new Particle
            {
                Position = pos,
                Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                Life = 0.18f,
                MaxLife = 0.28f,
                Size = particle < 3 ? 7f : 5f,
                Rotation = angle,
                Color = particle % 2 == 0 ? baseColor : new Color(255, 175, 71),
                Drag = 14f,
                Gravity = 70f,
                IsSquare = true
            });
        }
    }

    private static bool SweptCircleHit(Vector2 from, Vector2 to, Vector2 center, float radius)
    {
        return SweptCircleHitT(from, to, center, radius) < float.MaxValue;
    }

    private static float SweptCircleHitT(Vector2 from, Vector2 to, Vector2 center, float radius)
    {
        Vector2 d = to - from;
        Vector2 f = from - center;

        float a = Vector2.Dot(d, d);
        float b = 2f * Vector2.Dot(f, d);
        float c = Vector2.Dot(f, f) - radius * radius;

        if (a < 1e-10f) return c <= 0f ? 0f : float.MaxValue;

        float discriminant = b * b - 4f * a * c;
        if (discriminant < 0f) return float.MaxValue;

        float sqrtDisc = MathF.Sqrt(discriminant);
        float t0 = (-b - sqrtDisc) / (2f * a);
        float t1 = (-b + sqrtDisc) / (2f * a);

        if (t0 <= 1f && t1 >= 0f)
            return MathF.Max(0f, t0);

        return float.MaxValue;
    }

    private struct HitResult
    {
        public bool DidHit;
        public Vector2 SafePosition;
        public Vector2 Normal;
        public float T;
    }

    private static HitResult TraceRay(Vector2 from, Vector2 to, Func<Vector2, bool> isWall)
    {
        float x0 = from.X / TileSize;
        float y0 = from.Y / TileSize;
        float x1 = to.X / TileSize;
        float y1 = to.Y / TileSize;

        int tileX = (int)MathF.Floor(x0);
        int tileY = (int)MathF.Floor(y0);
        int endTileX = (int)MathF.Floor(x1);
        int endTileY = (int)MathF.Floor(y1);

        float dx = x1 - x0;
        float dy = y1 - y0;

        int stepX = dx > 0 ? 1 : dx < 0 ? -1 : 0;
        int stepY = dy > 0 ? 1 : dy < 0 ? -1 : 0;

        float tDeltaX = MathF.Abs(dx) > 1e-6f ? MathF.Abs(1f / dx) : float.MaxValue;
        float tDeltaY = MathF.Abs(dy) > 1e-6f ? MathF.Abs(1f / dy) : float.MaxValue;

        float tMaxX = MathF.Abs(dx) > 1e-6f
            ? (stepX > 0 ? MathF.Floor(x0) + 1f - x0 : x0 - MathF.Floor(x0)) * tDeltaX
            : float.MaxValue;

        float tMaxY = MathF.Abs(dy) > 1e-6f
            ? (stepY > 0 ? MathF.Floor(y0) + 1f - y0 : y0 - MathF.Floor(y0)) * tDeltaY
            : float.MaxValue;

        bool crossedX = false;

        while (true)
        {
            bool isStart = tileX == (int)MathF.Floor(x0) && tileY == (int)MathF.Floor(y0);

            if (!isStart)
            {
                Vector2 cellCenter = new Vector2((tileX + 0.5f) * TileSize, (tileY + 0.5f) * TileSize);
                if (isWall(cellCenter))
                {
                    Vector2 normal = crossedX ? new Vector2(-stepX, 0) : new Vector2(0, -stepY);
                    float tHit = Math.Clamp(crossedX ? tMaxX - tDeltaX : tMaxY - tDeltaY - 0.01f, 0f, 1f);
                    return new HitResult
                    {
                        DidHit = true,
                        SafePosition = new Vector2(from.X + (to.X - from.X) * tHit, from.Y + (to.Y - from.Y) * tHit),
                        Normal = normal,
                        T = tHit
                    };
                }
            }

            if (tileX == endTileX && tileY == endTileY) break;

            if (tMaxX < tMaxY) { tMaxX += tDeltaX; tileX += stepX; crossedX = true; }
            else { tMaxY += tDeltaY; tileY += stepY; crossedX = false; }
        }

        return new HitResult { DidHit = false };
    }

    private static Vector2 ReflectVelocity(Vector2 velocity, Vector2 normal)
    {
        return velocity - 2f * Vector2.Dot(velocity, normal) * normal;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D texture)
    {
        foreach (var b in _bullets)
            b.Draw(spriteBatch, texture);
    }
}
