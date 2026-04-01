using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace StormShooter;

public class Game1 : Game
{
    private SpriteBatch _spriteBatch;

    private RenderTarget2D _renderTarget;
    private readonly SamplerState _pointSampler = SamplerState.PointClamp;

    private static readonly int VirtualWidth  = Settings.VirtualWidth;
    private static readonly int VirtualHeight = Settings.VirtualHeight;
    private static readonly int WindowScale   = Settings.WindowScale;

    private readonly int _windowWidth  = VirtualWidth * WindowScale;
    private readonly int _windowHeight = VirtualHeight * WindowScale;

    private Texture2D _pixel;

    private Player _player;

    private Gun _currentGun;

    private BulletManager _bulletManager;
    private readonly Random _random = new();

    private GunController _gunController;

    private float _shakeTime;
    private float _shakeStrength;

    private float _hitStopTime;

    private Vector2 _cameraPos;

    private ParticleSystem _particles = new();
    private EnemyManager _enemyManager;
    
    private LightingRenderer _lighting;
    
    // Tiles
    private Tile[,] _grid;
    
    private int _gridWidth = 200;
    private int _gridHeight = 200;
    
    private int _tileSize = 10;

    // Custom multiply blend state for applying the lightmap
    private static readonly BlendState MultiplyBlend = new BlendState
    {
        ColorBlendFunction = BlendFunction.Add,
        ColorSourceBlend = Blend.DestinationColor,
        ColorDestinationBlend = Blend.Zero,
        AlphaBlendFunction = BlendFunction.Add,
        AlphaSourceBlend = Blend.DestinationAlpha,
        AlphaDestinationBlend = Blend.Zero,
    };

    public Game1()
    {
        var graphics = new GraphicsDeviceManager(this);
        graphics.PreferredBackBufferWidth = _windowWidth;
        graphics.PreferredBackBufferHeight = _windowHeight;
        graphics.ApplyChanges();

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void LoadContent()
    {
        _renderTarget = new RenderTarget2D(GraphicsDevice, VirtualWidth, VirtualHeight);
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _currentGun = GunData.BurstRifle;
        
        _lighting = new LightingRenderer(GraphicsDevice, VirtualWidth, VirtualHeight)
        {
            // Player light source settings
            PlayerRadius = 60f,
            DimMultiplier = 10f,
            DimBrightness = 0.35f
        };

        _bulletManager = new BulletManager();
        _enemyManager = new EnemyManager();

        _gunController = new GunController(_random);

        LevelGenerator generator = new LevelGenerator();
        _grid = new Tile[_gridWidth, _gridHeight];

        var generated = generator.Generate(_gridWidth, _gridHeight);
        var rooms = generator.Rooms;

        for (int x = 0; x < _gridWidth; x++)
        for (int y = 0; y < _gridHeight; y++)
        {
            _grid[x, y] = new Tile { Type = generated[x, y] };
        }


        Vector2 spawnPos = FindSpawnPosition();
        _player = new Player(spawnPos, Settings.PlayerSpeed, _pixel);

        var spawner = new EnemySpawner(_grid, _tileSize);
        spawner.Spawn(rooms, _enemyManager, spawnPos);

    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        float dt  = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var kb = Keyboard.GetState();
        var mouse = Mouse.GetState();
        
        Vector2 mouseWorld = _cameraPos + new Vector2(mouse.X, mouse.Y) / WindowScale;
        
        if (_hitStopTime > 0f)
        {
            _hitStopTime -= dt;
            return;
        }

        _player.Update(dt, kb, mouse, WindowScale, _cameraPos, IsWall);

        // Update enemies
        _enemyManager.Update(dt, _lighting);

        if (kb.IsKeyDown(Keys.D1)) _currentGun = GunData.BurstRifle;
        if (kb.IsKeyDown(Keys.D2)) _currentGun = GunData.Smg;
        if (kb.IsKeyDown(Keys.D3)) _currentGun = GunData.Pistol;
        if (kb.IsKeyDown(Keys.D4)) _currentGun = GunData.Shotgun;

        _gunController.Update(
            dt,
            mouse,
            mouseWorld,
            _player,
            _currentGun,
            _bulletManager,
            _lighting,
            ref _shakeTime,
            ref _shakeStrength
        );

        _bulletManager.Update(
            dt,
            _enemyManager.Enemies,
            _particles,
            _lighting,
            _currentGun,
            ref _hitStopTime,
            ref _shakeTime,
            ref _shakeStrength,
            VirtualWidth,
            VirtualHeight,
            _random,
            IsWall
        );
        
        // Update particles
        _particles.Update(dt);
        
        // Camera look-ahead
        Vector2 mouseScreen = new Vector2(mouse.X, mouse.Y);
        Vector2 camMouseWorld = mouseWorld;

        Vector2 lookOffset = (camMouseWorld - _player.Position) * 0.2f;

        Vector2 cameraTarget = _player.Position + lookOffset - new Vector2(VirtualWidth / 2f, VirtualHeight / 2f);

        _cameraPos = Vector2.Lerp(_cameraPos, cameraTarget, 10f * dt);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        var lightMap = _lighting.BuildLightMap(
            _spriteBatch,
            _player.Position,
            _cameraPos,
            dt
        );

        // Draw game to render target
        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(new Color(30, 30, 30));

        _spriteBatch.Begin(
            samplerState: _pointSampler,
            transformMatrix: Matrix.CreateTranslation(-_cameraPos.X, -_cameraPos.Y, 0f)
        );
        
        for (int x = 0; x < _gridWidth; x++)
        for (int y = 0; y < _gridHeight; y++)
        {
            Vector2 pos = new Vector2(x * _tileSize, y * _tileSize);

            Color color = _grid[x, y].Type switch
            {
                TileType.Empty => Color.DarkGray * 0.75f,
                
                TileType.Wall => Color.DarkSlateGray,
                TileType.Cover => Color.LightGray,
                _ => Color.Magenta
            };
            
            _spriteBatch.Draw(_pixel, pos, null, color, 0f, Vector2.Zero, new Vector2(_tileSize, _tileSize), SpriteEffects.None, 0f);
        }


        // Player and gun
        _player.Draw(_spriteBatch);

        // Bullets
        _bulletManager.Draw(_spriteBatch, _pixel);

        // Enemies
        _enemyManager.Draw(_spriteBatch, _pixel);

        // Particles
        _particles.Draw(_spriteBatch, _pixel);

        _spriteBatch.End();

        // Apply lights to the game world
        // This darkens the pixels that are not in the light
        _spriteBatch.Begin(
            blendState: MultiplyBlend,
            samplerState: _pointSampler
        );
        _spriteBatch.Draw(lightMap,
            new Rectangle(0, 0, VirtualWidth, VirtualHeight),
            Color.White);
        _spriteBatch.End();

        // Scale to window with screen shake
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(new Color(10, 10, 10));

        Vector2 shakeOffset = Vector2.Zero;
        if (_shakeTime > 0f)
        {
            _shakeTime -= dt;
            shakeOffset = new Vector2(
                (_random.NextSingle() - 0.5f) * _shakeStrength,
                (_random.NextSingle() - 0.5f) * _shakeStrength);
        }

        _spriteBatch.Begin(
            samplerState: _pointSampler
        );
        _spriteBatch.Draw(
            _renderTarget,
            new Rectangle((int)shakeOffset.X, (int)shakeOffset.Y, _windowWidth, _windowHeight),
            Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _lighting?.Dispose();
        base.UnloadContent();
    }


    bool IsWall(Vector2 pos)
    {
        int tx = (int)(pos.X / _tileSize);
        int ty = (int)(pos.Y / _tileSize);

        if (tx < 0 || ty < 0 || tx >= _gridWidth || ty >= _gridHeight)
            return true;

        return _grid[tx, ty].Type == TileType.Wall;
    }

    /*
    Vector2 GetRandomDirection()
    {
        int r = _random.Next(4);

        return r switch
        {
            0 => new Vector2(1, 0),
            1 => new Vector2(-1, 0),
            2 => new Vector2(0, 1),
            _ => new Vector2(0, -1),
        };
    }
    */
    
    Vector2 FindSpawnPosition()
    {
        for (int i = 0; i < 1000; i++)
        {
            int x = _random.Next(_gridWidth);
            int y = _random.Next(_gridHeight);

            if (_grid[x, y].Type == TileType.Empty)
            {
                return new Vector2(x * _tileSize + _tileSize / 2f,
                                   y * _tileSize + _tileSize / 2f);
            }
        }

        return new Vector2(0, 0);
    }
}

public class EnemySpawner
{
    private Tile[,] _grid;
    private int _tileSize;

    private Random _random = new Random();

    public EnemySpawner(Tile[,] grid, int tileSize)
    {
        _grid = grid;
        _tileSize = tileSize;
    }

    public void Spawn(List<Room> rooms, EnemyManager manager, Vector2 playerPos)
    {
        int clusterCount = Math.Min(rooms.Count, 4);

        List<Room> candidates = new();

        foreach (var room in rooms)
        {
            // only skip the exact starting area
            if (Vector2.Distance(room.Position * _tileSize, playerPos) < 120f)
                continue;

            candidates.Add(room);
        }

        if (candidates.Count == 0)
            candidates = new List<Room>(rooms);

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

            // sample inside circle
            float angle = (float)(_random.NextDouble() * Math.PI * 2);
            float radius = (float)_random.NextDouble() * room.Radius;

            Vector2 localOffset = new Vector2(
                (float)Math.Cos(angle),
                (float)Math.Sin(angle)
            ) * radius * _tileSize;

            Vector2 worldCenter = room.Position * _tileSize;
            Vector2 pos = worldCenter + localOffset;

            // convert to tile
            int tx = (int)(pos.X / _tileSize);
            int ty = (int)(pos.Y / _tileSize);

            // bounds check
            if (tx < 0 || ty < 0 || tx >= 200 || ty >= 200)
                continue;

            // ONLY spawn on empty tiles
            if (_grid[tx, ty].Type == TileType.Empty && IsFarFromOthers(pos, manager))
            {
                manager.AddEnemy(pos, EnemyType.Basic);
                spawned++;
            }
        }
    }

    private bool IsFarFromOthers(Vector2 pos, EnemyManager manager)
    {
        float minDist = 30f; // spacing between enemies

        foreach (var e in manager.Enemies)
        {
            if (Vector2.Distance(e.Position, pos) < minDist)
                return false;
        }

        return true;
    }
}