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
    private bool _hasAimRotation;

    private AnimatedSprite _idleAnim;
    private AnimatedSprite _walkAnim;
    private AnimatedSprite _currentAnim;
    private bool _isMoving = false;

    public int[] FootstepFrames = { 1, 4 };
    public Action<Vector2> OnFootstep;
    private Vector2 _lastMoveDir = Vector2.UnitX;
    public float SpeedMultiplier = 1f;

    public float MaxHealth = 60f;
    public float Health;
    public bool IsAlive => Health > 0f;
    public Action OnDeath;

    public Player(Vector2 startPos, float speed, Texture2D idleTexture, Texture2D walkTexture)
    {
        Position = startPos;
        _speed = speed;
        Health = MaxHealth;
        _idleAnim = new AnimatedSprite(idleTexture, 2, 2f);
        _walkAnim = new AnimatedSprite(walkTexture, 6, 8f);
        _currentAnim = _idleAnim;

        _walkAnim.OnFrameChanged += frame =>
        {
            if (_isMoving && Array.IndexOf(FootstepFrames, frame) >= 0)
                OnFootstep?.Invoke(_lastMoveDir);
        };
    }

    public void Update(float dt, KeyboardState kb, Vector2 mouseWorld, Gun gun, Func<Vector2, bool> isWall)
    {
        Vector2 move = Vector2.Zero;
        if (kb.IsKeyDown(Keys.W)) move.Y -= 1f;
        if (kb.IsKeyDown(Keys.S)) move.Y += 1f;
        if (kb.IsKeyDown(Keys.A)) move.X -= 1f;
        if (kb.IsKeyDown(Keys.D)) move.X += 1f;
        if (move != Vector2.Zero) move.Normalize();
        move *= _speed * SpeedMultiplier * gun.EquippedMoveSpeedMultiplier * dt;
        
        bool wasMoving = _isMoving;
        _isMoving = move != Vector2.Zero;
        if (_isMoving) _lastMoveDir = Vector2.Normalize(move);

        if (_isMoving != wasMoving)
        {
            _currentAnim = _isMoving ? _walkAnim : _idleAnim;
        }
        
        _currentAnim.Update(dt);

        if (!isWall(new Vector2(Position.X + move.X, Position.Y))) Position.X += move.X;
        if (!isWall(new Vector2(Position.X, Position.Y + move.Y))) Position.Y += move.Y;

        Vector2 direction = mouseWorld - Position;
        if (direction.LengthSquared() > 0.0001f) direction.Normalize();

        _gunPos = Position + direction * 0.5f + _recoilOffset + new Vector2(0f, 3f);
        float targetRotation = (float)Math.Atan2(direction.Y, direction.X);
        if (!_hasAimRotation)
        {
            _gunRotation = targetRotation;
            _hasAimRotation = true;
        }
        else
        {
            float aimLerpSpeed = MathHelper.Clamp(22f / Math.Max(0.2f, gun.AimDrag), 0f, 900f);
            _gunRotation = MathHelper.Lerp(_gunRotation, WrapAngleNear(_gunRotation, targetRotation), dt * aimLerpSpeed);
        }

        _finalRotation = _gunRotation + _recoilRotation;
        _gunFlip = direction.X < 0;

        _recoilOffset = Vector2.Lerp(_recoilOffset, Vector2.Zero, dt * _recoilRecoverSpeed);
        _recoilRotation = MathHelper.Lerp(_recoilRotation, 0f, dt * _recoilRecoverSpeed);

    }

    public void Hit(float damage)
    {
        if (!IsAlive) return;
        Health = Math.Max(0f, Health - damage);
        if (Health <= 0f)
        {
            SoundManager.Play("humanhit1", 1f, .45f);
            OnDeath?.Invoke();
        }
        else
        {
            SoundManager.Play("humanhit1", 1f, 1f);
        }
    }

    public void ApplyRecoil(Vector2 direction, float distance, float lateralDistance, float recoverySpeed, float rotationKick)
    {
        float recoilTravel = distance * 1.75f;
        Vector2 sideways = new Vector2(-direction.Y, direction.X);
        _recoilOffset += -direction * recoilTravel;
        _recoilOffset += sideways * lateralDistance;
        float maxRecoilOffset = recoilTravel * 5f;
        if (_recoilOffset.LengthSquared() > maxRecoilOffset * maxRecoilOffset)
        {
            _recoilOffset.Normalize();
            _recoilOffset *= maxRecoilOffset;
        }

        _recoilRotation += rotationKick;
        _recoilRotation = MathHelper.Clamp(_recoilRotation, -0.35f, 0.35f);
        _recoilRecoverSpeed = recoverySpeed;
    }

    private static float WrapAngleNear(float current, float target)
    {
        while (target - current > MathF.PI) target -= MathF.Tau;
        while (target - current < -MathF.PI) target += MathF.Tau;
        return target;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D gunTexture, Gun gun)
    {
        //Vector2 gunDrawPos = new Vector2(MathF.Round(_gunPos.X), MathF.Round(_gunPos.Y));
        Vector2 gunDrawPos = _gunPos;
        
        Rectangle? sourceRect = _currentAnim.GetSourceRect();

        Color spriteColor = Color.White;

        spriteBatch.Draw(_currentAnim.Texture, Position, sourceRect, spriteColor, 0f, new Vector2(_currentAnim.FrameWidth / 2, _currentAnim.FrameHeight / 2), new Vector2(1f, 1f), _gunFlip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);

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
