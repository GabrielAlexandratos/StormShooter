using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace StormShooter;

public class LevelGenerator
{
    private int _width;
    private int _height;
    private Random _rng;

    private const int TILE_SIZE = 10;

    public List<Room> Rooms { get; private set; } = new();
    public List<SpawnPoint> SpawnPoints = new();

    public TileType[,] Generate(int width, int height, int seed = -1)
    {
        _width = width;
        _height = height;
        _rng = seed == -1 ? new Random() : new Random(seed);

        var grid = new TileType[width, height];

        // Fill the level with solid walls
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
            grid[x, y] = TileType.Wall;

        Rooms = GenerateRooms();

        // Carve rooms
        foreach (var room in Rooms)
            CarveRoom(grid, room);

        // Carve corridors
        foreach (var room in Rooms)
        foreach (var other in room.Connections)
            CarveCorridor(grid, room.Position, other.Position);

        // Smoothing walls
        SmoothWalls(grid);

        BuildSpawnPoints(grid);

        return grid;
    }

    private List<Room> GenerateRooms()
    {
        var rooms = new List<Room>();

        var start = CreateRoom();
        rooms.Add(start);

        var mainPath = BuildMainPath(start, rooms);
        var branch = BuildGuaranteedBranch(rooms);
        BuildRandomBranches(rooms);
        AddRandomConnections(rooms, branch);
        
        return rooms;
    }

    private List<Room> BuildMainPath(Room start, List<Room> rooms)
    {
        var path = new List<Room> { start };
        var current = start;
        int straightCount = 0;

        // Main path
        for (int i = 0; i < 7; i++)
        {
            var next = CreateRoomNear(current);
            Connect(current, next);
            rooms.Add(next);
            path.Add(next);

            if (++straightCount >= 3)
            {
                straightCount = 0;
                var side = CreateRoomNear(current);
                Connect(current, side);
                rooms.Add(side);
            }

            current = next;
        }

        return path;
    }

    private List<Room> BuildGuaranteedBranch(List<Room> rooms)
    {
        // Generate at least 1 branching path that doesn't connect back to the main path in each level
        var branch = new List<Room>();
        int originIndex = _rng.Next(2, rooms.Count - 2);
        var current = rooms[originIndex];
        float dirY = _rng.Next(2) == 0 ? 1f : -1f;

        int length = _rng.Next(3, 5);
        for (int i = 0; i < length; i++)
        {
            var offset = new Vector2(_rng.Next(6, 10), dirY * _rng.Next(8, 14));
            var pos = Clamp(current.Position + offset);

            var next = new Room { Position = pos, Radius = _rng.Next(4, 6) };
            Connect(current, next);
            rooms.Add(next);
            branch.Add(next);
            current = next;
        }

        return branch;
    }

    private void BuildRandomBranches(List<Room> rooms)
    {
        // Branches
        int count = rooms.Count;
        for (int i = 1; i < count - 1; i++)
        {
            if (_rng.NextDouble() >= 0.4) continue;

            var current = CreateRoomNear(rooms[i]);
            Connect(rooms[i], current);
            rooms.Add(current);

            int length = _rng.Next(2, 4);
            for (int j = 0; j < length; j++)
            {
                var next = CreateRoomNear(current);
                Connect(current, next);
                rooms.Add(next);
                current = next;
            }
        }
    }

    private void AddRandomConnections(List<Room> rooms, List<Room> protectedBranch)
    {
        // Connect rooms that are not adjacent
        for (int i = 0; i < rooms.Count; i++)
        {
            if (_rng.NextDouble() >= 0.25) continue;

            var a = rooms[i];
            var b = rooms[_rng.Next(rooms.Count)];

            if (a == b || a.Connections.Contains(b)) continue;

            // prevent the guaranteed branch from reconnecting to main path
            bool aInBranch = protectedBranch.Contains(a);
            bool bInBranch = protectedBranch.Contains(b);
            if (aInBranch != bInBranch) continue;

            Connect(a, b);
        }
    }

    private void SmoothWalls(TileType[,] grid)
    {
        for (int pass = 0; pass < 3; pass++)
        {
            var copy = (TileType[,])grid.Clone();

            for (int x = 1; x < _width - 1; x++)
            for (int y = 1; y < _height - 1; y++)
            {
                int directEmpty = 0;
                if (copy[x + 1, y] == TileType.Empty) directEmpty++;
                if (copy[x - 1, y] == TileType.Empty) directEmpty++;
                if (copy[x, y + 1] == TileType.Empty) directEmpty++;
                if (copy[x, y - 1] == TileType.Empty) directEmpty++;

                // Remove single tiles / noise
                if (copy[x, y] == TileType.Empty && directEmpty <= 1)
                    grid[x, y] = TileType.Wall;

                // Fill small wall holes
                if (copy[x, y] == TileType.Wall && directEmpty >= 3)
                    grid[x, y] = TileType.Empty;
            }
        }
    }

    private Room CreateRoom()
    {
        return new Room
        {
            Position = new Vector2(15, _height / 2), // start on the left
            Radius = _rng.Next(4, 6)
        };
    }

    private Room CreateRoomNear(Room other)
    {
        Vector2 offset;

        // Check if this is a branch or main path based on the number of connections
        if (other.Connections.Count > 2)
        {
            // adding slight variation
            offset = new Vector2(_rng.Next(-12, 12), _rng.Next(-12, 12));
            if (offset.Length() < 6) offset *= 2; // No tiny offsets
        }
        else
        {
            // Main path stays going in one direction
            offset = new Vector2(_rng.Next(10, 18), _rng.Next(-8, 8));
        }

        return new Room
        {
            Position = Clamp(other.Position + offset),
            Radius = _rng.Next(4, 6)
        };
    }

    private void CarveRoom(TileType[,] grid, Room room)
    {
        for (int x = -room.Radius; x <= room.Radius; x++)
        for (int y = -room.Radius; y <= room.Radius; y++)
        {
            if (x * x + y * y > room.Radius * room.Radius) continue;

            int gx = (int)room.Position.X + x;
            int gy = (int)room.Position.Y + y;

            if (InBounds(gx, gy))
                grid[gx, gy] = TileType.Empty;
        }
    }

    private void CarveCorridor(TileType[,] grid, Vector2 a, Vector2 b)
    {
        var dir = b - a;
        int steps = (int)dir.Length();
        if (steps == 0) return;

        dir /= steps;
        var pos = a;

        for (int i = 0; i < steps; i++)
        {
            int gx = (int)pos.X;
            int gy = (int)pos.Y;

            if (InBounds(gx, gy))     grid[gx, gy]         = TileType.Empty;
            if (InBounds(gx + 1, gy)) grid[gx + 1, gy]     = TileType.Empty;
            if (InBounds(gx - 1, gy)) grid[gx - 1, gy]     = TileType.Empty;
            if (InBounds(gx, gy + 1)) grid[gx, gy + 1]     = TileType.Empty;
            if (InBounds(gx, gy - 1)) grid[gx, gy - 1]     = TileType.Empty;

            pos += dir;
        }
    }

    private void Connect(Room a, Room b)
    {
        a.Connections.Add(b);
        b.Connections.Add(a);
    }

    private void BuildSpawnPoints(TileType[,] grid)
    {
        SpawnPoints.Clear();

        for (int x = 1; x < _width - 1; x++)
        for (int y = 1; y < _height - 1; y++)
        {
            if (grid[x, y] != TileType.Empty)
                continue;

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

    private Vector2 Clamp(Vector2 pos)
    {
        return new Vector2(
            MathHelper.Clamp(pos.X, 10, _width - 10),
            MathHelper.Clamp(pos.Y, 10, _height - 10)
        );
    }

    private bool InBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < _width && y < _height;
    }
}

public class Room
{
    public Vector2 Position;
    public int Radius;
    public List<Room> Connections = new();
}