using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StormShooter;

public class BulletManager
{
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
        LightingRenderer lighting,
        Gun currentGun,
        ref float hitStopTime,
        ref float shakeTime,
        ref float shakeStrength,
        int virtualWidth,
        int virtualHeight,
        Random random,
        Func<Vector2, bool> isWall,
        Player player = null)
    {
        for (int i = _bullets.Count - 1; i >= 0; i--)
        {
            var b = _bullets[i];
            Vector2 frameStart = b.Position;

            b.Update(dt);

            if (!b.IsAlive || b.IsOffscreen(virtualWidth * 20, virtualHeight * 20))
            {
                _bullets.RemoveAt(i);
                continue;
            }

            HitResult hit = TraceRay(frameStart, b.Position, isWall);
            if (hit.DidHit)
            {
                if (b.BouncesRemaining > 0)
                {
                    b.Velocity = ReflectVelocity(b.Velocity, hit.Normal);
                    b.Position = hit.SafePosition;
                    b.BouncesRemaining--;
                }
                else
                {
                    _bullets.RemoveAt(i);
                    continue;
                }
            }

            _bullets[i] = b;

            bool bulletHit = false;

            if (b.IsEnemy && player != null)
            {
                float combinedRadius = 5f + b.HitRadius + b.Scale * 2.5f;
                if (SweptCircleHit(frameStart, b.Position, player.Position, combinedRadius))
                {
                    SpawnHitParticles(particles, random, player.Position,
                        b.Velocity.LengthSquared() > 0f ? Vector2.Normalize(b.Velocity) : Vector2.UnitX,
                        new Color(255, 200, 80));
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

                    if (!SweptCircleHit(frameStart, adjustedTip, e.Position, combinedRadius))
                        continue;

                    Vector2 dir = b.Velocity.LengthSquared() > 0f ? Vector2.Normalize(b.Velocity) : Vector2.UnitX;
                    float impact = b.Velocity.Length();

                    float capturedShakeTime = 0;
                    float capturedShakeStrength = 0;

                    e.Hit(dir * 15f, bulletDamage, impact, (t, s) =>
                    {
                        capturedShakeTime = t;
                        capturedShakeStrength = s;
                    }, currentGun.ShakeStrength);

                    shakeTime = capturedShakeTime;
                    shakeStrength = capturedShakeStrength;

                    if (e.IsDead())
                    {
                        hitStopTime = currentGun.HitStop;
                        SpawnDeathParticles(particles, random, e.Position);
                    }

                    SpawnHitParticles(particles, random, e.Position, dir, new Color(220, 30, 30));

                    bulletHit = true;
                    break;
                }
            }

            if (bulletHit)
                _bullets.RemoveAt(i);
            else
                lighting.AddLight(new LightSource(b.Position, 20f, Color.White, 0.04f));
        }
    }

    private static void SpawnDeathParticles(ParticleSystem particles, Random random, Vector2 pos)
    {
        for (int j = 0; j < 6; j++)
        {
            float angle = random.NextSingle() * MathF.PI * 2f;
            particles.Add(new Particle
            {
                Position = pos,
                Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (150f + random.NextSingle() * 200f),
                Life = 0.25f,
                MaxLife = 0.35f,
                Size = 8f,
                Rotation = angle,
                Color = new Color(200, 40, 40),
                Drag = 4f,
                Gravity = 40f
            });
        }
    }

    private static void SpawnHitParticles(ParticleSystem particles, Random random, Vector2 pos, Vector2 dir, Color baseColor)
    {
        for (int j = 0; j < 5 + random.Next(3); j++)
        {
            float angle = MathF.Atan2(dir.Y, dir.X) + (random.NextSingle() - 0.5f) * 1.8f;
            particles.Add(new Particle
            {
                Position = pos,
                Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (180f + random.NextSingle() * 320f),
                Life = 0.2f,
                MaxLife = 0.3f,
                Size = 8f,
                Rotation = angle,
                Color = j % 2 == 0 ? baseColor : new Color(255, 210, 80),
                Drag = 6f,
                Gravity = 60f
            });
        }
    }

    private static bool SweptCircleHit(Vector2 from, Vector2 to, Vector2 center, float radius)
    {
        Vector2 d = to - from;
        Vector2 f = from - center;

        float a = Vector2.Dot(d, d);
        float b = 2f * Vector2.Dot(f, d);
        float c = Vector2.Dot(f, f) - radius * radius;

        if (a < 1e-10f) return c <= 0f;

        float discriminant = b * b - 4f * a * c;
        if (discriminant < 0f) return false;

        float sqrtDisc = MathF.Sqrt(discriminant);
        float t0 = (-b - sqrtDisc) / (2f * a);
        float t1 = (-b + sqrtDisc) / (2f * a);

        return t0 <= 1f && t1 >= 0f;
    }

    private struct HitResult
    {
        public bool DidHit;
        public Vector2 SafePosition;
        public Vector2 Normal;
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
                        Normal = normal
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