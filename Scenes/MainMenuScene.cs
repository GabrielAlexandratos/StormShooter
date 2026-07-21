using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace StormShooter;

public class MainMenuScene : Scene
{
    private Texture2D _menuCrosshair;

    private SpriteFont _font;
    
    public MainMenuScene(Game1 game) : base(game) { }

    public override void LoadContent()
    {
        _menuCrosshair = Game.Content.Load<Texture2D>("crosshair");
        _font = Game.Content.Load<SpriteFont>("Fonts/HudFont");
    }

    public override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        if (kb.IsKeyDown(Keys.Enter))
            Game.ChangeScene(new GameplayScene(Game));
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Game.GraphicsDevice.Clear(Color.Black);
        
        int w = Game.GraphicsDevice.PresentationParameters.BackBufferWidth;
        int h = Game.GraphicsDevice.PresentationParameters.BackBufferHeight;

        var mouse = Mouse.GetState();
        Vector2 crosshairPos = new Vector2(mouse.X, mouse.Y);
        Vector2 crosshairOrigin = new Vector2(_menuCrosshair.Width / 2f, _menuCrosshair.Height / 2f);

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        string title = "Rouge Reload";
        string flavText = "Made by Gabriel Alexandratos";
        string subTitle = "Press [Enter] To Start";
        
        Vector2 titleSize = _font.MeasureString(title) * 2f;
        Vector2 subSize = _font.MeasureString(subTitle);
        
        Vector2 titlePos = new Vector2(40, 40);
        Vector2 flavPos = new Vector2(253, 118);
        Vector2 subPos = new Vector2(200, h / 2f + 10f);

        spriteBatch.DrawString(_font, title, titlePos + Vector2.One * 2f, Color.Black * 0.8f, 0f, Vector2.Zero, 3f, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_font, title, titlePos, Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 0f);

        spriteBatch.DrawString(_font, flavText, flavPos, Color.Yellow, -0.23f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
        
        spriteBatch.DrawString(_font, subTitle, subPos + Vector2.One, Color.Black * 0.8f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_font, subTitle, subPos, Color.Gray, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

        string tutorial = "---How To Play---" +
                          "\n- The goal is to kill all the enemies on each stage," +
                          "\n once you have done so a door to the next stage will appear." +
                          "\n- Be careful of running out of ammo, and taking hits" +
                          "\n as you will not heal damage taken." +
                          "\n" +
                          "\n---Controls---" +
                          "\nWASD: movement" +
                          "\nR: reload" +
                          "\nMouse1: shoot" +
                          "\nE: interact" +
                          "\nG: unload ammo from weapon" +
                          "\n1 & 2: switch to primary and sidearm";
        Vector2 tutorialSize = _font.MeasureString(tutorial);
        Vector2 tutorialPos = new Vector2(w - tutorialSize.X - 20, h - tutorialSize.Y-20);
        
        spriteBatch.DrawString(_font, tutorial, tutorialPos, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        
        
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
