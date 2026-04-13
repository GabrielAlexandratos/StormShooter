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
    public LightingRenderer Lighting  { get; set; }
    public Random Rng { get; set; } = new();

    private readonly Gun _enemyGun = GunData.VAL;

    private bool HasLOS(Vector2 a, Vector2 b)
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

    public void Add(Enemy enemy)
    {
        enemy.InitAI(Rng, IsWall, HasLOS);
        Enemies.Add(enemy);
    }

    public void AddEnemy(Vector2 position, EnemyType type = EnemyType.Basic, Gun gun = null)
    {
        var e = new Enemy { Position = position, Gun = gun ?? _enemyGun };
        e.InitAI(Rng, IsWall, HasLOS);
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
                float   angle = MathF.Atan2(dir.Y, dir.X) + spread;
                Vector2 fired = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                Bullets.FireEnemyBullet(muzzlePos, fired, e.Gun, Rng);
            });
            lighting?.AddLight(new LightSource(e.Position, 45f, Color.White * 1.5f, 0.15f));
        }
        Enemies.RemoveAll(e => e.IsDead());
    }

    public void Update(float dt, LightingRenderer lighting) => Update(dt, Vector2.Zero, lighting);

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (var e in Enemies)
            e.Draw(spriteBatch, pixel);
    }
}