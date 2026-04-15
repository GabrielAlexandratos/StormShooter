using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace StormShooter;

public class Game1 : Game
{
    public GraphicsDeviceManager Graphics { get; private set; }
    public SpriteBatch SpriteBatch { get; private set; }

    private Scene _currentScene;
    private Scene _nextScene;

    public Game1()
    {
        Graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = false;

        int monitorHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        int initialScale = Math.Max(1, (monitorHeight / Settings.VirtualHeight) - 1);
        Graphics.PreferredBackBufferWidth = Settings.VirtualWidth * initialScale;
        Graphics.PreferredBackBufferHeight = Settings.VirtualHeight * initialScale;
        Graphics.ApplyChanges();
        Window.AllowUserResizing = true;
    }

    public void ChangeScene(Scene scene)
    {
        _nextScene = scene;
    }

    protected override void LoadContent()
    {
        SpriteBatch = new SpriteBatch(GraphicsDevice);
        
        ChangeScene(new MainMenuScene(this));
    }

    protected override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        if (kb.IsKeyDown(Keys.Escape)) Exit();

        if (_nextScene != null)
        {
            _currentScene?.UnloadContent();
            _currentScene = _nextScene;
            _currentScene.LoadContent();
            _nextScene = null;
        }

        _currentScene?.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _currentScene?.Draw(gameTime, SpriteBatch);

        base.Draw(gameTime);
    }
}