using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StormShooter;

public struct PersistentParticleConfig
{
    public Color Color;
    public Vector2 Size;
    public float MinSizeScale;
    public float MaxSizeScale;
    public float MinSpeed;
    public float MaxSpeed;
    public float MaxAngularVelocity;
    public float Drag;
    public float AngularDrag;
    public float StopSpeed;
    public float BaseAngle;
    public float AngleSpread;
    public float SpawnRadius;
    public bool UseCircle;

    public static readonly PersistentParticleConfig Casing = new()
    {
        Color = new Color(210, 200, 40),
        Size = new Vector2(4f, 2f),
        MinSizeScale = 1f,
        MaxSizeScale = 1f,
        MinSpeed = 170f,
        MaxSpeed = 280f,
        MaxAngularVelocity = 50f,
        Drag = 14f,
        AngularDrag = 12f,
        StopSpeed = 8f,
        BaseAngle = MathHelper.PiOver2,
        AngleSpread = MathHelper.ToRadians(60f),
    };

    public static readonly PersistentParticleConfig Blood = new()
    {
        Color = new Color(235, 85, 85),
        Size = new Vector2(0.4f, 0.4f),
        MinSizeScale = 4f,
        MaxSizeScale = 14f,
        MinSpeed = 0f,
        MaxSpeed = 8f,
        MaxAngularVelocity = 0f,
        Drag = 20f,
        AngularDrag = 20f,
        StopSpeed = 12f,
        BaseAngle = 0f,
        AngleSpread = MathHelper.TwoPi,
        SpawnRadius = 5f,
        UseCircle = false,
    };
}

public struct PersistentParticle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Rotation;
    public float AngularVelocity;
    public bool Settled;
    public bool UseCircle;
    public Color Color;
    public Vector2 Size;
    public float Drag;
    public float AngularDrag;
    public float StopSpeed;
}

public class PersistentParticleSystem
{
    private const int MaxParticles = 500;

    private readonly List<PersistentParticle> _particles = new();

    public void Spawn(Vector2 position, PersistentParticleConfig config, Random rng)
    {
        if (_particles.Count >= MaxParticles)
            _particles.RemoveAt(0);

        float angle = config.BaseAngle + (rng.NextSingle() - 0.5f) * 2f * config.AngleSpread;
        Vector2 dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        float speed = config.MinSpeed + rng.NextSingle() * (config.MaxSpeed - config.MinSpeed);
        float sizeScale = config.MinSizeScale + rng.NextSingle() * (config.MaxSizeScale - config.MinSizeScale);

        float jitterAngle = rng.NextSingle() * MathF.Tau;
        float jitterDist = rng.NextSingle() * config.SpawnRadius;
        Vector2 spawnPos = position + new Vector2(MathF.Cos(jitterAngle), MathF.Sin(jitterAngle)) * jitterDist;

        _particles.Add(new PersistentParticle
        {
            Position = spawnPos,
            Velocity = dir * speed,
            Rotation = rng.NextSingle() * MathF.Tau,
            AngularVelocity = (rng.NextSingle() - 0.5f) * 2f * config.MaxAngularVelocity,
            Settled = false,
            UseCircle = config.UseCircle,
            Color = config.Color,
            Size = config.Size * sizeScale,
            Drag = config.Drag,
            AngularDrag = config.AngularDrag,
            StopSpeed = config.StopSpeed,
        });
    }

    public void Update(float dt)
    {
        for (int i = 0; i < _particles.Count; i++)
        {
            var p = _particles[i];
            if (p.Settled) continue;

            p.Velocity *= MathF.Max(0f, 1f - p.Drag * dt);
            p.AngularVelocity *= MathF.Max(0f, 1f - p.AngularDrag * dt);
            p.Position += p.Velocity * dt;
            p.Rotation += p.AngularVelocity * dt;

            if (p.Velocity.LengthSquared() < p.StopSpeed * p.StopSpeed)
            {
                p.Velocity = Vector2.Zero;
                p.AngularVelocity = 0f;
                p.Settled = true;
            }

            _particles[i] = p;
        }
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, Texture2D circle)
    {
        foreach (var p in _particles)
        {
            sb.Draw(
                p.UseCircle ? circle : pixel,
                p.Position,
                null,
                p.Color,
                p.Rotation,
                new Vector2(0.5f, 0.5f),
                p.Size,
                SpriteEffects.None,
                0f
            );
        }
    }
}
