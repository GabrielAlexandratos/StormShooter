using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StormShooter;

public class Enemy
{
    public Vector2 Position;
    public Vector2 Velocity;

    public float Radius = 6f;
    public float Health = 6f;
    public Gun Gun { get; set; } = null;

    private float _flashTime;

    public EnemyAI AI { get; private set; }

    public void InitAI(Random rng, Func<Vector2, bool> isWall, Func<Vector2, Vector2, bool> hasLOS)
    {
        AI = new EnemyAI(this, rng, isWall, hasLOS);
    }

    public void Update(float deltaTime)
    {
        Position += Velocity * deltaTime;
        Velocity *= 0.9f;
        if (_flashTime > 0) _flashTime -= deltaTime;
    }

    public void UpdateAI(float dt, Vector2 playerPos, Action<Vector2, Vector2, float> fireCallback)
    {
        AI?.Update(dt, playerPos, fireCallback);
    }

    public void Hit(Vector2 force, float damage, float impact, Action<float, float> addShake, float shakeStrength)
    {
        Health -= damage;
        Velocity += force;
        _flashTime = 0.065f;
        addShake?.Invoke(Settings.EnemyHitShakeDuration, shakeStrength);
    }

    public bool IsDead() => Health <= 0;

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        Color bodyColor = _flashTime > 0 ? Color.White : Color.Red;

        spriteBatch.Draw(pixel, Position, null, bodyColor, 0f,
            new Vector2(0.5f, 0.5f), new Vector2(Radius * 2, Radius * 2), SpriteEffects.None, 0f);

        if (AI != null && AI.State != EnemyState.Patrol)
        {
            float angle = AI.AimAngle;
            Vector2 gunPos = Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 5f;

            spriteBatch.Draw(pixel,
                new Vector2(MathF.Round(gunPos.X), MathF.Round(gunPos.Y)),
                null, Color.DarkGray, angle,
                new Vector2(0f, 0.5f), new Vector2(10f, 3f),
                MathF.Cos(angle) < 0f ? SpriteEffects.FlipVertically : SpriteEffects.None, 0f);
        }
    }
}