using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace StormShooter;

public class LevelGenerator
{
    private int _width;
    private int _height;
    private Random _rng;

    private const int TILE_SIZE = 16;

    private const float ChanceChangeDir = 0.4f;
    private const float ChanceSpawn = 0.03f;
    private const float ChanceDestroy = 0.10f;
    private const int MaxWalkers = 6;
    private const float PercentToFill = 0.350f;

    public List<Room> Rooms { get; private set; } = new();
    public List<SpawnPoint> SpawnPoints = new();

    public TileType[,] Generate(int width, int height, int seed = -1)
    {
        _width = width;
        _height = height;
        _rng = seed == -1 ? new Random() : new Random(seed);

        var grid = new TileType[width, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = TileType.Wall;

        CarveFloors(grid);
        BuildRoomsFromFloors(grid);
        BuildSpawnPoints(grid);

        return grid;
    }
    
    private struct Walker
    {
        public Vector2 Pos;
        public Vector2 Dir;
    }

    private void CarveFloors(TileType[,] grid)
    {
        var walkers = new List<Walker>();

        walkers.Add(new Walker
        {
            Pos = new Vector2(_width / 2f, _height / 2f),
            Dir = RandomDir()
        });

        int iterations = 0;
        int maxIterations = 100_000;

        do
        {
            foreach (var w in walkers)
                grid[(int)w.Pos.X, (int)w.Pos.Y] = TileType.Empty;

            int checks = walkers.Count;
            for (int i = 0; i < checks; i++)
            {
                if (walkers.Count > 1 && _rng.NextDouble() < ChanceDestroy)
                {
                    walkers.RemoveAt(i);
                    break;
                }
            }
            
            for (int i = 0; i < walkers.Count; i++)
            {
                if (_rng.NextDouble() < ChanceChangeDir)
                {
                    var w = walkers[i];
                    w.Dir = RandomDir();
                    walkers[i] = w;
                }
            }

            checks = walkers.Count;
            for (int i = 0; i < checks; i++)
            {
                if (walkers.Count < MaxWalkers && _rng.NextDouble() < ChanceSpawn)
                {
                    walkers.Add(new Walker { Pos = walkers[i].Pos, Dir = RandomDir() });
                }
            }

            for (int i = 0; i < walkers.Count; i++)
            {
                var w = walkers[i];
                w.Pos += w.Dir;
                
                w.Pos.X = MathHelper.Clamp(w.Pos.X, 1, _width  - 2);
                w.Pos.Y = MathHelper.Clamp(w.Pos.Y, 1, _height - 2);
                walkers[i] = w;
            }

            iterations++;

            if (FloorFraction(grid) >= PercentToFill)
                break;

        } while (iterations < maxIterations);
    }

    private Vector2 RandomDir()
    {
        return (_rng.Next(4)) switch
        {
            0 => new Vector2( 0, -1),
            1 => new Vector2( 0,  1),
            2 => new Vector2(-1,  0),
            _ => new Vector2( 1,  0),
        };
    }

    private float FloorFraction(TileType[,] grid)
    {
        int count = 0;
        foreach (var t in grid)
            if (t == TileType.Empty) count++;
        return (float)count / grid.Length;
    }
    
    private void BuildRoomsFromFloors(TileType[,] grid)
    {
        Rooms.Clear();
        var visited = new bool[_width, _height];

        for (int x = 1; x < _width - 1; x++)
        {
            for (int y = 1; y < _height - 1; y++)
            {
                if (grid[x, y] != TileType.Empty || visited[x, y]) continue;

                var blob = new List<Vector2>();
                var queue = new Queue<(int, int)>();
                queue.Enqueue((x, y));
                visited[x, y] = true;

                while (queue.Count > 0)
                {
                    var (cx, cy) = queue.Dequeue();
                    blob.Add(new Vector2(cx, cy));

                    foreach (var (nx, ny) in new[]{(cx+1,cy),(cx-1,cy),(cx,cy+1),(cx,cy-1)})
                    {
                        if (nx < 0 || ny < 0 || nx >= _width || ny >= _height) continue;
                        if (visited[nx, ny] || grid[nx, ny] != TileType.Empty) continue;
                        visited[nx, ny] = true;
                        queue.Enqueue((nx, ny));
                    }
                }

                if (blob.Count < 6) continue;

                Vector2 centroid = Vector2.Zero;
                foreach (var p in blob) centroid += p;
                centroid /= blob.Count;

                int radius = (int)MathF.Sqrt(blob.Count / MathF.PI);

                Rooms.Add(new Room
                {
                    Position = centroid,
                    Radius   = Math.Max(1, radius)
                });
            }
        }
    }

    private void BuildSpawnPoints(TileType[,] grid)
    {
        SpawnPoints.Clear();

        for (int x = 1; x < _width - 1; x++)
        {
            for (int y = 1; y < _height - 1; y++)
            {
                if (grid[x, y] != TileType.Empty) continue;

                int walls = 0;
                if (grid[x + 1, y] == TileType.Wall) walls++;
                if (grid[x - 1, y] == TileType.Wall) walls++;
                if (grid[x, y + 1] == TileType.Wall) walls++;
                if (grid[x, y - 1] == TileType.Wall) walls++;

                Vector2 worldPos = new Vector2(x * TILE_SIZE, y * TILE_SIZE);

                if (walls == 1)
                    SpawnPoints.Add(new SpawnPoint(worldPos, SpawnType.Edge));
                else if (walls >= 2)
                    SpawnPoints.Add(new SpawnPoint(worldPos, SpawnType.Group));
                else if (_rng.NextDouble() < 0.02)
                    SpawnPoints.Add(new SpawnPoint(worldPos, SpawnType.Open));
            }
        }
    }
}

public class Room
{
    public Vector2 Position;
    public int Radius;
    public List<Room> Connections = new();
}