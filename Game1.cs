using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace StormShooter;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private RenderTarget2D _renderTarget;
    private readonly SamplerState _pointSampler = SamplerState.PointClamp;

    private static int VirtualWidth => Settings.VirtualWidth;
    private static int VirtualHeight => Settings.VirtualHeight;

    private Texture2D _crosshairTexture;
    private Texture2D _pixel;
    private Player _player;
    private Gun _currentGun;
    private BulletManager _bulletManager;
    private Texture2D _bulletTexture;
    private Texture2D _playerWalkTexture;
    private Texture2D _playerIdleTexture;
    private readonly Random _random = new();
    private GunController _gunController;

    private float _shakeTime;
    private float _shakeStrength;
    private Vector2 _shakeOffset;
    private float _hitStopTime;
    private Vector2 _cameraPos;

    private ParticleSystem _particles = new();
    private EnemyManager _enemyManager;
    private LightingRenderer _lighting;
    private KeyboardState _previousKb;

    private Tile[,] _grid;
    private int _gridWidth = 50;
    private int _gridHeight = 35;
    private int _tileSize = 16;
    
    private Dictionary<string, Texture2D> _gunTextures = new();
    private Dictionary<TileType, Texture2D[]> _tileTextures = new();

    private int _frameCount = 0;
    private int _currentFps = 0;
    private double _fpsTimer = 0;

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
        _graphics = new GraphicsDeviceManager(this);

        int monitorHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        int initialScale = Math.Max(1, (monitorHeight / Settings.VirtualHeight) - 1);

        _graphics.PreferredBackBufferWidth = Settings.VirtualWidth * initialScale;
        _graphics.PreferredBackBufferHeight = Settings.VirtualHeight * initialScale;
        _graphics.HardwareModeSwitch = false;
        _graphics.ApplyChanges();

        Window.AllowUserResizing = true;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void LoadContent()
    {
        _renderTarget = new RenderTarget2D(GraphicsDevice, VirtualWidth, VirtualHeight);
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _graphics.PreferMultiSampling = false;
        _graphics.SynchronizeWithVerticalRetrace = false;
        this.IsFixedTimeStep = true;
        this.TargetElapsedTime = TimeSpan.FromSeconds(1d / 60d);
        _graphics.ApplyChanges();

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        IsMouseVisible = false;
        _crosshairTexture = Content.Load<Texture2D>("crosshair");
        _bulletTexture = Content.Load<Texture2D>("bullet");

        _playerIdleTexture = Content.Load<Texture2D>("player_idle");
        _playerWalkTexture = Content.Load<Texture2D>("player_walk_new");
        
        _gunTextures["gun_scraprifle"] = Content.Load<Texture2D>("gun_scraprifle");
        _gunTextures["gun_shotgun"] = Content.Load<Texture2D>("gun_shotgun");
        _gunTextures["gun_asval"] = Content.Load<Texture2D>("gun_asval");

        _tileTextures[TileType.Empty] = new[]
        {
            Content.Load<Texture2D>("snow_floor_0"),
            Content.Load<Texture2D>("snow_floor_1"),
            Content.Load<Texture2D>("snow_floor_2"),
        };

        _tileTextures[TileType.Wall] = new[]
        {
            Content.Load<Texture2D>("snow_wall_0"),
            Content.Load<Texture2D>("snow_wall_1"),
        };

        _currentGun = GunData.ScrapRifle;

        _lighting = new LightingRenderer(GraphicsDevice, VirtualWidth, VirtualHeight)
        {
            PlayerRadius = 60f,
            DimMultiplier = 10f,
            DimBrightness = 0.35f
        };

        _bulletManager = new BulletManager();
        _enemyManager = new EnemyManager();
        _gunController = new GunController(_random);

        _enemyManager.IsWall = IsWall;
        _enemyManager.Bullets = _bulletManager;
        _enemyManager.Particles = _particles;
        _enemyManager.Lighting = _lighting;
        _enemyManager.Rng = _random;

        LevelGenerator generator = new LevelGenerator();
        _grid = new Tile[_gridWidth, _gridHeight];

        var generated = generator.Generate(_gridWidth, _gridHeight);
        var rooms = generator.Rooms;

        for (int x = 0; x < _gridWidth; x++)
            for (int y = 0; y < _gridHeight; y++)
                _grid[x, y] = new Tile { Type = generated[x, y], Variant = _random.Next(0, 3) };

        Vector2 spawnPos = FindSpawnPosition();
        _player = new Player(spawnPos, Settings.PlayerSpeed, _playerIdleTexture, _playerWalkTexture);

        var spawner = new EnemySpawner(_grid, _tileSize);
        spawner.Spawn(rooms, _enemyManager, spawnPos);

        _cameraPos = spawnPos - new Vector2(VirtualWidth / 2f, VirtualHeight / 2f);
    }

    private Rectangle GetDestinationRectangle()
    {
        int sw = _graphics.GraphicsDevice.PresentationParameters.BackBufferWidth;
        int sh = _graphics.GraphicsDevice.PresentationParameters.BackBufferHeight;

        float scaleX = (float)sw / VirtualWidth;
        float scaleY = (float)sh / VirtualHeight;
        float scale = Math.Min(scaleX, scaleY);

        int rw = (int)(VirtualWidth * scale);
        int rh = (int)(VirtualHeight * scale);

        return new Rectangle((sw - rw) / 2, (sh - rh) / 2, rw, rh);
    }

    private Vector2 WorldToScreen(Vector2 worldPos, Rectangle destRect, float scale)
    {
        return new Vector2(
            destRect.X + (worldPos.X - _cameraPos.X) * scale,
            destRect.Y + (worldPos.Y - _cameraPos.Y) * scale
        );
    }

    protected override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        var mouse = Mouse.GetState();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _fpsTimer += dt;
        _frameCount++;
        if (_fpsTimer >= 1.0)
        {
            _currentFps = _frameCount;
            Window.Title = $"StormShooter | FPS: {_currentFps} | Position: {(int)_player.Position.X}, {(int)_player.Position.Y}";
            _frameCount = 0;
            _fpsTimer -= 1.0;
        }

        if (_shakeTime > 0) _shakeTime -= dt;

        if (kb.IsKeyDown(Keys.Escape)) Exit();
        if (kb.IsKeyDown(Keys.F11) && _previousKb.IsKeyUp(Keys.F11)) _graphics.ToggleFullScreen();
        _previousKb = kb;

        if (_hitStopTime > 0f)
        {
            _hitStopTime -= dt;
            return;
        }

        var destRect = GetDestinationRectangle();
        float currentScale = (float)destRect.Height / VirtualHeight;
        float mouseX = (mouse.X - destRect.X) / currentScale;
        float mouseY = (mouse.Y - destRect.Y) / currentScale;
        Vector2 mouseWorld = _cameraPos + new Vector2(mouseX, mouseY);

        _player.Update(dt, kb, mouseWorld, IsWall);
        _enemyManager.Update(dt, _player.Position, _lighting);

        if (kb.IsKeyDown(Keys.D1)) { _currentGun = GunData.ScrapRifle; _gunController.CancelReload(); }
        if (kb.IsKeyDown(Keys.D2)) { _currentGun = GunData.Shotgun; _gunController.CancelReload(); }
        if (kb.IsKeyDown(Keys.D3)) { _currentGun = GunData.VAL; _gunController.CancelReload(); }

        _gunController.Update(dt, mouse, kb, mouseWorld, _player, _currentGun, _bulletManager, _lighting, ref _shakeTime, ref _shakeStrength, ref _shakeOffset);
        _bulletManager.Update(dt, _enemyManager.Enemies, _particles, _lighting, _currentGun, ref _hitStopTime, ref _shakeTime, ref _shakeStrength, VirtualWidth, VirtualHeight, _random, IsWall, _player);
        _particles.Update(dt);

        Vector2 lookOffset = (mouseWorld - _player.Position) * 0.2f;
        Vector2 cameraTarget = _player.Position + lookOffset
                               - new Vector2(VirtualWidth / 2f, VirtualHeight / 2f);

        _cameraPos = Vector2.Lerp(_cameraPos, cameraTarget, 10f * dt);

        _shakeOffset = Vector2.Lerp(_shakeOffset, Vector2.Zero, dt * 20f);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var mouse = Mouse.GetState();

        var baseDestRect = GetDestinationRectangle();
        float currentScale = (float)baseDestRect.Height / VirtualHeight;

        var finalDestRect = baseDestRect;
        if (_shakeTime > 0f)
        {
            finalDestRect.X += (int)(_random.Next(-(int)_shakeStrength, (int)_shakeStrength) * currentScale);
            finalDestRect.Y += (int)(_random.Next(-(int)_shakeStrength, (int)_shakeStrength) * currentScale);
        }
        finalDestRect.X += (int)(_shakeOffset.X * currentScale);
        finalDestRect.Y += (int)(_shakeOffset.Y * currentScale);

        //Vector2 roundedCamera = new Vector2(MathF.Round(_cameraPos.X), MathF.Round(_cameraPos.Y));
        Vector2 cameraOffset = _cameraPos;
        Matrix lowResCamera = Matrix.CreateTranslation(-cameraOffset.X, -cameraOffset.Y, 0f);

        var lightMap = _lighting.BuildLightMap(_spriteBatch, _player.Position, _cameraPos, dt);

        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(samplerState: _pointSampler, transformMatrix: lowResCamera);

        int startX = Math.Max(0, (int)(cameraOffset.X / _tileSize));
        int startY = Math.Max(0, (int)(cameraOffset.Y / _tileSize));
        int endX = Math.Min(_gridWidth, startX + (VirtualWidth / _tileSize) + 2);
        int endY = Math.Min(_gridHeight, startY + (VirtualHeight / _tileSize) + 2);

        for (int x = startX; x < endX; x++)
            for (int y = startY; y < endY; y++)
            {
                var tile = _grid[x, y];
                if (_tileTextures.TryGetValue(tile.Type, out Texture2D[] textures))
                    _spriteBatch.Draw(textures[tile.Variant % textures.Length], new Vector2(x * _tileSize, y * _tileSize), Color.White);
            }

        _player.Draw(_spriteBatch, GetGunTexture(_currentGun), _currentGun);
        _enemyManager.Draw(_spriteBatch, _pixel);
        _bulletManager.Draw(_spriteBatch, _bulletTexture);
        _particles.Draw(_spriteBatch, _pixel);
        _spriteBatch.End();

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(samplerState: _pointSampler);
        _spriteBatch.Draw(_renderTarget, finalDestRect, Color.White);
        _spriteBatch.End();

        _spriteBatch.Begin(blendState: MultiplyBlend, samplerState: _pointSampler);
        _spriteBatch.Draw(lightMap, finalDestRect, Color.White);
        _spriteBatch.End();

        _spriteBatch.Begin(samplerState: _pointSampler);

        int currentAmmo = _gunController.GetCurrentAmmo(_currentGun);
        for (int i = 0; i < currentAmmo; i++)
        {
            int ax = finalDestRect.X + (int)(10 * currentScale) + (int)(i * 4 * currentScale);
            int ay = finalDestRect.Y + finalDestRect.Height - (int)(15 * currentScale);
            _spriteBatch.Draw(_pixel, new Rectangle(ax, ay, (int)(2 * currentScale), (int)(6 * currentScale)), Color.Orange);
        }

        if (_gunController.IsReloading)
        {
            Vector2 screenPos = WorldToScreen(_player.Position, finalDestRect, currentScale);
            int barW = (int)(40 * currentScale);
            int barH = (int)(6 * currentScale);
            Rectangle reloadBg = new Rectangle((int)screenPos.X - barW / 2, (int)screenPos.Y - (int)(40 * currentScale), barW, barH);
            float progress = 1f - (_gunController.ReloadProgress / _currentGun.ReloadTime);
            Rectangle reloadFill = new Rectangle(reloadBg.X, reloadBg.Y, (int)(reloadBg.Width * progress), reloadBg.Height);
            _spriteBatch.Draw(_pixel, reloadBg, Color.Black * 0.5f);
            _spriteBatch.Draw(_pixel, reloadFill, Color.White);
        }

        Vector2 crosshairPos = new Vector2(mouse.X, mouse.Y);
        Vector2 crosshairOrigin = new Vector2(_crosshairTexture.Width / 2, _crosshairTexture.Height / 2);
        _spriteBatch.Draw(_crosshairTexture, crosshairPos, null, Color.White, 0f, crosshairOrigin, 3f, SpriteEffects.None, 0f);

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _lighting?.Dispose();
        _renderTarget?.Dispose();
        _pixel?.Dispose();
        base.UnloadContent();
    }

    bool IsWall(Vector2 pos)
    {
        int tx = (int)(pos.X / _tileSize);
        int ty = (int)(pos.Y / _tileSize);
        if (tx < 0 || ty < 0 || tx >= _gridWidth || ty >= _gridHeight) return true;
        return _grid[tx, ty].Type == TileType.Wall;
    }

    Vector2 FindSpawnPosition()
    {
        for (int i = 0; i < 1000; i++)
        {
            int x = _random.Next(_gridWidth);
            int y = _random.Next(_gridHeight);
            if (_grid[x, y].Type == TileType.Empty)
                return new Vector2(x * _tileSize + _tileSize / 2f, y * _tileSize + _tileSize / 2f);
        }
        return Vector2.Zero;
    }

    private Texture2D GetGunTexture(Gun gun)
    {
        if (_gunTextures.TryGetValue(gun.SpriteName, out var texture)) return texture;
        return _pixel;
    }
}