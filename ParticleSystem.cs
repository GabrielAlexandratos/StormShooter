using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StormShooter;

public class ParticleSystem
{
    private List<Particle> _particles = new();

    public void Add(Particle p)
    {
        _particles.Add(p);
    }

    public void Update(float deltaTime)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Update(deltaTime);

            if (p.Life <= 0)
                _particles.RemoveAt(i);
            else
                _particles[i] = p;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (var p in _particles)
        {
            float t = p.T;

            float alpha = t < 0.6f ? 1f : 1f - ((t - 0.6f) / 0.4f);
            
            float rotation = p.Velocity.LengthSquared() > 0.001f
                ? (float)Math.Atan2(p.Velocity.Y, p.Velocity.X)
                : p.Rotation;

            float speed = p.Velocity.Length();

            // Stretch based on speed
            float length = 1f + MathF.Min(speed * 0.006f, 3.5f);
            float width = 0.7f;

            float size = p.Size * (1f - t * t);

            spriteBatch.Draw(
                pixel,
                p.Position,
                null,
                p.Color * alpha,
                rotation,
                new Vector2(0.5f, 0.5f),
                new Vector2(size * length, size * width),
                SpriteEffects.None,
                0f
            );
        }
    }
}