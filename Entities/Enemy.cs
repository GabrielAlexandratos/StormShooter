using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StormShooter;

public class Enemy
{
    public Vector2 Position;
    public Vector2 Velocity;

    public float Radius = 6f;
    public float Health = 6.5f;
    public Gun Gun { get; set; } = null;

    private float _flashTime;
    private Vector2 _prevPosition;
    private bool _isMoving;
    private Vector2 _lastMoveDir = Vector2.UnitX;

    private AnimatedSprite _idleAnim;
    private AnimatedSprite _walkAnim;
    private AnimatedSprite _currentAnim;

    public EnemyAI AI { get; private set; }

    public void InitAI(Random rng, Func<Vector2, bool> isWall, Func<Vector2, Vector2, bool> hasLOS)
    {
        AI = new EnemyAI(this, rng, isWall, hasLOS);
    }

    public void InitSprite(Texture2D idleTexture, Texture2D walkTexture)
    {
        _idleAnim = new AnimatedSprite(idleTexture, 2, 2f);
        _walkAnim = new AnimatedSprite(walkTexture, 6, 8f);
        _currentAnim = _idleAnim;
    }

    public void Update(float deltaTime)
    {
        _prevPosition = Position;
        Position += Velocity * deltaTime;
        Velocity *= 0.9f;
        if (_flashTime > 0) _flashTime -= deltaTime;
    }

    public void UpdateAI(float dt, Vector2 playerPos, Action<Vector2, Vector2, float> fireCallback)
    {
        AI?.Update(dt, playerPos, fireCallback);
    }

    public void UpdateAnimation(float dt)
    {
        Vector2 delta = Position - _prevPosition;
        bool moving = delta.LengthSquared() > 0.01f * dt * dt;
        if (moving)
            _lastMoveDir = delta;
        if (moving != _isMoving)
        {
            _isMoving = moving;
            _currentAnim = _isMoving ? _walkAnim : _idleAnim;
        }
        _currentAnim?.Update(dt);
    }

    public void Hit(Vector2 force, float damage, float impact, Action<float, float> addShake, float shakeStrength)
    {
        Health -= damage;
        Velocity += force;
        _flashTime = 0.065f;
        addShake?.Invoke(Settings.EnemyHitShakeDuration, shakeStrength);
    }

    public bool IsDead() => Health <= 0;

    public void Draw(SpriteBatch spriteBatch, Texture2D fallbackPixel, Texture2D gunTexture)
    {
        if (_currentAnim != null)
        {
            bool flip = _lastMoveDir.X < 0f;
            Color spriteColor = _flashTime > 0 ? Color.White
                             : AI?.State == EnemyState.WindUp ? Color.Orange
                             : Color.White;
            Rectangle? sourceRect = _currentAnim.GetSourceRect();
            spriteBatch.Draw(
                _currentAnim.Texture,
                Position,
                sourceRect,
                spriteColor,
                0f,
                new Vector2(_currentAnim.FrameWidth / 2f, _currentAnim.FrameHeight / 2f),
                Vector2.One,
                flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                0f);
        }

        if (Gun == null) return;

        gunTexture ??= fallbackPixel;
        float angle = AI != null && AI.State != EnemyState.Patrol
            ? AI.AimAngle
            : MathF.Atan2(_lastMoveDir.Y, _lastMoveDir.X);
        bool gunFlip = MathF.Cos(angle) < 0f;
        Vector2 gunDir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 gunPos = Position + gunDir * 0.5f + new Vector2(0f, 3f);

        if (gunTexture.Width == 1 && gunTexture.Height == 1)
        {
            spriteBatch.Draw(gunTexture, gunPos, null, Color.DarkGray, angle,
                new Vector2(0f, 0.5f), new Vector2(10f, 3f),
                gunFlip ? SpriteEffects.FlipVertically : SpriteEffects.None, 0f);
        }
        else
        {
            Vector2 drawOrigin = Gun.SpriteOrigin;
            if (gunFlip)
                drawOrigin.Y = gunTexture.Height - Gun.SpriteOrigin.Y;

            spriteBatch.Draw(gunTexture, gunPos, null, Color.White, angle,
                drawOrigin, Gun.SpriteScale,
                gunFlip ? SpriteEffects.FlipVertically : SpriteEffects.None, 0f);
        }
    }
}