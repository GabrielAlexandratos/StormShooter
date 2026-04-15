namespace StormShooter;

using Microsoft.Xna.Framework;

public class GunData
{
    public static Gun AssaultRifle = new Gun
    {
        Damage = 1f,
        MagSize = 11f,
        ReloadTime = 2f,
        FireRate = 4f,
        BulletSpeed = 1000f,
        VelocityDecay = 1f,
        MinBulletSpeed = 50f,
        Automatic = true,
        ShakeStrength = 1.4f,
        kickBack = 2.5f,
        BulletsPerShot = 1,
        SpreadAngle = 0.1f,
        BurstCount = 0,
        BurstDelay = 0f,
        RecoilPerShot = 1.2f,
        RecoilRecovery = 7f,
        CanBounce = false,
        MaxBounces = 0,
    };

    public static Gun VAL = new Gun
    {
        SpriteName = "gun_asval",
        Damage = 1f,
        MagSize = 24f,
        ReloadTime = 1.5f,
        FireRate = 15f,
        BulletSpeed = 1100f,
        VelocityDecay = 2f,
        MinBulletSpeed = 50f,
        Automatic = true,
        ShakeStrength = 1.5f,
        kickBack = 2f,
        BulletsPerShot = 1,
        SpreadAngle = 0.4f,
        BurstCount = 0,
        BurstDelay = 0f,
        RecoilPerShot = 1.2f,
        RecoilRecovery = 7f,
        CanBounce = true,
        MaxBounces = 10,
    };

    public static Gun Pistol = new Gun
    {
        Damage = 1f,
        MagSize = 11f,
        ReloadTime = 0.8f,
        FireRate = 7f,
        BulletSpeed = 820f,
        VelocityDecay = 2f,
        MinBulletSpeed = 50f,
        Automatic = false,
        ShakeStrength = 1.5f,
        kickBack = 2.5f,
        BulletsPerShot = 1,
        SpreadAngle = 0.2f,
        BurstCount = 0,
        BurstDelay = 0f,
        RecoilPerShot = 1.2f,
        RecoilRecovery = 7f,
        CanBounce = false,
        MaxBounces = 0,
    };

    public static Gun BurstRifle = new Gun
    {
        Damage = 1f,
        MagSize = 20f,
        ReloadTime = 2f,
        FireRate = 2.5f,
        BulletSpeed = 900f,
        VelocityDecay = 2.5f,
        MinBulletSpeed = 50f,
        Automatic = false,
        ShakeStrength = 1.5f,
        kickBack = 2.1f,
        BulletsPerShot = 1,
        SpreadAngle = 0.1f,
        BurstCount = 3,
        BurstDelay = 0.06f,
        RecoilPerShot = 1.2f,
        RecoilRecovery = 7f,
        CanBounce = true,
        MaxBounces = 10,
    };

    public static Gun ScrapRifle = new Gun
    {
        SpriteName = "gun_scraprifle",
        SpriteOrigin = new Vector2(1f, 4f),
        MuzzleOffset = new Vector2(15f, -1f),
        Damage = 1.5f,
        MagSize = 25f,
        ReloadTime = 1.8f,
        FireRate = 8f,
        SpreadAngle = 0.32f,
        BulletSpeed = 975f,
        VelocityDecay = 2.3f,
        MinBulletSpeed = 50f,
        Automatic = true,
        UseSpeedVariation = true,
        ShakeStrength = 1.5f,
        kickBack = 3.1f,
        BulletsPerShot = 1,
        BurstCount = 1,
        RecoilPerShot = 2f,
        RecoilRecovery = 8f,
        CanBounce = true,
        MaxBounces = 10
    };
    
    public static Gun EnemyRifle = new Gun
    {
        SpriteName = "gun_scraprifle",
        SpriteOrigin = new Vector2(1f, 4f),
        MuzzleOffset = new Vector2(15f, -1f),
        Damage = 1.5f,
        MagSize = 25f,
        ReloadTime = 1.8f,
        FireRate = 6f,
        SpreadAngle = 0.32f,
        BulletSpeed = 905f,
        VelocityDecay = 2.3f,
        MinBulletSpeed = 50f,
        Automatic = true,
        UseSpeedVariation = true,
        ShakeStrength = 1.5f,
        kickBack = 3.1f,
        BulletsPerShot = 1,
        BurstCount = 1,
        RecoilPerShot = 2f,
        RecoilRecovery = 8f,
        CanBounce = true,
        MaxBounces = 2
    };

    public static Gun LongGun = new Gun
    {
        Damage = 5f,
        MagSize = 5f,
        ReloadTime = 4f,
        FireRate = 1f,
        SpreadAngle = 0.02f,
        BulletSpeed = 1500f,
        VelocityDecay = 0f,
        Automatic = false,
        ShakeStrength = 5f,
        kickBack = 30f,
        CanBounce = false
    };

    public static Gun Shotgun = new Gun
    {
        SpriteName = "gun_shotgun",
        Damage = 1f,
        MagSize = 6f,
        ReloadTime = 2.5f,
        FireRate = 1.5f,
        SpreadAngle = 0.3f,
        BulletsPerShot = 6,
        BulletSpeed = 800f,
        VelocityDecay = 2.3f,
        MinBulletSpeed = 0f,
        Automatic = false,
        UseSpeedVariation = true,
        RecoilPerShot = 4f,
        ShakeStrength = 4.2f,
        kickBack = 10f,
        CanBounce = true,
        MaxBounces = 10
    };
}
