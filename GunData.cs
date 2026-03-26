namespace StormShooter;

public class GunData
{
    public static Gun Pistol = new Gun
    {
        FireRate = 8.5f,
        Spread = 0.14f,
        BulletSpeed = 240f,
        Automatic = false,
        
        ShakeStrength = 7.5f,
        ShakeDuration = 0.135f,
        Recoil = 4.5f
    };

    public static Gun Rifle = new Gun
    {
        FireRate = 6.5f,
        Spread = 0.2f,
        BulletSpeed = 240f,
        Automatic = true,
        
        ShakeStrength = 4.6f,
        ShakeDuration = 0.1f,
        Recoil = 3f
    };
}