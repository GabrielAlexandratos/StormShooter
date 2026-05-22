using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace StormShooter;

public class GameplayScene : Scene
{
    private RenderTarget2D _renderTarget;
    private readonly SamplerState _pointSampler = SamplerState.PointClamp;

    private int VirtualWidth => Settings.VirtualWidth;
    private int VirtualHeight => Settings.VirtualHeight;

    private Texture2D _crosshairTexture;
    private Texture2D _pixel;
    private Texture2D _dropSprite;
    private SpriteFont _hudFont;
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
    private float _zoom = 1f;
    private float _vignetteAlpha = 0f;
    private Texture2D _vignetteTexture;
    private Texture2D _circleTexture;

    private ParticleSystem _particles = new();
    private PersistentParticleSystem _casings = new();
    private EnemyManager _enemyManager;
    private LightingRenderer _lighting;
    private KeyboardState _previousKb;

    private Tile[,] _grid;
    private int _gridWidth = 50;
    private int _gridHeight = 35;
    private int _tileSize = 16;

    private Dictionary<string, Texture2D> _gunTextures = new();
    private Dictionary<TileType, Texture2D[]> _tileTextures = new();

    private List<DroppedGun> _droppedGuns = new();

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

    public GameplayScene(Game1 game) : base(game)
    {
        
    }

    public override void LoadContent()
    {
        _renderTarget = new RenderTarget2D(Game.GraphicsDevice, VirtualWidth, VirtualHeight);

        _pixel = new Texture2D(Game.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _vignetteTexture = new Texture2D(Game.GraphicsDevice, VirtualWidth, VirtualHeight);
        var vignPixels = new Color[VirtualWidth * VirtualHeight];
        Vector2 vignCenter = new Vector2(VirtualWidth / 2f, VirtualHeight / 2f);
        float innerRadius = Math.Min(VirtualWidth, VirtualHeight) * 0.3f;
        float outerRadius = Math.Min(VirtualWidth, VirtualHeight) * 0.72f;
        for (int y = 0; y < VirtualHeight; y++)
        {
            for (int x = 0; x < VirtualWidth; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), vignCenter);
                float t = MathHelper.Clamp((dist - innerRadius) / (outerRadius - innerRadius), 0f, 1f);
                vignPixels[y * VirtualWidth + x] = Color.Black * (t * t);
            }
        }
        _vignetteTexture.SetData(vignPixels);

        int cr = 8;
        _circleTexture = new Texture2D(Game.GraphicsDevice, cr * 2, cr * 2);
        var circlePixels = new Color[cr * 2 * cr * 2];
        Vector2 circleCenter = new Vector2(cr - 0.5f, cr - 0.5f);
        for (int y = 0; y < cr * 2; y++)
            for (int x = 0; x < cr * 2; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), circleCenter);
                float a = MathHelper.Clamp(cr - dist, 0f, 1f);
                circlePixels[y * cr * 2 + x] = Color.White * a;
            }
        _circleTexture.SetData(circlePixels);

        // placeholder ammo drop sprite
        _dropSprite = new Texture2D(Game.GraphicsDevice, 16, 8);
        var dropPixels = new Color[16 * 8];
        for (int i = 0; i < dropPixels.Length; i++)
            dropPixels[i] = new Color(255, 220, 60);
        _dropSprite.SetData(dropPixels);

        _hudFont = Game.Content.Load<SpriteFont>("Fonts/HudFont");

        _crosshairTexture = Game.Content.Load<Texture2D>("crosshair");
        _bulletTexture = Game.Content.Load<Texture2D>("bullet");

        _playerIdleTexture = Game.Content.Load<Texture2D>("player_idle");
        _playerWalkTexture = Game.Content.Load<Texture2D>("player_walk_new");

        _gunTextures["gun_scraprifle"] = Game.Content.Load<Texture2D>("gun_scraprifle");
        _gunTextures["gun_shotgun"] = Game.Content.Load<Texture2D>("gun_shotgun");
        _gunTextures["gun_asval"] = Game.Content.Load<Texture2D>("gun_asval");

        _tileTextures[TileType.Empty] = new[]
        {
            Game.Content.Load<Texture2D>("snow_floor_0"),
            Game.Content.Load<Texture2D>("snow_floor_1"),
            Game.Content.Load<Texture2D>("snow_floor_2"),
        };

        _tileTextures[TileType.Wall] = new[]
        {
            Game.Content.Load<Texture2D>("snow_wall_0"),
            Game.Content.Load<Texture2D>("snow_wall_1"),
        };

        _currentGun = GunData.ScrapRifle;

        _lighting = new LightingRenderer(Game.GraphicsDevice, VirtualWidth, VirtualHeight)
        {
            PlayerRadius = 60f,
            DimMultiplier = 10f,
            DimBrightness = 0.35f
        };

        SoundManager.Load(Game.Content);
        SoundManager.PlayWindAmbience(1.15f);

        _bulletManager = new BulletManager();
        _enemyManager = new EnemyManager();
        _gunController = new GunController(_random);

        _enemyManager.IsWall = IsWall;
        _enemyManager.Bullets = _bulletManager;
        _enemyManager.Particles = _particles;
        _enemyManager.Lighting = _lighting;
        _enemyManager.Rng = _random;
        _enemyManager.OnEnemyDropped += (pos, gun, ammo) => _droppedGuns.Add(new DroppedGun(pos, gun, ammo));

        LevelGenerator generator = new LevelGenerator();
        _grid = new Tile[_gridWidth, _gridHeight];

        var generated = generator.Generate(_gridWidth, _gridHeight);
        var rooms = generator.Rooms;

        for (int x = 0; x < _gridWidth; x++)
            for (int y = 0; y < _gridHeight; y++)
                _grid[x, y] = new Tile { Type = generated[x, y], Variant = _random.Next(0, 3) };

        Vector2 spawnPos = FindSpawnPosition();
        _player = new Player(spawnPos, Settings.PlayerSpeed, _playerIdleTexture, _playerWalkTexture);
        _player.OnFootstep += () => SoundManager.PlayRandom(0.25f, (_random.NextSingle() - 0.5f) * 0.08f, "snowstep1", "snowstep2", "snowstep3");
        _player.OnDeath += () => Game.ChangeScene(new LoseScene(Game));

        var spawner = new EnemySpawner(_grid, _tileSize);
        spawner.Spawn(rooms, _enemyManager, spawnPos);

        _cameraPos = spawnPos - new Vector2(VirtualWidth / 2f, VirtualHeight / 2f);
    }

    public override void Update(GameTime gameTime)
    {
        var mouse = Mouse.GetState();
        var kb = Keyboard.GetState();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _fpsTimer += dt;
        _frameCount++;
        if (_fpsTimer >= 1.0)
        {
            _currentFps = _frameCount;
            Game.Window.Title = $"StormShooter | FPS: {_currentFps}";
            _frameCount = 0;
            _fpsTimer -= 1.0;
        }

        if (_shakeTime > 0) _shakeTime -= dt;

        if (kb.IsKeyDown(Keys.Escape) && _previousKb.IsKeyUp(Keys.Escape))
        {
            Game.ChangeScene(new MainMenuScene(Game));
            return;
        }

        if (kb.IsKeyDown(Keys.F11) && _previousKb.IsKeyUp(Keys.F11))
            Game.Graphics.ToggleFullScreen();

        if (_hitStopTime > 0f)
        {
            _hitStopTime -= dt;
            return;
        }

        float targetZoom = _gunController.IsReloading ? 1.65f : 1f;
        _zoom = MathHelper.Lerp(_zoom, targetZoom, dt * 6f);
        _player.SpeedMultiplier = _gunController.IsReloading ? 0.3f : 1f;

        var destRect = GetDestinationRectangle();
        float currentScale = (float)destRect.Height / VirtualHeight;
        float mouseX = (mouse.X - destRect.X) / currentScale;
        float mouseY = (mouse.Y - destRect.Y) / currentScale;
        Vector2 cameraCenter = _cameraPos + new Vector2(VirtualWidth / 2f, VirtualHeight / 2f);
        Vector2 mouseWorld = cameraCenter + (new Vector2(mouseX, mouseY) - new Vector2(VirtualWidth / 2f, VirtualHeight / 2f)) / _zoom;

        bool reloading = _gunController.IsReloading;
        _vignetteAlpha = MathHelper.Lerp(_vignetteAlpha, reloading ? 1f : 0f, dt * 8f);

        _player.Update(dt, kb, mouseWorld, _currentGun, IsWall);
        _enemyManager.Update(dt, _player.Position, reloading ? null : _lighting);

        Gun selectedGun = null;
        if (kb.IsKeyDown(Keys.D1) && _previousKb.IsKeyUp(Keys.D1)) selectedGun = GunData.ScrapRifle;
        else if (kb.IsKeyDown(Keys.D2) && _previousKb.IsKeyUp(Keys.D2)) selectedGun = GunData.Shotgun;
        else if (kb.IsKeyDown(Keys.D3) && _previousKb.IsKeyUp(Keys.D3)) selectedGun = GunData.VAL;
        else if (kb.IsKeyDown(Keys.D4) && _previousKb.IsKeyUp(Keys.D4)) selectedGun = GunData.BurstRifle;

        if (selectedGun != null && _currentGun != selectedGun)
        {
            _currentGun = selectedGun;
            SwitchGun();
        }
        
        _gunController.Update(dt, mouse, kb, _previousKb, mouseWorld, _player, _currentGun, _bulletManager, _lighting, _casings, ref _shakeTime, ref _shakeStrength, ref _shakeOffset);
        _bulletManager.Update(dt, _enemyManager.Enemies, _particles, _casings, reloading ? null : _lighting, _currentGun, ref _hitStopTime, ref _shakeTime, ref _shakeStrength, VirtualWidth, VirtualHeight, _random, IsWall, _player);
        _particles.Update(dt);
        _casings.Update(dt);

        // Update dropped guns and handle pickup interaction
        bool fHeld = kb.IsKeyDown(Keys.F);
        var pickupToRemove = new List<DroppedGun>();
        foreach (var drop in _droppedGuns)
        {
            bool done = drop.Update(dt, _player.Position, fHeld);
            if (done)
            {
                _gunController.AddAmmo(drop.Gun.AmmoType, drop.AmmoCount);
                SoundManager.Play("unload");
                pickupToRemove.Add(drop);
            }
        }
        foreach (var drop in pickupToRemove)
            _droppedGuns.Remove(drop);

        if (!reloading)
            foreach (var drop in _droppedGuns)
                _lighting.AddLight(new LightSource(drop.Position, 35f, new Color(255, 220, 60) * 2f, 0.4f));

        Vector2 lookOffset = (mouseWorld - _player.Position) * 0.2f;
        Vector2 cameraTarget = _player.Position + lookOffset - new Vector2(VirtualWidth / 2f, VirtualHeight / 2f);

        _cameraPos = Vector2.Lerp(_cameraPos, cameraTarget, 10f * dt);
        _shakeOffset = Vector2.Lerp(_shakeOffset, Vector2.Zero, dt * 20f);
        _previousKb = kb;
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
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

        Vector2 cameraCenter = _cameraPos + new Vector2(VirtualWidth / 2f, VirtualHeight / 2f);
        Matrix lowResCamera =
            Matrix.CreateTranslation(-cameraCenter.X, -cameraCenter.Y, 0f) *
            Matrix.CreateScale(_zoom, _zoom, 1f) *
            Matrix.CreateTranslation(VirtualWidth / 2f, VirtualHeight / 2f, 0f);

        var lightMap = _lighting.BuildLightMap(spriteBatch, _player.Position, _cameraPos, dt);

        Game.GraphicsDevice.SetRenderTarget(_renderTarget);
        Game.GraphicsDevice.Clear(Color.Black);

        spriteBatch.Begin(samplerState: _pointSampler, transformMatrix: lowResCamera);

        int startX = Math.Max(0, (int)(_cameraPos.X / _tileSize));
        int startY = Math.Max(0, (int)(_cameraPos.Y / _tileSize));
        int endX = Math.Min(_gridWidth, startX + (VirtualWidth / _tileSize) + 2);
        int endY = Math.Min(_gridHeight, startY + (VirtualHeight / _tileSize) + 2);

        for (int x = startX; x < endX; x++)
            for (int y = startY; y < endY; y++)
            {
                var tile = _grid[x, y];
                if (_tileTextures.TryGetValue(tile.Type, out Texture2D[] textures))
                    spriteBatch.Draw(textures[tile.Variant % textures.Length], new Vector2(x * _tileSize, y * _tileSize), Color.White);
            }

        // Draw dropped guns
        foreach (var drop in _droppedGuns)
        {
            Vector2 worldPos = drop.Position;
            int spriteW = _dropSprite.Width;
            int spriteH = _dropSprite.Height;
            spriteBatch.Draw(_dropSprite,
                new Vector2(MathF.Round(worldPos.X - spriteW / 2f), MathF.Round(worldPos.Y - spriteH / 2f)),
                Color.White);
        }

        _casings.Draw(spriteBatch, _pixel, _circleTexture);
        _player.Draw(spriteBatch, GetGunTexture(_currentGun), _currentGun);
        _enemyManager.Draw(spriteBatch, _pixel);
        _bulletManager.Draw(spriteBatch, _bulletTexture);
        _particles.Draw(spriteBatch, _pixel);
        spriteBatch.End();

        Game.GraphicsDevice.SetRenderTarget(null);
        Game.GraphicsDevice.Clear(Color.Black);

        spriteBatch.Begin(samplerState: _pointSampler);
        spriteBatch.Draw(_renderTarget, finalDestRect, Color.White);
        spriteBatch.End();

        spriteBatch.Begin(blendState: MultiplyBlend, samplerState: _pointSampler);
        spriteBatch.Draw(lightMap, finalDestRect, Color.White);
        spriteBatch.End();

        if (_vignetteAlpha > 0.01f)
        {
            spriteBatch.Begin(samplerState: _pointSampler);
            spriteBatch.Draw(_vignetteTexture, finalDestRect, Color.White * _vignetteAlpha);
            spriteBatch.End();
        }

        // draw all UI
        spriteBatch.Begin(samplerState: _pointSampler);

        // Health bar 
        int hBarW = (int)(80 * currentScale);
        int hBarH = (int)(7 * currentScale);
        int hBarX = finalDestRect.X + (int)(10 * currentScale);
        int hBarY = finalDestRect.Y + (int)(10 * currentScale);
        int hBarFill = (int)(hBarW * (_player.Health / _player.MaxHealth));
        spriteBatch.Draw(_pixel, new Rectangle(hBarX, hBarY, hBarW, hBarH), Color.Black * 0.5f);
        spriteBatch.Draw(_pixel, new Rectangle(hBarX, hBarY, hBarFill, hBarH), Color.Red);

        // Ammo casings
        int currentAmmo = _gunController.GetCurrentAmmo(_currentGun);
        for (int i = 0; i < currentAmmo; i++)
        {
            int ax = finalDestRect.X + (int)(10 * currentScale) + (int)(i * 4 * currentScale);
            int ay = finalDestRect.Y + finalDestRect.Height - (int)(15 * currentScale);
            spriteBatch.Draw(_pixel, new Rectangle(ax, ay, (int)(2 * currentScale), (int)(6 * currentScale)), Color.Orange);
        }

        // Ammo pool count
        int poolAmmo = _gunController.GetPoolAmmo(_currentGun.AmmoType);
        Color poolColor = _currentGun.AmmoType switch
        {
            AmmoType.Light => new Color(180, 230, 255),
            AmmoType.Medium => new Color(255, 210, 100),
            AmmoType.Heavy => new Color(255, 120, 80),
            _               => Color.White
        };
        string poolLabel = _currentGun.AmmoType switch
        {
            AmmoType.Light => "LIGHT",
            AmmoType.Medium => "MEDIUM",
            AmmoType.Heavy => "HEAVY",
            _               => "---"
        };
        string poolText = $"{poolLabel} {poolAmmo}";

        float fontScale = currentScale * 0.6f;
        Vector2 textSize = _hudFont.MeasureString(poolText) * fontScale;
        Vector2 poolTextPos = new Vector2(
            finalDestRect.X + (int)(10 * currentScale),
            finalDestRect.Y + finalDestRect.Height - (int)(15 * currentScale) - textSize.Y - (int)(3 * currentScale)
        );
        // slight text shadow
        spriteBatch.DrawString(_hudFont, poolText, poolTextPos + Vector2.One * currentScale, Color.Black * 0.8f, 0f, Vector2.Zero, fontScale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_hudFont, poolText, poolTextPos, poolColor, 0f, Vector2.Zero, fontScale, SpriteEffects.None, 0f);

        // Reload bar above player
        if (_gunController.IsReloading)
        {
            Vector2 screenPos = WorldToScreen(_player.Position, finalDestRect, currentScale);
            int barW = (int)(40 * currentScale);
            int barH = (int)(6 * currentScale);
            Rectangle reloadBg = new Rectangle((int)screenPos.X - barW / 2, (int)screenPos.Y - (int)(40 * currentScale), barW, barH);
            float progress = 1f - (_gunController.ReloadProgress / _currentGun.ReloadTime);
            Rectangle reloadFill = new Rectangle(reloadBg.X, reloadBg.Y, (int)(reloadBg.Width * progress), reloadBg.Height);
            spriteBatch.Draw(_pixel, reloadBg, Color.Black * 0.5f);
            spriteBatch.Draw(_pixel, reloadFill, Color.White);
        }

        // pickup prompt
        foreach (var drop in _droppedGuns)
        {
            if (!drop.InRange) continue;

            Vector2 screenPos = WorldToScreen(drop.Position, finalDestRect, currentScale);

            if (!drop.IsBeingInteracted)
            {
                string textPopup = "[F] Unload";
                float textScale = currentScale * 0.5f;
                Vector2 popupSize = _hudFont.MeasureString(textPopup) * textScale;
                Vector2 pPos = new Vector2(screenPos.X - popupSize.X / 2f, screenPos.Y - (int)(18 * currentScale));
                spriteBatch.DrawString(_hudFont, textPopup, pPos + Vector2.One * currentScale, Color.Black * 0.8f, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_hudFont, textPopup, pPos, new Color(255, 220, 60), 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
            }
        }

        foreach (var drop in _droppedGuns)
        {
            if (!drop.IsBeingInteracted) continue;

            Vector2 screenPos = WorldToScreen(drop.Position, finalDestRect, currentScale);
            int barW = (int)(24 * currentScale);
            int barH = (int)(3  * currentScale);
            int barX = (int)screenPos.X - barW / 2;
            int barY = (int)screenPos.Y - (int)(12 * currentScale);
            int fillW = (int)(barW * drop.InteractProgress);

            spriteBatch.Draw(_pixel, new Rectangle(barX, barY, barW, barH), Color.Black * 0.6f);
            spriteBatch.Draw(_pixel, new Rectangle(barX, barY, fillW, barH), Color.White);
        }

        Vector2 crosshairPos = new Vector2(mouse.X, mouse.Y);
        Vector2 crosshairOrigin = new Vector2(_crosshairTexture.Width / 2, _crosshairTexture.Height / 2);
        spriteBatch.Draw(_crosshairTexture, crosshairPos, null, Color.White, 0f, crosshairOrigin, 3f, SpriteEffects.None, 0f);

        spriteBatch.End();
    }

    public override void UnloadContent()
    {
        SoundManager.StopWindAmbience();
        _lighting?.Dispose();
        _renderTarget?.Dispose();
        _pixel?.Dispose();
        _circleTexture?.Dispose();
        _dropSprite?.Dispose();
        _vignetteTexture?.Dispose();
        base.UnloadContent();
    }

    private Rectangle GetDestinationRectangle()
    {
        int sw = Game.GraphicsDevice.PresentationParameters.BackBufferWidth;
        int sh = Game.GraphicsDevice.PresentationParameters.BackBufferHeight;

        float scaleX = (float)sw / VirtualWidth;
        float scaleY = (float)sh / VirtualHeight;
        float scale = Math.Min(scaleX, scaleY);

        int rw = (int)(VirtualWidth * scale);
        int rh = (int)(VirtualHeight * scale);

        return new Rectangle((sw - rw) / 2, (sh - rh) / 2, rw, rh);
    }

    private Vector2 WorldToScreen(Vector2 worldPos, Rectangle destRect, float scale)
    {
        Vector2 cameraCenter = _cameraPos + new Vector2(VirtualWidth / 2f, VirtualHeight / 2f);
        Vector2 offset = (worldPos - cameraCenter) * _zoom;
        return new Vector2(
            destRect.X + (VirtualWidth / 2f + offset.X) * scale,
            destRect.Y + (VirtualHeight / 2f + offset.Y) * scale
        );
    }

    private bool IsWall(Vector2 pos)
    {
        int tx = (int)(pos.X / _tileSize);
        int ty = (int)(pos.Y / _tileSize);
        if (tx < 0 || ty < 0 || tx >= _gridWidth || ty >= _gridHeight) return true;
        return _grid[tx, ty].Type == TileType.Wall;
    }

    private Vector2 FindSpawnPosition()
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
        return _gunTextures.GetValueOrDefault(gun.SpriteName, _pixel);
    }

    private void SwitchGun()
    {
        _gunController.CancelReload();
        SoundManager.Play("equip", 0.3f);
    }
}
