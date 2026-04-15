using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace StormShooter;

public class EnemySpawner
{
    private readonly Tile[,] _grid;
    private readonly int _tileSize;
    private readonly Random _random = new();

    public EnemySpawner(Tile[,] grid, int tileSize)
    {
        _grid = grid;
        _tileSize = tileSize;
    }

    public void Spawn(List<Room> rooms, EnemyManager manager, Vector2 playerPos)
    {
        int clusterCount = Math.Min(rooms.Count, 4);

        List<Room> safe = new();
        List<Room> fallback = new();

        foreach (var room in rooms)
        {
            if (Vector2.Distance(room.Position * _tileSize, playerPos) < 300f)
                fallback.Add(room);
            else
                safe.Add(room);
        }

        safe.Sort((a, b) =>
            Vector2.Distance(a.Position * _tileSize, playerPos)
                .CompareTo(Vector2.Distance(b.Position * _tileSize, playerPos)));

        List<Room> chosen = new();

        if (safe.Count >= clusterCount)
        {
            float bucketSize = (float)safe.Count / clusterCount;
            for (int i = 0; i < clusterCount; i++)
            {
                int start = (int)(i * bucketSize);
                int end   = (int)((i + 1) * bucketSize);
                end = Math.Min(end, safe.Count);
                if (start >= end) start = Math.Max(0, end - 1);
                chosen.Add(safe[_random.Next(start, end)]);
            }
        }
        else
        {
            chosen.AddRange(safe);
            fallback.Sort((a, b) =>
                Vector2.Distance(a.Position * _tileSize, playerPos)
                    .CompareTo(Vector2.Distance(b.Position * _tileSize, playerPos)));

            for (int i = fallback.Count - 1; i >= 0 && chosen.Count < clusterCount; i--)
            {
                if (!chosen.Contains(fallback[i]))
                    chosen.Add(fallback[i]);
            }
        }

        foreach (var room in chosen)
            SpawnCluster(room, manager);
    }

    private void SpawnCluster(Room room, EnemyManager manager)
    {
        int count = _random.Next(2, 5);
        int attempts = 0;
        int spawned = 0;

        while (spawned < count && attempts < count * 35)
        {
            attempts++;
            float angle = (float)(_random.NextDouble() * Math.PI * 2);
            
            
            float radius = (float)_random.NextDouble() * room.Radius;
            Vector2 pos = (room.Position * _tileSize) + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius * _tileSize;

            int tx = (int)(pos.X / _tileSize);
            int ty = (int)(pos.Y / _tileSize);

            if (tx < 0 || ty < 0 || tx >= _grid.GetLength(0) || ty >= _grid.GetLength(1)) continue;

            if (_grid[tx, ty].Type == TileType.Empty && IsFarFromOthers(pos, manager))
            {
                manager.AddEnemy(pos, EnemyType.Basic, GunData.EnemyRifle);
                spawned++;
            }
        }
    }

    private bool IsFarFromOthers(Vector2 pos, EnemyManager manager)
    {
        foreach (var e in manager.Enemies)
        {
            if (Vector2.Distance(e.Position, pos) < 50f) return false;
        }
        return true;
    }
}
