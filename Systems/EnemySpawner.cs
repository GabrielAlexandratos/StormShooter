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
        if (rooms.Count == 0) return;

        Room safeRoom = ClosestRoom(rooms, playerPos);

        const int maxTotal = 6;
        int totalSpawned = 0;

        foreach (var room in rooms)
        {
            if (totalSpawned >= maxTotal) break;
            if (room == safeRoom) continue;

            float dist = Vector2.Distance(room.Center * _tileSize, playerPos);
            if (dist < 140f) continue; // buffer around spawn

            // 1 enemy in nearby rooms, chance of 2 only in distant ones
            int count = dist > 350f && _random.NextDouble() < 0.4 ? 2 : 1;
            count = Math.Min(count, maxTotal - totalSpawned);

            SpawnInRoom(room, count, manager);
            totalSpawned += count;
        }
    }

    private void SpawnInRoom(Room room, int count, EnemyManager manager)
    {
        int spawned = 0;
        int attempts = 0;

        while (spawned < count && attempts < count * 20)
        {
            attempts++;
            int tx = _random.Next(room.Bounds.X + 1, room.Bounds.X + room.Bounds.Width  - 1);
            int ty = _random.Next(room.Bounds.Y + 1, room.Bounds.Y + room.Bounds.Height - 1);

            if (_grid[tx, ty].Type != TileType.Empty) continue;

            Vector2 pos = new Vector2(tx * _tileSize + _tileSize / 2f, ty * _tileSize + _tileSize / 2f);
            if (!IsFarFromOthers(pos, manager)) continue;

            manager.AddEnemy(pos, EnemyType.Basic, GunData.EnemyRifle);
            spawned++;
        }
    }

    private Room ClosestRoom(List<Room> rooms, Vector2 worldPos)
    {
        Room closest = rooms[0];
        float best = float.MaxValue;
        foreach (var r in rooms)
        {
            float d = Vector2.Distance(r.Center * _tileSize, worldPos);
            if (d < best) { best = d; closest = r; }
        }
        return closest;
    }

    private bool IsFarFromOthers(Vector2 pos, EnemyManager manager)
    {
        foreach (var e in manager.Enemies)
            if (Vector2.Distance(e.Position, pos) < 40f) return false;
        return true;
    }
}
