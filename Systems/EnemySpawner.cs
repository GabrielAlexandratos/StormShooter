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
        int clusterCount = Math.Min(rooms.Count, 6);
        List<Room> candidates = new();

        foreach (var room in rooms)
        {
            if (Vector2.Distance(room.Position * _tileSize, playerPos) < 120f)
                continue;
            candidates.Add(room);
        }

        if (candidates.Count == 0) candidates = new List<Room>(rooms);

        for (int i = 0; i < clusterCount && candidates.Count > 0; i++)
        {
            int index = _random.Next(candidates.Count);
            var room = candidates[index];
            SpawnCluster(room, manager);
            candidates.RemoveAt(index);
        }
    }

    private void SpawnCluster(Room room, EnemyManager manager)
    {
        int count = _random.Next(2, 4);
        int attempts = 0;
        int spawned = 0;

        while (spawned < count && attempts < count * 10)
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
                manager.AddEnemy(pos, EnemyType.Basic);
                spawned++;
            }
        }
    }

    private bool IsFarFromOthers(Vector2 pos, EnemyManager manager)
    {
        foreach (var e in manager.Enemies)
        {
            if (Vector2.Distance(e.Position, pos) < 30f) return false;
        }
        return true;
    }
}