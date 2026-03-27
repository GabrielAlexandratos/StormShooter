using System.Drawing;

namespace StormShooter;

public class GunData
{
    private static float _globalBulletSpeed = 390f;
    
    public static Gun Pistol = new Gun
    {
        Damage = 2.5f,
        ReloadTime = 2f,
        MagSize = 12f,
        
        FireRate = 8.5f,
        Spread = 0.14f,
        BulletSpeed = _globalBulletSpeed,
        Automatic = false,
        
        ShakeStrength = 7.5f,
        ShakeDuration = 0.135f,
        Recoil = 6.5f,
        
        HitStop = 0.03f
    };

    public static Gun Rifle = new Gun
    {
        Damage = 2f,
        MagSize = 20f,
        ReloadTime = 2f,
        
        FireRate = 6.5f,
        Spread = 0.12f,
        BulletSpeed = _globalBulletSpeed,
        Automatic = true,
        
        ShakeStrength = 6f,
        ShakeDuration = 0.1f,
        Recoil = 6f,
        
        HitStop = 0.03f
    };

    public static Gun Smg = new Gun
    {
        Damage = 1.5f,
        MagSize = 30f,
        ReloadTime = 2.5f,
        
        FireRate = 10.5f,
        Spread = 0.3f,
        BulletSpeed = _globalBulletSpeed,
        Automatic = true,
        
        ShakeStrength = 5f,
        ShakeDuration = 0.085f,
        Recoil = 8f,
        
        HitStop = 0.03f
    };
}