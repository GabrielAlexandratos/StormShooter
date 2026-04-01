using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StormShooter;

public class BulletManager
{
    private readonly List<Bullet> _bullets = new();

    public void Spawn(Vector2 position, Vector2 velocity, float decay = 0f, float minSpeed = 0f, float scale = 1f)
    {
        _bullets.Add(new Bullet(position, velocity, decay, minSpeed, scale));
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
        Random random)
    {
        float localShakeTime = shakeTime;
        float localShakeStrength = shakeStrength;

        for (int i = _bullets.Count - 1; i >= 0; i--)
        {
            var b = _bullets[i];
            b.Update(dt);

            if (!b.IsAlive) { _bullets.RemoveAt(i); continue; }

            bool hit = false;

            foreach (var e in enemies)
            {
                if (Vector2.Distance(b.Position, e.Position) >= e.Radius + 4f * b.Scale)
                    continue;

                Vector2 dir = Vector2.Normalize(b.Velocity);
                float impact = b.Velocity.Length();
                e.Hit(dir * 15f, currentGun.Damage, impact, (t, s) =>
                {
                    localShakeTime = MathF.Max(localShakeTime, t);
                    localShakeStrength = MathF.Max(localShakeStrength, s);
                });

                if (e.IsDead())
                {
                    hitStopTime = currentGun.HitStop;
                    shakeTime = 0.03f;
                    shakeStrength = 0f;

                    for (int j = 0; j < 6; j++)
                    {
                        float angle = random.NextSingle() * MathF.PI * 2f;
                        particles.Add(new Particle
                        {
                            Position = e.Position,
                            Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (150f + random.NextSingle() * 200f),
                            Life = 0.25f, MaxLife = 0.35f, Size = 8f, Rotation = angle,
                            Color = new Color(200, 40, 40), Drag = 4f, Gravity = 40f
                        });
                    }
                }

                for (int j = 0; j < 5 + random.Next(3); j++)
                {
                    float angle = (float)Math.Atan2(dir.Y, dir.X) + (random.NextSingle() - 0.5f) * 1.8f;
                    particles.Add(new Particle
                    {
                        Position = e.Position,
                        Velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * (180f + random.NextSingle() * 320f),
                        Life = 0.2f, MaxLife = 0.3f, Size = 8f, Rotation = angle,
                        Color = j % 2 == 0 ? new Color(220, 30, 30) : new Color(255, 210, 80),
                        Drag = 6f, Gravity = 60f
                    });
                }

                hit = true;
                break;
            }

            if (hit || b.IsOffscreen(virtualWidth * 20, virtualHeight * 20))
                _bullets.RemoveAt(i);
            else
                lighting.AddLight(new LightSource(b.Position, 20f, Color.White, 0.04f));
        }
        shakeTime = localShakeTime;
        shakeStrength = localShakeStrength;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (var b in _bullets)
            b.Draw(spriteBatch, pixel);
    }
}