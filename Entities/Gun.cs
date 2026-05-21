namespace StormShooter;

using Microsoft.Xna.Framework;

public enum AmmoType { Light, Medium, Heavy }

public class Gun
{
    public AmmoType AmmoType = AmmoType.Medium;
    public float Damage = 1f; // damage per bullet
    public float MagSize = 12f; // total mag size
    public float ReloadTime = 1f; // time to reload
    public float HitStop = 0.05f; // how long the game stops when an enemy is killed
    public float FireRate = 3f; // fire rate
    public float BulletSpeed = 700f; // speed that bullets travel
    public bool Automatic = false; // can the mouse be held down to shoot the gun continuously
    public float HitShakeStrength; // strength of the shake applied when this gun's bullets hit an enemy
    public float CameraKickDistance; // backward camera kick applied when the gun is fired
    public int BulletsPerShot = 1; // how many bullets are shot per click
    public float SpreadAngle = 0f; // the accuracy of the bullets
    public float VelocityDecay = 0f; // how fast does the bullet loose speed
    public float MinBulletSpeed = 0f; // despawn bullets if they are moving too slow
    public int BurstCount = 1; // how many times should the gun be shot per click
    public float BurstDelay = 0f; // time in between bursts
    public float RecoilDistance = 2f; // how far the gun sprite moves backward per shot
    public float RecoilRotationKick = 2f; // how much the gun sprite rotates per shot
    public float RecoilReturnSpeed = 10f; // how quickly gun recoil settles back to neutral
    public float BulletScale = 1f; // how big the bullet is
    public bool UseSpeedVariation = false; // should each bullet that comes out of the gun have a slight variation in speed
    public bool CanBounce = false; // can the bullets bounce
    public int MaxBounces = 0; // how many times can they bounce before despawning
    public string SpriteName = "gun_smg";
    public Vector2 SpriteOrigin = new Vector2(1f, 4f); // offseting the sprite so that it looks more natural to be held
    public Vector2 MuzzleOffset = new Vector2(15f, -1f); // positioning the fire point
    public float SpriteScale = 1f;
    public float EquippedMoveSpeedMultiplier = 1f; // how much the gun slows the player while held
    public float AimDrag = 1f; // how much the gun drags behind the cursor
    public string ShotSound = "pistolshot";
    public string ReloadSound = "akreload";
}
