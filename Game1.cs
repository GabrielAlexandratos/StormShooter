using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace StormShooter;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private RenderTarget2D _renderTarget;
    private SamplerState _pointSampler = SamplerState.PointClamp;

    private static int _virtualWidth = 150;
    private static int _virtualHeight = 150;
    private int _windowWidth = _virtualWidth * 4;
    private int _windowHeight = _virtualHeight * 4;
    
    private Vector2 _playerPos = new Vector2(120, 90);
    private Texture2D _playerTempTexture;
    private float _playerSpeed = 50;
    

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);

        _graphics.PreferredBackBufferWidth = _windowWidth;
        _graphics.PreferredBackBufferHeight = _windowHeight;

        _graphics.ApplyChanges();
        
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _renderTarget = new RenderTarget2D(
            GraphicsDevice,
            _virtualWidth,
            _virtualHeight);
        
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        _playerTempTexture = new Texture2D(GraphicsDevice, 1, 1);
        _playerTempTexture.SetData([Color.White]);

        // TODO: use this.Content to load your game content here
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        var keyboard = Keyboard.GetState();

        // Vertical movement
        if (keyboard.IsKeyDown(Keys.W))
        {
            _playerPos.Y -= _playerSpeed * dt;
        }
        if (keyboard.IsKeyDown(Keys.S))
        {
            _playerPos.Y += _playerSpeed * dt;
        }

        // Horizontal movement
        if (keyboard.IsKeyDown(Keys.A))
        {
            _playerPos.X -= _playerSpeed * dt;
        }
        if (keyboard.IsKeyDown(Keys.D))
        {
            _playerPos.X += _playerSpeed * dt;
        }

        // TODO: Add your update logic here

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkSlateGray);
        
        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(samplerState: _pointSampler);

        var drawPos = new Vector2(
            (int)_playerPos.X,
            (int)_playerPos.Y
            );
        
        _spriteBatch.Draw(
            _playerTempTexture,
            _playerPos,
            null,
            Color.White,
            0f,
            Vector2.Zero,
            new Vector2(5, 5),
            SpriteEffects.None,
            0f
        );
        
        _spriteBatch.End();

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);

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
