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

    // Tiles
    private Tile[,] _grid;
    private int _gridWidth = 200;
    private int _gridHeight = 200;
    private int _tileSize = 10;

    private Dictionary<string, Texture2D> _gunTextures = new();

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

        // Window setup based on monitor
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

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        IsMouseVisible = false;
        _crosshairTexture = Content.Load<Texture2D>("crosshair");

        _bulletTexture = Content.Load<Texture2D>("bullet");

        // _gunTextures["gun_machinegun"] = Content.Load<Texture2D>("gun_machinegun");
        _gunTextures["gun_smg"] = Content.Load<Texture2D>("gun_smg");
        _gunTextures["gun_shotgun"] = Content.Load<Texture2D>("gun_shotgun");

        _currentGun = GunData.MachineGun;

        _lighting = new LightingRenderer(GraphicsDevice, VirtualWidth, VirtualHeight)
        {
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

        // Enemy Spawner setup
        var spawner = new EnemySpawner(_grid, _tileSize);
        spawner.Spawn(rooms, _enemyManager, spawnPos);

        // Start camera on player
        _cameraPos = spawnPos - new Vector2(VirtualWidth / 2f, VirtualHeight / 2f);
    }

    private Rectangle GetDestinationRectangle()
    {
        int sw = _graphics.GraphicsDevice.PresentationParameters.BackBufferWidth;
        int sh = _graphics.GraphicsDevice.PresentationParameters.BackBufferHeight;

        int scale = sh / VirtualHeight;
        if (VirtualWidth * scale > sw) scale = sw / VirtualWidth;
        scale = Math.Max(1, scale);

        int rw = VirtualWidth * scale;
        int rh = VirtualHeight * scale;

        return new Rectangle((sw - rw) / 2, (sh - rh) / 2, rw, rh);
    }

    protected override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        var mouse = Mouse.GetState();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

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
        _enemyManager.Update(dt, _lighting);

        if (kb.IsKeyDown(Keys.D1))
        {
            _currentGun = GunData.MachineGun;
            _gunController.CancelReload();
        }
        if (kb.IsKeyDown(Keys.D2))
        {
            _currentGun = GunData.Shotgun;
            _gunController.CancelReload();
        }
        if (kb.IsKeyDown(Keys.D3))
        {
            _currentGun = GunData.BurstRifle;
            _gunController.CancelReload();
        }
        if (kb.IsKeyDown(Keys.D4))
        {
            _currentGun = GunData.Smg;
            _gunController.CancelReload();
        }
        if (kb.IsKeyDown(Keys.D5))
        {
            _currentGun = GunData.LongGun;
            _gunController.CancelReload();
        }
        if (kb.IsKeyDown(Keys.D6))
        {
            //_currentGun = GunData.
            _gunController.CancelReload();
        }

        _gunController.Update(dt, mouse, kb, mouseWorld, _player, _currentGun, _bulletManager, _lighting, ref _shakeTime, ref _shakeStrength, ref _shakeOffset);
        _bulletManager.Update(dt, _enemyManager.Enemies, _particles, _lighting, _currentGun, ref _hitStopTime, ref _shakeTime, ref _shakeStrength, VirtualWidth, VirtualHeight, _random, IsWall);
        _particles.Update(dt);

        //camera
        Vector2 lookOffset = (mouseWorld - _player.Position) * 0.2f;
        Vector2 cameraTarget = _player.Position + lookOffset - new Vector2(VirtualWidth / 2f, VirtualHeight / 2f);
        _shakeOffset = Vector2.Lerp(_shakeOffset, Vector2.Zero, dt * 20f);
        _cameraPos = Vector2.Lerp(_cameraPos, cameraTarget, 10f * dt);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var mouse = Mouse.GetState();

        var baseDestRect = GetDestinationRectangle();
        float currentScale = (float)baseDestRect.Height / VirtualHeight;

        var lightMap = _lighting.BuildLightMap(_spriteBatch, _player.Position, _cameraPos, dt);

        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(new Color(30, 30, 30));

        // draw game objects
        Matrix cameraTransform = Matrix.CreateTranslation(-(int)MathF.Round(_cameraPos.X), -(int)MathF.Round(_cameraPos.Y), 0f);
        _spriteBatch.Begin(samplerState: _pointSampler, transformMatrix: cameraTransform);

        for (int x = 0; x < _gridWidth; x++)
        {
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
        }

        _player.Draw(_spriteBatch, GetGunTexture(_currentGun), _currentGun);
        _bulletManager.Draw(_spriteBatch, _bulletTexture);
        _enemyManager.Draw(_spriteBatch, _pixel);
        _particles.Draw(_spriteBatch, _pixel);
        _spriteBatch.End();

        // apply lighting
        _spriteBatch.Begin(blendState: MultiplyBlend, samplerState: _pointSampler);
        _spriteBatch.Draw(lightMap, new Rectangle(0, 0, VirtualWidth, VirtualHeight), Color.White);
        _spriteBatch.End();

        // drawing cursor
        _spriteBatch.Begin(samplerState: _pointSampler);

        float vx = (mouse.X - baseDestRect.X) / currentScale;
        float vy = (mouse.Y - baseDestRect.Y) / currentScale;
        Vector2 snappedMousePos = new Vector2((int)MathF.Round(vx), (int)MathF.Round(vy));
        Vector2 intOrigin = new Vector2((int)(_crosshairTexture.Width / 2), (int)(_crosshairTexture.Height / 2));

        _spriteBatch.Draw(_crosshairTexture, snappedMousePos, null, Color.White, 0f, intOrigin, 1f, SpriteEffects.None, 0f);
        _spriteBatch.End();

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);

        // screen shake
        var finalDestRect = baseDestRect;
        if (_shakeTime > 0f)
        {
            finalDestRect.X += (int)(_random.Next(-(int)_shakeStrength, (int)_shakeStrength) * currentScale);
            finalDestRect.Y += (int)(_random.Next(-(int)_shakeStrength, (int)_shakeStrength) * currentScale);
        }
        finalDestRect.X += (int)(_shakeOffset.X * currentScale);
        finalDestRect.Y += (int)(_shakeOffset.Y * currentScale);

        // upscale to screen
        _spriteBatch.Begin(samplerState: _pointSampler);
        _spriteBatch.Draw(_renderTarget, finalDestRect, Color.White);

        // ui
        int currentAmmo = _gunController.GetCurrentAmmo(_currentGun);
        for (int i = 0; i < currentAmmo; i++)
        {
            int x = finalDestRect.X + 10 * (int)currentScale + (i * 4 * (int)currentScale);
            int y = finalDestRect.Y + finalDestRect.Height - (15 * (int)currentScale);
            Rectangle ammoRect = new Rectangle(x, y, 2 * (int)currentScale, 6 * (int)currentScale);
            _spriteBatch.Draw(_pixel, ammoRect, Color.Orange);
        }
        if (_gunController.IsReloading)
        {
            // get player position in screen space to attach ui to them
            Vector2 screenPos = new Vector2(
                finalDestRect.X + (_player.Position.X - _cameraPos.X) * currentScale,
                finalDestRect.Y + (_player.Position.Y - _cameraPos.Y) * currentScale
            );

            Rectangle reloadBg = new Rectangle((int)screenPos.X - (int)(20 * currentScale), (int)screenPos.Y - (int)(40 * currentScale), (int)(40 * currentScale), (int)(6 * currentScale));
            float progress = 1f - (_gunController.ReloadProgress / _currentGun.ReloadTime);
            Rectangle reloadFill = new Rectangle(reloadBg.X, reloadBg.Y, (int)(reloadBg.Width * progress), reloadBg.Height);

            _spriteBatch.Draw(_pixel, reloadBg, Color.Black * 0.5f);
            _spriteBatch.Draw(_pixel, reloadFill, Color.White);
        }
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
        return new Vector2(0, 0);
    }

    private Texture2D GetGunTexture(Gun gun)
    {
        if (_gunTextures.TryGetValue(gun.SpriteName, out var texture)) return texture;
        return _pixel;
    }
}
