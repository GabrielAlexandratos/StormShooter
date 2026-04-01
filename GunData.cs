using System.Drawing;

namespace StormShooter;

public class GunData
{
    private static float _globalBulletSpeed = 350f;
    private static float _hitStopTime = 0.05f;
    
    public static Gun Pistol = new Gun
    {
        Damage = 1f,
        ReloadTime = 2f,
        MagSize = 12f,
        
        FireRate = 8.5f,
        Spread = 0.14f,
        BulletSpeed = _globalBulletSpeed,
        Automatic = false,
        
        ShakeStrength = 7.5f,
        ShakeDuration = 0.135f,
        Recoil = 6.5f,

        BulletsPerShot = 1,
        SpreadAngle = 0.05f,

        BurstCount = 1,
        BurstDelay = 0f,

        SpreadBloom = 0.02f,
        SpreadRecovery = 4f,

        RecoilPerShot = 1.5f,
        RecoilRecovery = 6f,
        
        HitStop = _hitStopTime
    };

    public static Gun BurstRifle = new Gun
    {
        Damage = 1f,
        MagSize = 20f,
        ReloadTime = 2f,
        
        FireRate = 2.5f,
        Spread = 0.08f,
        BulletSpeed = _globalBulletSpeed,
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
        
        HitStop = _hitStopTime
    };

    public static Gun Smg = new Gun
    {
        Damage = 1f,
        MagSize = 20f,
        ReloadTime = 2f,
        
        FireRate = 9f,
        Spread = 0.5f,
        BulletSpeed = _globalBulletSpeed,
        Automatic = true,
        
        ShakeStrength = 30f,
        ShakeDuration = 0.15f,
        Recoil = 6f,

        BulletsPerShot = 20,
        SpreadAngle = 10f,

        BurstCount = 10,
        BurstDelay = 0.06f,

        SpreadBloom = 0.01f,
        SpreadRecovery = 5f,

        RecoilPerShot = 1.2f,
        RecoilRecovery = 7f,
        
        HitStop = _hitStopTime,
        
        CanBounce =  true,
        MaxBounces = 999
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

        BulletSpeed = 400f,

        Automatic = false,
        UseSpeedVariation = true,

        Recoil = 10f,
        RecoilPerShot = 4f,

        VelocityDecay = 3.5f,
        MinBulletSpeed = 0f,

        ShakeStrength = 15f,
        ShakeDuration = 0.2f,

        HitStop = _hitStopTime,
        
        CanBounce = true,
        MaxBounces = 10
    };
}