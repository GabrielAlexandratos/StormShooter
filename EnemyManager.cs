using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StormShooter;

public class EnemyManager
{
    private readonly List<Enemy> _enemies = new();

    public List<Enemy> Enemies => _enemies;

    public void Add(Enemy enemy)
    {
        _enemies.Add(enemy);
    }

    public void Update(float dt, LightingRenderer lighting)
    {
        foreach (var e in _enemies)
        {
            e.Update(dt);

            // constant light
            lighting.AddLight(new LightSource(
                e.Position,
                45f,
                Color.White * 1.5f,
                0.15f));
        }

        _enemies.RemoveAll(e => e.IsDead());
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (var e in _enemies)
            e.Draw(spriteBatch, pixel);
    }
}