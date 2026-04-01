namespace StormShooter;

public class Gun
{
    public float Damage;
    public float MagSize;
    public float ReloadTime;
    public float HitStop;
    public float FireRate;
    public float Spread;
    public float BulletSpeed;
    public bool Automatic;
    public float ShakeStrength;
    public float ShakeDuration;
    public float Recoil;
    public int BulletsPerShot = 1;
    public float SpreadAngle = 0f;
    public float VelocityDecay = 0f;
    public float MinBulletSpeed = 0f;
    public int BurstCount = 1;
    public float BurstDelay = 0f;
    public float RecoilPerShot = 0f;
    public float RecoilRecovery = 0f;
    public float SpreadBloom = 0f;
    public float SpreadRecovery = 0f;
    public float DamageFalloffStart = 0f;
    public float DamageFalloffEnd = 0f;
    public float BulletScale = 1.5f;
    public bool UseSpeedVariation = false;
}