using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StormShooter;

public class Enemy
{
    public Vector2 Position;
    public Vector2 Velocity;

    public float Radius = 6f;
    public float Health = 5f;

    private float _flashTime;

    public Enemy(Vector2 position)
    {
        Position = position;
    }

    public void Update(float deltaTime)
    {
        Position += Velocity * deltaTime;

        if (_flashTime > 0)
            _flashTime -= deltaTime;

        Velocity *= 0.9f;
    }

    public void Hit(Vector2 force, float damage, float impact, Action<float, float> addShake, float shakeStrength)
    {
        Health -= damage;
        Velocity += force;
        _flashTime = 0.065f;

        float shakeTime = Settings.EnemyHitShakeDuration;
        addShake?.Invoke(shakeTime, shakeStrength);
    }

    public bool IsDead() => Health <= 0;

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        Color color = _flashTime > 0 ? Color.White : Color.Red;

        spriteBatch.Draw(pixel,
            Position,
            null,
            color,
            0f,
            new Vector2(0.5f, 0.5f),
            new Vector2(Radius * 2, Radius * 2),
            SpriteEffects.None,
            0f);
    }
}
