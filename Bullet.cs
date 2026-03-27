using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StormShooter;

public class Bullet
{
    public Vector2 Position;
    public Vector2 Velocity;

    public Bullet(Vector2 position, Vector2 velocity)
    {
        Position = position;
        Velocity = velocity;
    }
    
    // Check if bullets are far enough offscreen to be removed
    public bool IsOffscreen(int width, int height)
    {
        return Position.X < -300 || Position.X > width + 300 ||
               Position.Y < -300 || Position.Y > height + 300;
    }

    public void Update(float deltaTime)
    {
        
        // Move each frame
        Position += Velocity * deltaTime;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D texture)
    {
        // Draw bullets at their position and rotation
        var drawPos = new Vector2((int)Position.X, (int)Position.Y);
        
        float rotation = (float)Math.Atan2(Velocity.Y, Velocity.X);
        
        spriteBatch.Draw(
            texture,
            drawPos,
            null,
            Color.White,
            rotation,
            new Vector2(0.5f, 0.5f),
            new Vector2(7, 5),
            SpriteEffects.None,
            0f);
    }
}