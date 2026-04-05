using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace StormShooter;

public class GunController
{
    private float _shotCooldown;
    private MouseState _previousMouse;
    private readonly Random _random;
    
    private int _burstShotsRemaining;
    private float _burstTimer;
    private bool _isBursting;

    public GunController(Random random) => _random = random;

    public void Update(
        float dt,
        MouseState mouse,
        Vector2 mouseWorld,
        Player player,
        Gun gun,
        BulletManager bulletManager,
        LightingRenderer lighting,
        ref float shakeTime,
        ref float shakeStrength,
        ref Vector2 shakeOffset)
    {
        // Handle burst firing
        if (_isBursting)
        {
            _burstTimer -= dt;
            if (_burstTimer <= 0f && _burstShotsRemaining > 0)
            {
                Fire(player, gun, mouseWorld, bulletManager, lighting, ref shakeTime, ref shakeStrength, ref shakeOffset);
                _burstTimer = gun.BurstDelay;
                if (--_burstShotsRemaining <= 0) _isBursting = false;
            }
        }

        // Handle automatic gun
        bool wantsToShoot = gun.Automatic
            ? mouse.LeftButton == ButtonState.Pressed
            : mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;

        if (_shotCooldown > 0f) _shotCooldown -= dt;

        //Check if the player can is not allready shooting or in a cooldown
        if (wantsToShoot && _shotCooldown <= 0f && !_isBursting)
        {
            if (gun.BurstCount > 1)
            {
                _isBursting = true;
                _burstShotsRemaining = gun.BurstCount;
                _burstTimer = 0f;
            }
            else
            {
                Fire(player, gun, mouseWorld, bulletManager, lighting, ref shakeTime, ref shakeStrength, ref shakeOffset);
            }

            _shotCooldown = 1f / gun.FireRate;
        }

        _previousMouse = mouse;
    }

    private void Fire(
        Player player,
        Gun gun,
        Vector2 mouseWorld,
        BulletManager bulletManager,
        LightingRenderer lighting,
        ref float shakeTime,
        ref float shakeStrength,
        ref Vector2 shakeOffset)
    {
        Vector2 direction = mouseWorld - player.Position;
        if (direction.LengthSquared() > 0.0001f) direction.Normalize();

        float baseAngle = (float)Math.Atan2(direction.Y, direction.X);
        int bulletCount = Math.Max(1, gun.BulletsPerShot);

        for (int i = 0; i < bulletCount; i++)
        {
            float t = bulletCount == 1 ? 0.5f : (float)i / (bulletCount - 1);
            float angle = baseAngle
                + MathHelper.Lerp(-gun.SpreadAngle / 2f, gun.SpreadAngle / 2f, t)
                + (_random.NextSingle() - 0.5f) * gun.SpreadAngle;

            Vector2 shootDir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            Vector2 muzzlePos = player.GunPos + shootDir * 8f;

            float speedMultiplier = 1f;
            float decayVariation = 0f;

            if (gun.UseSpeedVariation)
            {
                speedMultiplier = 0.85f + _random.NextSingle() * 0.3f;
                decayVariation = (_random.NextSingle() * 0.8f) - 0.4f;
            }

            bulletManager.Spawn(
                muzzlePos,
                shootDir * gun.BulletSpeed * speedMultiplier,
                gun.VelocityDecay + decayVariation,
                gun.MinBulletSpeed,
                gun.BulletScale,
                gun.CanBounce ? gun.MaxBounces : 0
            );
            lighting.AddFlash(muzzlePos, 40f, Color.White, 0.06f);
            lighting.AddFlash(muzzlePos, 30f, Color.Yellow, 0.10f);
        }

        shakeOffset += -direction * gun.ShakeStrength;
        
        shakeTime = gun.ShakeDuration;
        // shakeStrength = gun.ShakeStrength; (old random screen shake when shooting)
        player.ApplyRecoil(direction, gun.Recoil * 1.3f, _random.NextSingle() * 0.12f);
    }
}