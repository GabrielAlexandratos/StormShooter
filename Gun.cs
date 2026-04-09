namespace StormShooter;

using Microsoft.Xna.Framework;

public class Gun
{
    public float Damage = 1f; // damage per bullet
    public float MagSize = 12f; // total mag size
    public float ReloadTime = 1f; // time to reload
    public float HitStop = 0.05f; // time that game stops when an enemy is killed
    public float FireRate = 3f; // fire rate
    public float BulletSpeed = 700f; // speed that bullets travel
    public bool Automatic = false; // can the mouse be held down to shoot the gun continuously
    public float ShakeStrength; // strength of the random screen shake
    public float kickBack; // distance that the screen is displaced in opposite direction to gun shot
    public float ShakeDuration = 0.05f; // how long the screen shakes for randomly
    public int BulletsPerShot = 1; // how many bullets are shot on mouse click
    public float SpreadAngle = 0f; // the accuracy of the bullets
    public float VelocityDecay = 0f; // how fast does the bullet loose speed if at all
    public float MinBulletSpeed = 0f; // if a bullet is less than the minimum then it despawns
    public int BurstCount = 1; // how many times should the gun be shot per mouse click
    public float BurstDelay = 0f; // time inbetween bursts
    public float recoil = 2f; // amount the gun sprite is kicked back on a shot
    public float RecoilPerShot = 0f; // unused right now
    public float RecoilRecovery = 0f; // unused right now
    public float BulletScale = 1f; // size of bullet (try not to edit per gun)
    public bool UseSpeedVariation = false; // should each bullet that comes out of the gun have a slight variation in speed
    public bool CanBounce = false; // can the bullets from the gun bounce
    public int MaxBounces = 5; // how many times can they bounce max before despawning
    public string SpriteName = "gun_smg";
    public Vector2 SpriteOrigin = new Vector2(1f, 4f);
    public Vector2 MuzzleOffset = new Vector2(15f, -1f); // positioning the fire point 
    public float SpriteScale = 1f;
}
