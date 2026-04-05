using System.Drawing;

namespace StormShooter;

public class GunData
{
    private static float _globalBulletSpeed = 350f;
    private static float _hitStopTime = 0.05f;

    public static Gun BurstRifle = new Gun
    {
        Damage = 1f,
        MagSize = 20f,
        ReloadTime = 2f,

        FireRate = 2.5f,
        Spread = 0.08f,
        
        BulletSpeed = 800f, 
        VelocityDecay = 3.0f, 
        MinBulletSpeed = 50f,

        Automatic = false,

        ShakeStrength = 10f,
        ShakeDuration = 0.15f,
        Recoil = 6f,

        BulletsPerShot = 1,
        SpreadAngle = 0.1f,

        BurstCount = 3,
        BurstDelay = 0.06f,

        SpreadBloom = 0.01f,
        SpreadRecovery = 5f,

        RecoilPerShot = 1.2f,
        RecoilRecovery = 7f,
        
        CanBounce = true,
        MaxBounces = 3,

        HitStop = _hitStopTime
    };

    public static Gun Shotgun = new Gun
    {
        Damage = 1f,
        MagSize = 6f,
        ReloadTime = 2.5f,

        FireRate = 1.5f,
        Spread = 0f,
        SpreadAngle = 0.3f,
        BulletsPerShot = 6,

        BulletSpeed = 450f,
        VelocityDecay = 1.8f,
        MinBulletSpeed = 0f,

        Automatic = false,
        UseSpeedVariation = true,

        Recoil = 10f,
        RecoilPerShot = 4f,

        ShakeStrength = 40f,
        ShakeDuration = 0.4f,

        HitStop = _hitStopTime,

        CanBounce = true,
        MaxBounces = 10
    };

    public static Gun Smg = new Gun
    {
        Damage = 1f,
        MagSize = 20f,
        ReloadTime = 2f,

        FireRate = 7f,
        Spread = 0.05f,
        BulletSpeed = _globalBulletSpeed,
        Automatic = true,

        ShakeStrength = 5f,
        ShakeDuration = 0.015f,
        Recoil = 6f,

        BulletsPerShot = 1,
        SpreadAngle = 0.5f,

        BurstCount = 1,
        BurstDelay = 0.0f,

        SpreadBloom = 0.01f,
        SpreadRecovery = 5f,

        RecoilPerShot = 1.2f,
        RecoilRecovery = 7f,

        HitStop = _hitStopTime,

        CanBounce = true,
        MaxBounces = 5
    };
}