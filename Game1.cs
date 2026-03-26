using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace StormShooter;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private RenderTarget2D _renderTarget;
    private readonly SamplerState _pointSampler = SamplerState.PointClamp;

    private static readonly int VirtualWidth  = Settings.VirtualWidth;
    private static readonly int VirtualHeight = Settings.VirtualHeight;
    private static readonly int WindowScale   = Settings.WindowScale;

    private readonly int _windowWidth  = VirtualWidth * WindowScale;
    private readonly int _windowHeight = VirtualHeight * WindowScale;

    private Vector2 _playerPos = new Vector2(150, 150);
    private Texture2D _pixel;

    private readonly float _playerSpeed = Settings.PlayerSpeed;

    private Gun _currentGun;
    private float _shotCooldown;

    private Vector2 _gunPos;
    private float _gunRotation;
    private bool _gunFlip;

    private List<Bullet> _bullets = new();
    private MouseState _previousMouse;

    private Random _random = new();

    private float _shakeTime;
    private float _shakeStrength;
    private Vector2 _recoilOffset;
    private float _recoilRecoverSpeed = 10f;

    private LightingRenderer _lighting;

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
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = _windowWidth;
        _graphics.PreferredBackBufferHeight = _windowHeight;
        _graphics.ApplyChanges();

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void LoadContent()
    {
        _renderTarget = new RenderTarget2D(GraphicsDevice, VirtualWidth, VirtualHeight);
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _currentGun = GunData.Rifle;
        
        _lighting = new LightingRenderer(GraphicsDevice, VirtualWidth, VirtualHeight);
        
        // Player light source settings
        _lighting.PlayerRadius = 60f;
        _lighting.DimMultiplier = 2.5f;
        _lighting.DimBrightness = 0.35f;
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        float dt  = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var kb = Keyboard.GetState();
        var mouse = Mouse.GetState();

        // Player movement
        if (kb.IsKeyDown(Keys.W)) _playerPos.Y -= _playerSpeed * dt;
        if (kb.IsKeyDown(Keys.S)) _playerPos.Y += _playerSpeed * dt;
        if (kb.IsKeyDown(Keys.A)) _playerPos.X -= _playerSpeed * dt;
        if (kb.IsKeyDown(Keys.D)) _playerPos.X += _playerSpeed * dt;

        Vector2 mouseWorld = new Vector2(mouse.X, mouse.Y) / WindowScale;
        Vector2 direction = mouseWorld - _playerPos;

        if (direction.LengthSquared() > 0.0001f)
            direction.Normalize();
        else
            direction = Vector2.Zero;

        _gunPos = _playerPos + direction * 6f + _recoilOffset;
        _gunRotation = (float)Math.Atan2(direction.Y, direction.X);
        _gunFlip = direction.X < 0;

        bool canShoot = _shotCooldown <= 0f;
        bool wantsToShoot =
            _currentGun.Automatic
                ? mouse.LeftButton == ButtonState.Pressed
                : (mouse.LeftButton == ButtonState.Pressed &&
                   _previousMouse.LeftButton == ButtonState.Released);

        if (_shotCooldown > 0f) _shotCooldown -= dt;

        if (wantsToShoot && canShoot)
        {
            float spread = _currentGun.Spread;
            float angleOffset = ((float)_random.NextDouble() - 0.5f) * spread;
            float angle = (float)Math.Atan2(direction.Y, direction.X) + angleOffset;

            Vector2 shootDir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            Vector2 velocity = shootDir * _currentGun.BulletSpeed;
            Vector2 muzzlePos = _gunPos + shootDir * 8f;

            _bullets.Add(new Bullet(muzzlePos, velocity));

            _shakeTime = _currentGun.ShakeDuration;
            _shakeStrength = _currentGun.ShakeStrength;
            _recoilOffset = -shootDir * _currentGun.Recoil;
            _shotCooldown = 1f / _currentGun.FireRate;

            // Muzzle flash: main layer light + colour glow
            _lighting.AddFlash(muzzlePos, 40f, Color.White,  duration: 0.06f);
            _lighting.AddFlash(muzzlePos, 30f, Color.Yellow, duration: 0.10f);
        }

        for (int i = _bullets.Count - 1; i >= 0; i--)
        {
            _bullets[i].Update(dt);
            if (_bullets[i].IsOffscreen(VirtualWidth, VirtualHeight))
                _bullets.RemoveAt(i);
            else
            {
                // Bullet: main layer light
                _lighting.AddLight(new LightSource(_bullets[i].Position, 20f, Color.White, lifetime: 0.04f));
                // Bullet: colour light
                _lighting.AddLight(new LightSource(_bullets[i].Position, 5.5f, Color.Orange, lifetime: 0.04f));
            }
        }

        _previousMouse = mouse;
        _recoilOffset = Vector2.Lerp(_recoilOffset, Vector2.Zero, dt * _recoilRecoverSpeed);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        var lightMap = _lighting.BuildLightMap(_spriteBatch, _playerPos, dt);

        // Draw game to render target
        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(new Color(30, 30, 30));

        _spriteBatch.Begin(samplerState: _pointSampler);

        // Background dots grid
        for (int x = 0; x < VirtualWidth;  x += 10)
        for (int y = 0; y < VirtualHeight; y += 10)
            _spriteBatch.Draw(_pixel, new Vector2(x, y), Color.DarkGray * 0.6f);

        // Player
        _spriteBatch.Draw(_pixel,
            new Vector2((int)_playerPos.X, (int)_playerPos.Y),
            null, Color.White, 0f,
            new Vector2(0.5f, 0.5f), new Vector2(10, 10),
            SpriteEffects.None, 0f);

        // Gun
        _spriteBatch.Draw(_pixel,
            new Vector2((int)_gunPos.X, (int)_gunPos.Y),
            null, Color.Red, _gunRotation,
            new Vector2(0f, 0.5f), new Vector2(11, 4),
            _gunFlip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);

        // Bullets
        foreach (var bullet in _bullets)
            bullet.Draw(_spriteBatch, _pixel);

        _spriteBatch.End();

        // Apply lights to the game world
        // This darkens the pixels that are not in the light
        _spriteBatch.Begin(blendState: MultiplyBlend, samplerState: _pointSampler);
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

        _spriteBatch.Begin(samplerState: _pointSampler);
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
}