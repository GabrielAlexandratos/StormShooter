using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StormShooter;

public class Bullet
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Scale = 2f;
    public float Decay;
    public float MinSpeed;
    public bool IsAlive = true;
    public int BouncesRemaining;

    public Bullet(Vector2 position, Vector2 velocity, float decay = 0f, float minSpeed = 0f, float scale = 1f, int bounces = 0)
    {
        Position = position;
        Velocity = velocity;
        Decay = decay;
        MinSpeed = minSpeed;
        Scale = scale;
        BouncesRemaining = bounces;
    }

    // Checks if bullets are far enough away to safely despawn them
    public bool IsOffscreen(int width, int height) =>
        Position.X < -300 || Position.X > width + 300 ||
        Position.Y < -300 || Position.Y > height + 300;

    public void Update(float deltaTime)
    {
        if (Decay > 0f)
        {
            float speed = Velocity.Length();

            if (speed > 0f)
            {
                // Slowing down the bullets as they travel
                speed -= Decay * speed * deltaTime;

                // Despawn if they are not moving 
                if (speed <= 0f)
                {
                    IsAlive = false;
                    return;
                }

                Velocity = Vector2.Normalize(Velocity) * speed;
            }
        }

        Position += Velocity * deltaTime;

        // Kill bullets that are below the minimum speed threshold
        if (Velocity.LengthSquared() < 400f)
            IsAlive = false;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D texture)
    {
        float speed = Velocity.Length();
        float rotation = (float)Math.Atan2(Velocity.Y, Velocity.X);

        // Stretch the bullet sprite based on speed
        float lengthScale = MathHelper.Clamp(speed / 250f, 2.0f, 6.0f);
        float widthScale = MathHelper.Clamp(speed / 300f, 0.9f, 1.3f);
        Vector2 scaleVec = new Vector2(Scale * lengthScale * 4.0f, Scale * widthScale * 4.0f);

        spriteBatch.Draw(
            texture,
            new Vector2((int)Position.X, (int)Position.Y),
            null,
            Color.White,
            rotation,
            new Vector2(0f, 0.5f),
            scaleVec,
            SpriteEffects.None,
            0f
        );
    }
}