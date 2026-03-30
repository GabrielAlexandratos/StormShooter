using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StormShooter;

public class BulletManager
{
    private readonly List<Bullet> _bullets = new();

    public void Spawn(Vector2 position, Vector2 velocity)
    {
        _bullets.Add(new Bullet(position, velocity));
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
        for (int i = _bullets.Count - 1; i >= 0; i--)
        {
            var b = _bullets[i];
            b.Update(dt);

            bool hit = false;

            foreach (var e in enemies)
            {
                float dist = Vector2.Distance(b.Position, e.Position);

                if (dist < e.Radius)
                {
                    Vector2 dir = Vector2.Normalize(b.Velocity);
                    Vector2 knockback = dir * 35f;

                    e.Hit(knockback, currentGun.Damage);

                    hitStopTime = currentGun.HitStop;
                    shakeTime = 0.03f;
                    shakeStrength = 0f;

                    // Impact particles
                    int sparkCount = 5 + random.Next( 3);
                    for (int j = 0; j < sparkCount; j++)
                    {
                        float angle = (float)System.Math.Atan2(dir.Y, dir.X)
                                      + (random.NextSingle() - 0.5f) * 1.8f;

                        Vector2 sparkDir = new Vector2(
                            (float)System.Math.Cos(angle),
                            (float)System.Math.Sin(angle));

                        float speed = 180f + random.NextSingle() * 320f;

                        Color color = j % 2 == 0
                            ? new Color(220, 30, 30)
                            : new Color(255, 210, 80);

                        particles.Add(new Particle
                        {
                            Position = e.Position,
                            Velocity = sparkDir * speed,
                            Life = 0.2f,
                            MaxLife = 0.3f,
                            Size = 8f,
                            Rotation = angle,
                            Color = color,
                            Drag = 6f,
                            Gravity = 60f
                        });
                    }

                    hit = true;
                    break;
                }
            }

            // Set large bounds so that bullets are not despawned while still in the map
            if (hit || b.IsOffscreen(virtualWidth * 20, virtualHeight * 20))
            {
                _bullets.RemoveAt(i);
            }
            else
            {
                lighting.AddLight(new LightSource(b.Position, 20f, Color.White, 0.04f));
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (var b in _bullets)
            b.Draw(spriteBatch, pixel);
    }
}