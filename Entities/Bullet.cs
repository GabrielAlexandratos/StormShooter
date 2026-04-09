using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StormShooter;

public class Bullet
{
    public Vector2 Position;
    public Vector2 PreviousPosition;
    public Vector2 Velocity;
    public float Scale = 2f;
    public float Decay;
    public float MinSpeed;
    public bool IsAlive = true;
    public int BouncesRemaining;

    public float HitRadius;

    private float _timeActive = 0f;
    private float _lifeProgress = 0f;

    public Bullet(Vector2 position, Vector2 velocity, float decay = 0f, float minSpeed = 0f, float scale = 1f, int bounces = 0)
    {
        Position = position;
        PreviousPosition = position;
        Velocity = velocity;
        Decay = decay;
        MinSpeed = minSpeed;
        Scale = scale;
        BouncesRemaining = bounces;

        HitRadius = 1f * scale;
    }

    public void Update(float dt)
    {
        PreviousPosition = Position;

        _timeActive += dt;
        _lifeProgress = Math.Clamp(_timeActive * Decay, 0f, 1f);

        float speedFactor = 1.0f - _lifeProgress;

        Velocity *= MathF.Pow(0.98f, dt * 60f);
        Position += (Velocity * speedFactor) * dt;

        if (_lifeProgress >= 1.0f || (Velocity * speedFactor).Length() < 1f)
        {
            IsAlive = false;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D texture)
    {
        float speed = Velocity.Length();
        float rotation = (float)Math.Atan2(Velocity.Y, Velocity.X);

        float vanishThreshold = 0.91f;
        float visibility = 1.0f;
        if (_lifeProgress > vanishThreshold)
        {
            visibility = 1.0f - ((_lifeProgress - vanishThreshold) / (1.0f - vanishThreshold));
        }

        int visibleWidth = (int)(texture.Width * visibility);
        Rectangle sourceRect = new Rectangle(texture.Width - visibleWidth, 0, visibleWidth, texture.Height);

        float lengthScale = MathHelper.Clamp(speed / 250f, 0.3f, 1.2f) * Scale;
        float widthScale = MathHelper.Clamp(speed / 300f, 0.9f, 1.3f) * Scale;
        Vector2 finalScale = new Vector2(lengthScale, widthScale);

        // Make sure that the origin is at the tip of the bullet
        Vector2 origin = new Vector2(visibleWidth, texture.Height / 2f);
        Vector2 visualOffset = Vector2.Normalize(Velocity) * (HitRadius * 0.5f);

        spriteBatch.Draw(
            texture,
            Position + (Vector2.Normalize(Velocity) * 7f),
            sourceRect,
            Color.White,
            rotation,
            origin,
            finalScale,
            SpriteEffects.None,
            0f
        );
    }

    public bool IsOffscreen(int width, int height)
    {
        return Position.X < -100 || Position.X > width + 100 || Position.Y < -100 || Position.Y > height + 100;
    }
}
