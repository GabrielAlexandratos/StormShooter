using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace StormShooter;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private Vector2 _playerPos = new Vector2(400, 300);
    private Texture2D _playerTempTexture;
    private float _playerSpeed = 200;
    

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);

        _graphics.PreferredBackBufferWidth = 700;
        _graphics.PreferredBackBufferHeight = 700;
        
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
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();

        _spriteBatch.Draw(_playerTempTexture, _playerPos, null, Color.White, 0f, Vector2.Zero, new Vector2(20, 20), SpriteEffects.None, 0f);

        _spriteBatch.End();
        
        base.Draw(gameTime);
    }
}
