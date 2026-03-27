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

    private Vector2 _playerPos = new Vector2(150, 150);
    private Texture2D _pixel;

    private readonly float _playerSpeed = Settings.PlayerSpeed;

    private Gun _currentGun;
    private float _shotCooldown;

    private Vector2 _gunPos;
    private float _gunRotation;
    private float _finalRotation;
    private bool _gunFlip;

    private readonly List<Bullet> _bullets = new();
    private MouseState _previousMouse;

    private readonly Random _random = new();

    private float _shakeTime;
    private float _shakeStrength;
    private Vector2 _recoilOffset;
    private float _recoilRotation;
    private readonly float _recoilRecoverSpeed = 10f;

    private float _hitStopTime = 0.05f;

    private ParticleSystem _particles = new();
    
    private List<Enemy> _enemies = new();
    
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
        _pixel.SetData([Color.White]);

        _currentGun = GunData.Rifle;
        
        _lighting = new LightingRenderer(GraphicsDevice, VirtualWidth, VirtualHeight)
        {
            // Player light source settings
            PlayerRadius = 60f,
            DimMultiplier = 100f,
            DimBrightness = 0.35f
        };

        _enemies.Add(new Enemy(new Vector2(100, 100)));
        _enemies.Add(new Enemy(new Vector2(200, 100)));
        _enemies.Add(new Enemy(new Vector2(200, 200)));
        
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        float dt  = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var kb = Keyboard.GetState();
        var mouse = Mouse.GetState();
        
        if (_hitStopTime > 0f)
        {
            _hitStopTime -= dt;
            return;
        }

        // Player movement
        if (kb.IsKeyDown(Keys.W)) _playerPos.Y -= _playerSpeed * dt;
        if (kb.IsKeyDown(Keys.S)) _playerPos.Y += _playerSpeed * dt;
        if (kb.IsKeyDown(Keys.A)) _playerPos.X -= _playerSpeed * dt;
        if (kb.IsKeyDown(Keys.D)) _playerPos.X += _playerSpeed * dt;

        // Update enemies
        foreach (var e in _enemies)
        {
            e.Update(dt);

            // Enemy emits constant light
            _lighting.AddLight(new LightSource(e.Position, 45f, Color.White * 1.5f, lifetime: 0.15f));
        }

        if (kb.IsKeyDown(Keys.D1)) _currentGun = GunData.Rifle;
        if (kb.IsKeyDown(Keys.D2)) _currentGun = GunData.Smg;
        if (kb.IsKeyDown(Keys.D3)) _currentGun = GunData.Pistol;

        Vector2 mouseWorld = new Vector2(mouse.X, mouse.Y) / WindowScale;
        Vector2 direction = mouseWorld - _playerPos;

        if (direction.LengthSquared() > 0.0001f)
            direction.Normalize();
        else
            direction = Vector2.Zero;

        _gunPos = _playerPos + direction * 6f + _recoilOffset;
        _gunRotation = (float)Math.Atan2(direction.Y, direction.X);
        _finalRotation = _gunRotation + _recoilRotation;
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
            // Get all the data needed for a bullet to shoot
            float spread = _currentGun.Spread;
            float angleOffset = ((float)_random.NextDouble() - 0.5f) * spread;
            float angle = (float)Math.Atan2(direction.Y, direction.X) + angleOffset;

            Vector2 shootDir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            Vector2 velocity = shootDir * _currentGun.BulletSpeed;
            Vector2 muzzlePos = _gunPos + shootDir * 8f;

            _bullets.Add(new Bullet(muzzlePos, velocity));

            // Setting up gun impact fx variables
            _shakeTime = _currentGun.ShakeDuration;
            _shakeStrength = _currentGun.ShakeStrength;
            _recoilOffset = -shootDir * _currentGun.Recoil * 1.3f;
            _recoilRotation = (_random.NextSingle()) * 0.12f;
            _shotCooldown = 1f / _currentGun.FireRate;

            // Muzzle flash: main layer light + colour glow
            _lighting.AddFlash(muzzlePos, 40f, Color.White,  duration: 0.06f);
            _lighting.AddFlash(muzzlePos, 30f, Color.Yellow, duration: 0.10f);
        }

        for (int i = _bullets.Count - 1; i >= 0; i--)
        {
            var b = _bullets[i];
            b.Update(dt);

            bool hit = false;

            foreach (var e in _enemies)
            {
                float dist = Vector2.Distance(b.Position, e.Position);

                if (dist < e.Radius)
                {
                    Vector2 dir = Vector2.Normalize(b.Velocity);
                    Vector2 knockback = dir * 35f;

                    e.Hit(knockback, _currentGun.Damage);

                    // Hit stop + screen shake
                    _hitStopTime = _currentGun.HitStop;
                    _shakeTime = 0.03f;
                    _shakeStrength = 0f;

                    /*
                    _particles.Add(new Particle
                    {
                        Position = e.Position,
                        Velocity = Vector2.Zero,
                        Life = 0.05f,
                        MaxLife = 0.05f,
                        Size = 7f,
                        Rotation = 0f,
                        Color = Color.White,
                        Drag = 0f,
                        Gravity = 0f
                    });
                    */

                    // Spark particles on enemy hit spray in a cone shape
                    int sparkCount = 5 + _random.Next(3);
                    for (int j = 0; j < sparkCount; j++)
                    {
                        // Cone shape
                        float angle = (float)Math.Atan2(dir.Y, dir.X)
                                    + (_random.NextSingle() - 0.5f) * 1.8f; // ~±52°
                        Vector2 sparkDir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                        float speed2 = 180f + _random.NextSingle() * 320f; // updated speed

                        // Alternate between red and yellow (blood/sparks)
                        Color sparkColor = j % 2 == 0
                            ? new Color(220, 30, 30)
                            : new Color(255, 210, 80);

                        _particles.Add(new Particle
                        {
                            Position = e.Position,
                            Velocity = sparkDir * speed2,
                            Life = 0.18f + _random.NextSingle() * 0.1f,
                            MaxLife = 0.30f,
                            Size = 6f + _random.NextSingle() * 4f,
                            Rotation = angle,
                            Color = sparkColor,
                            Drag = 6f,    
                            Gravity = 60f
                        });
                    }

                    // White sparks
                    for (int j = 0; j < 2; j++)
                    {
                        float offset = (_random.NextSingle() - 0.5f) * 0.5f;
                        float chipAngle = (float)Math.Atan2(dir.Y, dir.X) + offset;
                        Vector2 chipDir = new Vector2((float)Math.Cos(chipAngle), (float)Math.Sin(chipAngle));

                        _particles.Add(new Particle
                        {
                            Position = e.Position,
                            Velocity = chipDir * (400f + _random.NextSingle() * 300f),
                            Life = 0.10f,
                            MaxLife = 0.10f,
                            Size = 5f,
                            Rotation = chipAngle,
                            Color = Color.White,
                            Drag = 6f,
                            Gravity = 0f
                        });
                    }

                    hit = true;
                    break;
                }
            }

            if (hit || b.IsOffscreen(VirtualWidth, VirtualHeight))
            {
                _bullets.RemoveAt(i);
            }
            else
            {
                _lighting.AddLight(new LightSource(b.Position, 20f, Color.White, lifetime: 0.04f));
                _lighting.AddLight(new LightSource(b.Position, 5f, Color.Orange, lifetime: 0.04f));
            }
        }

        _previousMouse = mouse;
        _recoilOffset = Vector2.Lerp(_recoilOffset, Vector2.Zero, dt * _recoilRecoverSpeed);
        _recoilRotation = MathHelper.Lerp(_recoilRotation, 0f, dt * _recoilRecoverSpeed);

        _enemies.RemoveAll(e => e.IsDead());

        // Update particles
        _particles.Update(dt);

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
            null, Color.Red, _finalRotation,
            new Vector2(0f, 0.5f), new Vector2(11, 4),
            _gunFlip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);

        // Bullets
        foreach (var bullet in _bullets)
            bullet.Draw(_spriteBatch, _pixel);

        // Enemies
        foreach (var e in _enemies)
            e.Draw(_spriteBatch, _pixel);

        // Particles
        _particles.Draw(_spriteBatch, _pixel);

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