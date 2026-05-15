using System;
using System.Collections.Generic;
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

    // ammo tracking for each gun
    private readonly Dictionary<Gun, int> _ammoPool = new();
    private float _reloadTimer;
    public int GetCurrentAmmo(Gun gun) => _ammoPool.GetValueOrDefault(gun, (int)gun.MagSize);

    public float ReloadProgress => _reloadTimer;
    public bool IsReloading => _reloadTimer > 0f;

    // Global ammo pools
    private int _lightAmmo = 20;
    private int _mediumAmmo = 15;
    private int _heavyAmmo = 5;

    private const int MaxAmmo = 999;

    public int GetPoolAmmo(AmmoType type) => type switch
    {
        AmmoType.Light  => _lightAmmo,
        AmmoType.Medium => _mediumAmmo,
        AmmoType.Heavy  => _heavyAmmo,
        _               => 0
    };

    public void AddAmmo(AmmoType type, int amount)
    {
        switch (type)
        {
            case AmmoType.Light:  _lightAmmo  = Math.Min(MaxAmmo, _lightAmmo  + amount); break;
            case AmmoType.Medium: _mediumAmmo = Math.Min(MaxAmmo, _mediumAmmo + amount); break;
            case AmmoType.Heavy:  _heavyAmmo  = Math.Min(MaxAmmo, _heavyAmmo  + amount); break;
        }
    }

    private void ConsumeAmmo(AmmoType type, int amount)
    {
        switch (type)
        {
            case AmmoType.Light:  _lightAmmo  = Math.Max(0, _lightAmmo  - amount); break;
            case AmmoType.Medium: _mediumAmmo = Math.Max(0, _mediumAmmo - amount); break;
            case AmmoType.Heavy:  _heavyAmmo  = Math.Max(0, _heavyAmmo  - amount); break;
        }
    }

    public GunController(Random random) => _random = random;

    public void Update(
        float dt,
        MouseState mouse,
        KeyboardState kb,
        Vector2 mouseWorld,
        Player player,
        Gun gun,
        BulletManager bulletManager,
        LightingRenderer lighting,
        ref float shakeTime,
        ref float shakeStrength,
        ref Vector2 shakeOffset)
    {
        if (!_ammoPool.ContainsKey(gun))
            _ammoPool[gun] = (int)gun.MagSize;

        if (_reloadTimer > 0f)
        {
            _reloadTimer -= dt;
            if (_reloadTimer <= 0f)
            {
                int needed    = (int)gun.MagSize - _ammoPool[gun];
                int available = GetPoolAmmo(gun.AmmoType);
                int toLoad    = Math.Min(needed, available);
                _ammoPool[gun] += toLoad;
                ConsumeAmmo(gun.AmmoType, toLoad);
            }

            return;
        }

        bool magNotFull  = _ammoPool[gun] < (int)gun.MagSize;
        bool poolHasAmmo = GetPoolAmmo(gun.AmmoType) > 0;

        if (kb.IsKeyDown(Keys.R) && magNotFull && poolHasAmmo)
        {
            _reloadTimer = gun.ReloadTime;
            SoundManager.Play(gun.ReloadSound, 1f);
            return;
        }

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

        if (wantsToShoot && _shotCooldown <= 0f && !_isBursting)
        {
            if (_ammoPool[gun] > 0)
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
            else
            {
                SoundManager.Play("dry_fire", 0.6f);
                _shotCooldown = 0.3f; // i want to stop this from firing when holding down on an auto weapon
            }
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
        _ammoPool[gun]--;
        SoundManager.Play(gun.ShotSound, 0.4f, (_random.NextSingle() - 0.5f) * 0.4f);

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
            Vector2 muzzlePos = player.GetMuzzleWorld(gun);

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

        shakeOffset += -direction * gun.kickBack;

        shakeTime = gun.ShakeDuration;
        shakeStrength = 0f;
        player.ApplyRecoil(direction, 10, _random.NextSingle() * 0.12f);
    }

    public void CancelReload()
    {
        _reloadTimer = 0f;
    }
}
