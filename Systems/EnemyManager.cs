using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StormShooter;

public enum EnemyType { Test, Basic }

public class EnemyManager
{
    public List<Enemy> Enemies { get; } = new();

    public Func<Vector2, bool> IsWall { get; set; }
    public BulletManager Bullets { get; set; }
    public ParticleSystem Particles { get; set; }
    public LightingRenderer Lighting { get; set; }
    public Random Rng { get; set; } = new();
    public Texture2D EnemyIdleTexture { get; set; }
    public Texture2D EnemyWalkTexture { get; set; }
    public Func<Gun, Texture2D> GetGunTexture { get; set; }

    public event Action<Vector2, Gun, int> OnEnemyDropped;

    public event Action<Vector2> OnLastEnemyKilled;

    private const float DropChance = 0.4f;

    private bool HasLineOfSight(Vector2 a, Vector2 b)
    {
        Vector2 dir = b - a;
        float len = dir.Length();
        if (len < 0.001f) return true;
        dir /= len;
        int steps = (int)(len / 8f) + 1;
        for (int i = 1; i < steps; i++)
        {
            if (IsWall != null && IsWall(a + dir * (i * 8f)))
                return false;
        }
        return true;
    }

    public void AddEnemy(Vector2 position, EnemyType type = EnemyType.Basic, Gun gun = null)
    {
        var e = new Enemy { Position = position, Gun = gun ?? GunData.PickRandomEnemyGun(Rng) };
        e.InitAI(Rng, IsWall, HasLineOfSight);
        e.InitSprite(EnemyIdleTexture, EnemyWalkTexture);
        Enemies.Add(e);
    }

    public void Update(float dt, Vector2 playerPos, LightingRenderer lighting)
    {
        foreach (var e in Enemies)
        {
            e.Update(dt);
            e.UpdateAI(dt, playerPos, (muzzlePos, dir, spread) =>
            {
                if (Bullets == null) return;
                float angle = MathF.Atan2(dir.Y, dir.X) + spread;
                Vector2 fired = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                Bullets.FireEnemyBullet(muzzlePos, fired, e.Gun, Rng);
            });
            e.UpdateAnimation(dt);
            lighting?.AddLight(new LightSource(e.Position, 45f, Color.White * 1.5f, 0.15f));
        }
        Vector2 lastDeadPos = Vector2.Zero;
        bool anyDied = false; 
        foreach (var e in Enemies)
        {
            if (e.IsDead())
            {
                SoundManager.Play("humanhit1", 1f, (float)(Rng.NextDouble() - 0.5) * 0.5f);
                OnEnemyDropped?.Invoke(e.Position, e.Gun, DroppedGun.RollDropAmmo(Rng));
                lastDeadPos = e.Position;
                anyDied = true;
            }   
        }   
  
        Enemies.RemoveAll(e => e.IsDead());
  
        if (anyDied && Enemies.Count == 0)
            OnLastEnemyKilled?.Invoke(lastDeadPos);
    }

    public void Update(float dt, LightingRenderer lighting) => Update(dt, Vector2.Zero, lighting);

    public void Draw(SpriteBatch spriteBatch, Texture2D fallbackPixel)
    {
        foreach (var e in Enemies)
            e.Draw(spriteBatch, fallbackPixel, GetGunTexture?.Invoke(e.Gun));
    }
}