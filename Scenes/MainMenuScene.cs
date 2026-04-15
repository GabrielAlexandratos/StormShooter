using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace StormShooter;

public class MainMenuScene : Scene
{
    private Texture2D _menuCrosshair;
    
    public MainMenuScene(Game1 game) : base(game) { }

    public override void LoadContent()
    {
        _menuCrosshair = Game.Content.Load<Texture2D>("circle_crosshair");   
    }

    public override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();

        if (kb.IsKeyDown(Keys.Enter))
        {
            Game.ChangeScene(new GameplayScene(Game));
        }
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Game.GraphicsDevice.Clear(Color.DarkSlateGray);

        var mouse = Mouse.GetState();
        Vector2 crosshairPos = new Vector2(mouse.X, mouse.Y);
        Vector2 crosshairOrigin = new Vector2(_menuCrosshair.Width / 2f, _menuCrosshair.Height / 2f);

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        
        spriteBatch.Draw(
            _menuCrosshair, 
            crosshairPos, 
            null, 
            Color.White, 
            0f, 
            crosshairOrigin, 
            3f, 
            SpriteEffects.None, 
            0f
        );
        
        spriteBatch.End();
    }
}