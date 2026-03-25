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

    private static readonly int VirtualWidth = Settings.VirtualWidth;
    private static readonly int VirtualHeight = Settings.VirtualHeight;
    private static readonly int WindowScale = Settings.WindowScale;

    private readonly int _windowWidth = VirtualWidth * WindowScale;
    private readonly int _windowHeight = VirtualHeight * WindowScale;

    private Vector2 _playerPos = new Vector2(150, 150);
    private Texture2D _pixel;

    private readonly float _playerSpeed = Settings.PlayerSpeed;

    private Vector2 _gunPos;
    private float _gunRotation;
    private bool _gunFlip;
    
    private List<Bullet> _bullets = new List<Bullet>();
    private MouseState _previousMouse;
    Random _random = new Random();

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
        _renderTarget = new RenderTarget2D(
            GraphicsDevice,
            VirtualWidth,
            VirtualHeight
        );

        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Initializing keyboard and mouse for input
        var kb = Keyboard.GetState();
        var mouse = Mouse.GetState();
        
        bool justClicked =
            mouse.LeftButton == ButtonState.Pressed &&
            _previousMouse.LeftButton == ButtonState.Released;

        // Player movement
        if (kb.IsKeyDown(Keys.W)) _playerPos.Y -= _playerSpeed * dt;
        if (kb.IsKeyDown(Keys.S)) _playerPos.Y += _playerSpeed * dt;
        if (kb.IsKeyDown(Keys.A)) _playerPos.X -= _playerSpeed * dt;
        if (kb.IsKeyDown(Keys.D)) _playerPos.X += _playerSpeed * dt;

        Vector2 mouseScreen = new Vector2(mouse.X, mouse.Y);
        Vector2 mouseWorld = mouseScreen / WindowScale;

        Vector2 direction = mouseWorld - _playerPos;

        if (direction.LengthSquared() > 0.0001f)
            direction.Normalize();
        else
            direction = Vector2.Zero;

        // Handle the gun updating
        _gunPos = _playerPos + direction * 6f;
        _gunRotation = (float)Math.Atan2(direction.Y, direction.X);
        _gunFlip = direction.X < 0;

        // Shoot on left mouse click
        if (justClicked)
        {
            float spread = 0.2f;
            float angleOffset = ((float)_random.NextDouble() - 0.5f) * spread;
            
            float angle = (float)Math.Atan2(direction.Y, direction.X);
            angle += angleOffset;

            Vector2 shootDirection = new Vector2(
                (float)Math.Cos(angle),
                (float)Math.Sin(angle)
                );

            float speed = Settings.BulletSpeed;
            Vector2 velocity = shootDirection * speed;
            Vector2 muzzlePos = _gunPos + shootDirection * 8f;
            _bullets.Add(new Bullet(muzzlePos, velocity));
        }

        foreach (var bullet in _bullets)
        {
            bullet.Update(dt);
        }

        _previousMouse = mouse;
        
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(samplerState: _pointSampler);

        for (int x = 0; x < VirtualWidth; x += 10)
        {
            for (int y = 0; y < VirtualHeight; y += 10)
            {
                _spriteBatch.Draw(_pixel, new Vector2(x, y), Color.DarkGray * 0.2f);
            }
        }
        
        // Snapping to the pixel perfect grid/resolution
        var playerDrawPos = new Vector2(
            (int)_playerPos.X,
            (int)_playerPos.Y
        );
        var gunDrawPos = new Vector2(
            (int)_gunPos.X,
            (int)_gunPos.Y
        );

        
        // Drawing player
        _spriteBatch.Draw(
            _pixel,
            playerDrawPos,
            null,
            Color.White,
            0f,
            new Vector2(0.5f, 0.5f),
            new Vector2(10, 10),
            SpriteEffects.None,
            0f
        );

        // Drawing gun
        _spriteBatch.Draw(
            _pixel,
            gunDrawPos,
            null,
            Color.Red,
            _gunRotation,
            new Vector2(0f, 0.5f),
            new Vector2(11, 4),
            _gunFlip ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
            0f
        );
        
        // Drawing bullets
        foreach (var bullet in _bullets)
        {
            bullet.Draw(_spriteBatch, _pixel);
        }

        _spriteBatch.End();

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(new Color(10, 10, 10));

        _spriteBatch.Begin(samplerState: _pointSampler);

        _spriteBatch.Draw(
            _renderTarget,
            new Rectangle(0, 0, _windowWidth, _windowHeight),
            Color.White
        );

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}