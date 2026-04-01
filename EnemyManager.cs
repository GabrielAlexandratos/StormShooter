using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StormShooter;

public enum EnemyType { Test, Basic }

public class EnemyManager
{
    public List<Enemy> Enemies { get; } = new();

    public void Add(Enemy enemy) => Enemies.Add(enemy);

    public void AddEnemy(Vector2 position, EnemyType type = EnemyType.Basic) => Enemies.Add(new Enemy(position));

    public void Update(float dt, LightingRenderer lighting)
    {
        foreach (var e in Enemies)
        {
            e.Update(dt);
            lighting.AddLight(new LightSource(e.Position, 45f, Color.White * 1.5f, 0.15f));
        }

        Enemies.RemoveAll(e => e.IsDead());
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (var e in Enemies)
            e.Draw(spriteBatch, pixel);
    }
}