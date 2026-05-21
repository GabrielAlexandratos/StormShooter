using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StormShooter;

public class DroppedGun
{
    public Vector2 Position;
    public Gun Gun;
    public int AmmoCount;

    private float _interactTimer;

    public const float PickupRadius = 20f;
    public const float InteractDuration = 1.25f;

    public const int MinDropAmmo = 5;
    public const int MaxDropAmmo = 13;
    public static int RollDropAmmo(Random rng) =>
        rng.Next(MinDropAmmo, MaxDropAmmo);

    public float InteractProgress => _interactTimer / InteractDuration;
    public bool IsBeingInteracted => _interactTimer > 0f;
    public bool InRange { get; private set; }

    public DroppedGun(Vector2 position, Gun gun, int ammoCount)
    {
        Position = position;
        Gun = gun;
        AmmoCount = ammoCount;
    }

    public bool Update(float dt, Vector2 playerPos, bool fHeld)
    {
        InRange = Vector2.Distance(playerPos, Position) <= PickupRadius;

        if (InRange && fHeld)
        {
            _interactTimer += dt;
            if (_interactTimer >= InteractDuration)
                return true;
        }
        else
        {
            _interactTimer = 0f;
        }

        return false;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D dropSprite, Texture2D pixel, float scale, Vector2 cameraPos, Rectangle destRect)
    {
        Vector2 screenPos = new Vector2(
            destRect.X + (Position.X - cameraPos.X) * scale,
            destRect.Y + (Position.Y - cameraPos.Y) * scale
        );

        // spawn sprite
        int spriteSize = (int)(8 * scale);
        Rectangle drawRect = new Rectangle(
            (int)screenPos.X - spriteSize / 2,
            (int)screenPos.Y - spriteSize / 2,
            spriteSize, spriteSize
        );
        spriteBatch.Draw(dropSprite, drawRect, Color.White);

        // interaction progress bar
        if (IsBeingInteracted)
        {
            int barW = (int)(24 * scale);
            int barH = (int)(3 * scale);
            int barX = (int)screenPos.X - barW / 2;
            int barY = (int)screenPos.Y - spriteSize / 2 - barH - (int)(3 * scale);
            int fillW = (int)(barW * InteractProgress);

            spriteBatch.Draw(pixel, new Rectangle(barX, barY, barW, barH), Color.Black * 0.6f);
            spriteBatch.Draw(pixel, new Rectangle(barX, barY, fillW, barH), Color.White);
        }
    }
}
