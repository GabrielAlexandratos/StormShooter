using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StormShooter;

public enum DropInteractResult { None, PickedUp, Unloaded }

public class DroppedGun
{
    public Vector2 Position;
    public Gun Gun;
    public int AmmoCount;
    public float Rotation;

    public const float PickupRadius = 20f;
    public const float UnloadDuration = 1.25f;

    public const int MinDropAmmo = 5;
    public const int MaxDropAmmo = 13;
    public static int RollDropAmmo(Random rng) =>
        rng.Next(MinDropAmmo, MaxDropAmmo);

    private float _unloadTimer;
    public float UnloadProgress => _unloadTimer / UnloadDuration;
    public bool IsUnloading => _unloadTimer > 0f;
    public bool InRange { get; private set; }

    public DroppedGun(Vector2 position, Gun gun, int ammoCount)
    {
        Position = position;
        Gun = gun;
        AmmoCount = ammoCount;
    }

    public DropInteractResult Update(float dt, Vector2 playerPos, bool fPressed, bool gHeld)
    {
        InRange = Vector2.Distance(playerPos, Position) <= PickupRadius;

        if (!InRange)
        {
            _unloadTimer = 0f;
            return DropInteractResult.None;
        }

        if (fPressed)
        {
            _unloadTimer = 0f;
            return DropInteractResult.PickedUp;
        }

        if (gHeld && AmmoCount > 0)
        {
            _unloadTimer += dt;
            if (_unloadTimer >= UnloadDuration)
            {
                _unloadTimer = 0f;
                return DropInteractResult.Unloaded;
            }
        }
        else
        {
            _unloadTimer = 0f;
        }

        return DropInteractResult.None;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D dropSprite, Texture2D pixel, float scale, Vector2 cameraPos, Rectangle destRect)
    {
        Vector2 screenPos = new Vector2(
            destRect.X + (Position.X - cameraPos.X) * scale,
            destRect.Y + (Position.Y - cameraPos.Y) * scale
        );

        int spriteSize = (int)(8 * scale);
        Rectangle drawRect = new Rectangle(
            (int)screenPos.X - spriteSize / 2,
            (int)screenPos.Y - spriteSize / 2,
            spriteSize, spriteSize
        );
        spriteBatch.Draw(dropSprite, drawRect, Color.White);
    }
}
