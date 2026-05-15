using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace StormShooter;

public class LevelGenerator
{
    private int _width;
    private int _height;
    private Random _rng;

    public List<Room> Rooms { get; private set; } = new();

    public TileType[,] Generate(int width, int height, int seed = -1)
    {
        _width = width;
        _height = height;
        _rng = seed == -1 ? new Random() : new Random(seed);
        Rooms.Clear();

        var grid = new TileType[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = TileType.Wall;

        PlaceRooms();
        ConnectRooms(grid);
        CarveRooms(grid);
        ErodeEdges(grid);

        return grid;
    }

    private void PlaceRooms()
    {
        const int padding = 2;
        const int maxRooms = 8;

        for (int attempt = 0; attempt < 300 && Rooms.Count < maxRooms; attempt++)
        {
            int w = _rng.Next(5, 12);
            int h = _rng.Next(4, 9);
            int x = _rng.Next(2, _width  - w - 2);
            int y = _rng.Next(2, _height - h - 2);

            var candidate = new Rectangle(x, y, w, h);
            bool overlaps = false;

            foreach (var room in Rooms)
            {
                var padded = new Rectangle(
                    room.Bounds.X - padding,
                    room.Bounds.Y - padding,
                    room.Bounds.Width  + padding * 2,
                    room.Bounds.Height + padding * 2);

                if (padded.Intersects(candidate)) { overlaps = true; break; }
            }

            if (!overlaps)
                Rooms.Add(new Room { Bounds = candidate });
        }
    }

    private void ConnectRooms(TileType[,] grid)
    {
        if (Rooms.Count < 2) return;

        // cheeky minimum spanning tree
        var connected = new HashSet<int> { 0 };

        while (connected.Count < Rooms.Count)
        {
            int bestFrom = -1, bestTo = -1;
            float bestDist = float.MaxValue;

            foreach (int i in connected)
            {
                for (int j = 0; j < Rooms.Count; j++)
                {
                    if (connected.Contains(j)) continue;
                    float d = Vector2.Distance(Rooms[i].Center, Rooms[j].Center);
                    if (d < bestDist) { bestDist = d; bestFrom = i; bestTo = j; }
                }
            }

            Rooms[bestFrom].Connections.Add(Rooms[bestTo]);
            Rooms[bestTo].Connections.Add(Rooms[bestFrom]);
            CarveCorridor(grid, Rooms[bestFrom].Center, Rooms[bestTo].Center);
            connected.Add(bestTo);
        }

        // Add a couple of extra connections to create loops
        int extras = _rng.Next(1, 3);
        for (int i = 0; i < extras; i++)
        {
            int a = _rng.Next(Rooms.Count);
            int b = _rng.Next(Rooms.Count);
            if (a == b || Rooms[a].Connections.Contains(Rooms[b])) continue;
            Rooms[a].Connections.Add(Rooms[b]);
            Rooms[b].Connections.Add(Rooms[a]);
            CarveCorridor(grid, Rooms[a].Center, Rooms[b].Center);
        }
    }

    private void CarveCorridor(TileType[,] grid, Vector2 from, Vector2 to)
    {
        int x1 = (int)from.X, y1 = (int)from.Y;
        int x2 = (int)to.X,   y2 = (int)to.Y;
        const int w = 2;

        if (_rng.NextDouble() < 0.5)
        {
            CarveHLine(grid, y1, Math.Min(x1, x2), Math.Max(x1, x2), w);
            CarveVLine(grid, x2, Math.Min(y1, y2), Math.Max(y1, y2), w);
        }
        else
        {
            CarveVLine(grid, x1, Math.Min(y1, y2), Math.Max(y1, y2), w);
            CarveHLine(grid, y2, Math.Min(x1, x2), Math.Max(x1, x2), w);
        }
    }

    private void CarveHLine(TileType[,] grid, int y, int x1, int x2, int width)
    {
        for (int x = x1; x <= x2; x++)
            for (int dy = 0; dy < width; dy++)
                SetFloor(grid, x, y + dy);
    }

    private void CarveVLine(TileType[,] grid, int x, int y1, int y2, int width)
    {
        for (int y = y1; y <= y2; y++)
            for (int dx = 0; dx < width; dx++)
                SetFloor(grid, x + dx, y);
    }

    private void CarveRooms(TileType[,] grid)
    {
        foreach (var room in Rooms)
            for (int x = room.Bounds.X; x < room.Bounds.X + room.Bounds.Width; x++)
                for (int y = room.Bounds.Y; y < room.Bounds.Y + room.Bounds.Height; y++)
                    SetFloor(grid, x, y);
    }

    // Randomly removes wall tiles with many floor neighbours to like soften corners and make it feel more natural
    private void ErodeEdges(TileType[,] grid)
    {
        var toErode = new List<(int x, int y)>();

        for (int x = 1; x < _width - 1; x++)
        {
            for (int y = 1; y < _height - 1; y++)
            {
                if (grid[x, y] != TileType.Wall) continue;

                int floorNeighbors = 0;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        if (grid[x + dx, y + dy] == TileType.Empty) floorNeighbors++;
                    }

                if (floorNeighbors >= 5 || (floorNeighbors >= 3 && _rng.NextDouble() < 0.45))
                    toErode.Add((x, y));
            }
        }

        foreach (var (x, y) in toErode)
            grid[x, y] = TileType.Empty;
    }

    private void SetFloor(TileType[,] grid, int x, int y)
    {
        if (x >= 1 && x < _width - 1 && y >= 1 && y < _height - 1)
            grid[x, y] = TileType.Empty;
    }
}

public class Room
{
    public Rectangle Bounds;
    public List<Room> Connections = new();

    public Vector2 Center => new Vector2(
        Bounds.X + Bounds.Width  / 2f,
        Bounds.Y + Bounds.Height / 2f);
}
