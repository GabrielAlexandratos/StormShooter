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

    public void Update(float dt, KeyboardState kb, Vector2 mouseWorld, Func<Vector2, bool> isWall)
    {
        Vector2 move = Vector2.Zero;
        if (kb.IsKeyDown(Keys.W)) move.Y -= 1f;
        if (kb.IsKeyDown(Keys.S)) move.Y += 1f;
        if (kb.IsKeyDown(Keys.A)) move.X -= 1f;
        if (kb.IsKeyDown(Keys.D)) move.X += 1f;

        if (move != Vector2.Zero) move.Normalize();
        move *= _speed * dt;

        // Collision handling
        if (!isWall(new Vector2(Position.X + move.X, Position.Y))) Position.X += move.X;
        if (!isWall(new Vector2(Position.X, Position.Y + move.Y))) Position.Y += move.Y;

        // Aiming
        Vector2 direction = mouseWorld - Position;
        if (direction.LengthSquared() > 0.0001f) direction.Normalize();

        _gunPos = Position + direction * 6f + _recoilOffset;
        _gunRotation = (float)Math.Atan2(direction.Y, direction.X);
        _finalRotation = _gunRotation + _recoilRotation;
        _gunFlip = direction.X < 0;

        _recoilOffset = Vector2.Lerp(_recoilOffset, Vector2.Zero, dt * _recoilRecoverSpeed);
        _recoilRotation = MathHelper.Lerp(_recoilRotation, 0f, dt * _recoilRecoverSpeed);
    }

    public void ApplyRecoil(Vector2 direction, float strength, float randomRotation)
    {
        _recoilOffset = -direction * strength;
        _recoilRotation = randomRotation;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D gunTexture, Gun gun)
    {
        Vector2 drawPos = new Vector2(MathF.Round(Position.X), MathF.Round(Position.Y));
        Vector2 gunDrawPos = new Vector2(MathF.Round(_gunPos.X), MathF.Round(_gunPos.Y));

        if (gunTexture.Width == 1 && gunTexture.Height == 1)
        {
            spriteBatch.Draw(gunTexture, gunDrawPos, null, Color.Red, _finalRotation,
                new Vector2(0f, 0.5f), new Vector2(12, 4),
                _gunFlip ? SpriteEffects.FlipVertically : SpriteEffects.None, 0f);
        }
        else
        {
            Vector2 drawOrigin = gun.SpriteOrigin;
            if (_gunFlip)
                drawOrigin.Y = gunTexture.Height - gun.SpriteOrigin.Y;

            spriteBatch.Draw(gunTexture, gunDrawPos, null, Color.White, _finalRotation,
                drawOrigin, gun.SpriteScale,
                _gunFlip ? SpriteEffects.FlipVertically : SpriteEffects.None, 0f);
        }
        
        spriteBatch.Draw(_pixel, drawPos, null, Color.White, 0f, new Vector2(0.5f, 0.5f), new Vector2(10, 10), SpriteEffects.None, 0f);
   }
    
    public Vector2 GetMuzzleWorld(Gun gun)
    {
        Vector2 muzzleOffset = gun.MuzzleOffset;
        if (_gunFlip)
            muzzleOffset.Y = -muzzleOffset.Y;

        float cos = MathF.Cos(_gunRotation);
        float sin = MathF.Sin(_gunRotation);
        Vector2 rotated = new Vector2(
            muzzleOffset.X * cos - muzzleOffset.Y * sin,
            muzzleOffset.X * sin + muzzleOffset.Y * cos
        );
        return _gunPos + rotated;
    }
}
