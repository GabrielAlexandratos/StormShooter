using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace StormShooter;

public class Player
{
    public Vector2 Position;

    private float _speed;

    private Vector2 _gunPos;
    private float _gunRotation;
    private float _finalRotation;
    private bool _gunFlip;

    private Vector2 _recoilOffset;
    private float _recoilRotation;
    private float _recoilRecoverSpeed = 10f;

    private Texture2D _pixel;

    public Vector2 GunPos => _gunPos;
    public float GunRotation => _gunRotation;
    public float FinalRotation => _finalRotation;
    public bool GunFlip => _gunFlip;

    public Player(Vector2 startPos, float speed, Texture2D pixel)
    {
        Position = startPos;
        _speed = speed;
        _pixel = pixel;
    }

    public void Update(float dt, KeyboardState kb, MouseState mouse, int windowScale, Vector2 cameraPos, Func<Vector2, bool> isWall)
    {
        // Movement (axis-separated for proper sliding)
        Vector2 move = Vector2.Zero;

        if (kb.IsKeyDown(Keys.W)) move.Y -= 1f;
        if (kb.IsKeyDown(Keys.S)) move.Y += 1f;
        if (kb.IsKeyDown(Keys.A)) move.X -= 1f;
        if (kb.IsKeyDown(Keys.D)) move.X += 1f;

        if (move != Vector2.Zero)
            move.Normalize();

        move *= _speed * dt;

        // X movement
        Vector2 newPosX = new Vector2(Position.X + move.X, Position.Y);
        if (!isWall(newPosX))
            Position.X = newPosX.X;

        // Y movement
        Vector2 newPosY = new Vector2(Position.X, Position.Y + move.Y);
        if (!isWall(newPosY))
            Position.Y = newPosY.Y;

        // Aim
        Vector2 mouseWorld = cameraPos + new Vector2(mouse.X, mouse.Y) / windowScale;
        Vector2 direction = mouseWorld - Position;

        if (direction.LengthSquared() > 0.0001f)
            direction.Normalize();
        else
            direction = Vector2.Zero;

        _gunPos = Position + direction * 6f + _recoilOffset;

        _gunRotation = (float)Math.Atan2(direction.Y, direction.X);
        _finalRotation = _gunRotation + _recoilRotation;

        _gunFlip = direction.X < 0;

        // Recoil recovery
        _recoilOffset = Vector2.Lerp(_recoilOffset, Vector2.Zero, dt * _recoilRecoverSpeed);
        _recoilRotation = MathHelper.Lerp(_recoilRotation, 0f, dt * _recoilRecoverSpeed);
    }

    public void ApplyRecoil(Vector2 direction, float strength, float randomRotation)
    {
        _recoilOffset = -direction * strength;
        _recoilRotation = randomRotation;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Player body
        spriteBatch.Draw(_pixel,
            Position,
            null,
            Color.White,
            0f,
            new Vector2(0.5f, 0.5f),
            new Vector2(10, 10),
            SpriteEffects.None,
            0f);

        // Gun
        spriteBatch.Draw(_pixel,
            _gunPos,
            null,
            Color.Red,
            _finalRotation,
            new Vector2(0f, 0.5f),
            new Vector2(11, 4),
            _gunFlip ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
            0f);
    }
}