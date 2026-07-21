using System;
using Microsoft.Xna.Framework;

namespace StormShooter;

public class GunData
{
    public static Gun Pistol = new Gun
    {
        AmmoType = AmmoType.Light,
        SpriteName = "gun_pistol",
        DroppedSpriteName = "gun_pistol_drop",
        SpriteOrigin = new Vector2(-5f, 3f),
        MuzzleOffset = new Vector2(-1.5f, 0f),
        Damage = 2f,
        MagSize = 13f,
        ReloadTime = 1f,
        FireRate = 10f,
        BulletSpeed = 1600f,
        VelocityDecay = 0f,
        MinBulletSpeed = 0f,
        BulletScale = 0.6f,
        HitStop = 0.07f,
        Automatic = false,
        UseSpeedVariation = false,
        HitShakeStrength = 2.6f,
        CameraKickDistance = 3.4f,
        BulletsPerShot = 1,
        SpreadAngle = 0.17f,
        BurstCount = 0,
        BurstDelay = 0f,
        RecoilRotationKick = 5.8f,
        RecoilReturnSpeed = 8.5f,
        EquippedMoveSpeedMultiplier = 1f,
        AimDrag = 1f,
        CanBounce = false,
    };
    
    // SMGs
    public static Gun VAL = new Gun
    {
        AmmoType = AmmoType.Light,
        SpriteName = "gun_asval",
        DroppedSpriteName = "gun_asval_drop",
        SpriteOrigin = new Vector2(1f, 4f),
        MuzzleOffset = new Vector2(18f, -1f),
        Damage = 1.5f,
        MagSize = 15f,
        ReloadTime = 1.5f,
        FireRate = 12.5f,
        BulletSpeed = 2000f,
        VelocityDecay = 0.2f,
        MinBulletSpeed = 600f,
        BulletScale = 0.6f,
        HitStop = 0.07f,
        Automatic = true,
        HitShakeStrength = 2.6f,
        CameraKickDistance = 3.4f,
        BulletsPerShot = 1,
        SpreadAngle = 0.2325f, //0.3325f
        BurstCount = 0,
        BurstDelay = 0f,
        RecoilRotationKick = 5.8f,
        RecoilReturnSpeed = 8.5f,
        EquippedMoveSpeedMultiplier = 0.9f,
        AimDrag = 1f,
        CanBounce = false,
        MaxBounces = 0,
    };

    // Assault Rifles
    public static Gun ScrapRifle = new Gun
    {
        AmmoType = AmmoType.Medium,
        SpriteName = "gun_scraprifle",
        DroppedSpriteName = "gun_scraprifle_drop",
        SpriteOrigin = new Vector2(-1f, 4f),
        MuzzleOffset = new Vector2(15f, -1f),
        Damage = 1.5f,
        MagSize = 20f,
        ReloadTime = 2.5f,
        FireRate = 9f,
        SpreadAngle = 0.21f,
        BulletSpeed = 1550f,
        VelocityDecay = 0.4f,
        MinBulletSpeed = 400f,
        BulletScale = 0.7f,
        HitStop = 0.09f,
        Automatic = true,
        UseSpeedVariation = false,
        HitShakeStrength = 3.8f,
        CameraKickDistance = 3.5f,
        BulletsPerShot = 1,
        BurstCount = 1,
        RecoilRotationKick = 6.2f,
        RecoilDistance = 2f,
        RecoilReturnSpeed = 7.2f,
        EquippedMoveSpeedMultiplier = 0.8f,
        AimDrag = 1.3f,
        CanBounce = false,
        MaxBounces = 0
    };

    public static Gun AssaultRifle = new Gun
    {
        AmmoType = AmmoType.Medium,
        SpriteName = "gun_assaultrifle",
        SpriteOrigin = new Vector2(0f, 0f),
        MuzzleOffset = new Vector2(0f, 0f),
        Damage = 2f,
        MagSize = 23f,
        ReloadTime = 2.1f,
        FireRate = 7f,
        SpreadAngle = 0.2f,
        BulletSpeed = 1150f,
        VelocityDecay = 0.4f,
        MinBulletSpeed = 400f,
        BulletScale = 0.7f,
        HitStop = 0.09f,
        Automatic = true,
        UseSpeedVariation = false,
        HitShakeStrength = 4f,
        CameraKickDistance = 7.8f,
        BulletsPerShot = 1,
        BurstCount = 1,
        RecoilRotationKick = 3f,
        RecoilReturnSpeed = 7.8f,
        EquippedMoveSpeedMultiplier = 0.96f,
        AimDrag = 1.05f,
        CanBounce = false
    };

    public static Gun SplitRifle = new Gun
    {
        AmmoType = AmmoType.Medium,
        SpriteName =  "gun_splitrifle",
        SpriteOrigin = new Vector2(0f, 0f),
        MuzzleOffset = new Vector2(0f, 0f),
        Damage = 2.2f,
        MagSize = 25f,
        ReloadTime = 2.5f,
        FireRate = 7f,
        SpreadAngle = 0.2f,
        BulletSpeed = 1150f,
        VelocityDecay = 0.4f,
        MinBulletSpeed = 400f,
        BulletScale = 0.7f,
        HitStop = 0.09f,
        Automatic = true,
        UseSpeedVariation = true,
        HitShakeStrength = 4.8f,
        CameraKickDistance = 8.8f,
        BulletsPerShot = 2,
        BurstCount = 1,
        RecoilRotationKick = 3.6f,
        RecoilReturnSpeed = 7f,
        EquippedMoveSpeedMultiplier = 0.94f,
        AimDrag = 1.15f,
        CanBounce = false
    };

    public static Gun HuntingRifle = new Gun
    {
        AmmoType = AmmoType.Medium,
        SpriteName =  "gun_scraprifle",
        SpriteOrigin = new Vector2(0f, 0f),
        MuzzleOffset = new Vector2(0f, 0f),
        Damage = 3.5f,
        MagSize = 12f,
        ReloadTime = 2.5f,
        FireRate = 1f,
        SpreadAngle =  0.1f,
        BulletSpeed = 1500f,
        VelocityDecay = 0.75f,
        MinBulletSpeed = 200f,
        BulletScale = 0.9f,
        HitStop = 0.2f,
        Automatic = false,
        UseSpeedVariation = false,
        HitShakeStrength = 10.5f,
        CameraKickDistance = 15f,
        BulletsPerShot = 1,
        BurstCount = 1,
        RecoilRotationKick = 5.4f,
        RecoilReturnSpeed = 2.8f,
        EquippedMoveSpeedMultiplier = 0.82f,
        AimDrag = 3.4f,
        CanBounce = false
    };
    
    // Burst rifles
    public static Gun BurstRifle = new Gun
    {
        AmmoType = AmmoType.Medium,
        SpriteName = "gun_scraprifle",
        SpriteOrigin = new Vector2(1f, 4f),
        MuzzleOffset = new Vector2(15f, -1f),
        Damage = 1.6f,
        MagSize = 18f,
        ReloadTime = 2f,
        FireRate = 1.5f,
        SpreadAngle = 0.185f,
        BulletSpeed = 1600f,
        VelocityDecay = 0.4f,
        MinBulletSpeed = 400f,
        BulletScale = 0.8f,
        HitStop = 0.075f,
        Automatic = true,
        UseSpeedVariation = false,
        HitShakeStrength = 5.2f,
        CameraKickDistance = 9.6f,
        BulletsPerShot = 1,
        BurstCount = 3,
        BurstDelay = 0.045f,
        RecoilDistance = 1.3f,
        RecoilRotationKick = 8.2f,
        RecoilReturnSpeed = 7f,
        EquippedMoveSpeedMultiplier = 0.75f,
        AimDrag = 1.2f,
        CanBounce = false
    };
    
    // Shotguns
    public static Gun Shotgun = new Gun
    {
        AmmoType = AmmoType.Heavy,
        SpriteName = "gun_shotgun",
        DroppedSpriteName = "gun_shotgun_drop",
        Damage = 1f,
        MagSize = 6f,
        ReloadTime = 2.5f,
        FireRate = 1.5f,
        SpreadAngle = 0.29f,
        BulletsPerShot = 5,
        BulletSpeed = 1850f,
        VelocityDecay = 5.5f,
        MinBulletSpeed = 200f,
        BulletScale = 0.55f,
        HitStop = 0.12f,
        Automatic = false,
        UseSpeedVariation = true,
        RecoilRotationKick = 6.2f,
        RecoilDistance = 4f,
        RecoilReturnSpeed = 2f,
        HitShakeStrength = 3.5f,
        CameraKickDistance = 13.5f,
        EquippedMoveSpeedMultiplier = 0.85f,
        AimDrag = 4f,
        CanBounce = false,
        MaxBounces = 0
    };

    
    public static readonly Gun[] EnemyGunPool =
    [
        VAL,
        ScrapRifle,
        Shotgun,
    ];

    public static Gun PickRandomEnemyGun(Random rng) =>
        EnemyGunPool[rng.Next(EnemyGunPool.Length)];
}
