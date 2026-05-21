using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace StormShooter;

public class LoseScene : Scene
{
    private SpriteFont _font;
    private KeyboardState _previousKb;

    public LoseScene(Game1 game) : base(game) { }

    public override void LoadContent()
    {
        _font = Game.Content.Load<SpriteFont>("Fonts/HudFont");
    }

    public override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        if (kb.IsKeyDown(Keys.Enter) && _previousKb.IsKeyUp(Keys.Enter))
            Game.ChangeScene(new MainMenuScene(Game));
        _previousKb = kb;
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Game.GraphicsDevice.Clear(Color.Black);

        int w = Game.GraphicsDevice.PresentationParameters.BackBufferWidth;
        int h = Game.GraphicsDevice.PresentationParameters.BackBufferHeight;

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        string title = "You Died :(";
        string sub = "Press Enter to retry\ntry not to die so quickly next time";

        Vector2 titleSize = _font.MeasureString(title) * 2f;
        Vector2 subSize = _font.MeasureString(sub);

        Vector2 titlePos = new Vector2(w / 2f - titleSize.X / 2f, h / 2f - titleSize.Y - 10f);
        Vector2 subPos = new Vector2(w / 2f - subSize.X / 2f, h / 2f + 10f);

        spriteBatch.DrawString(_font, title, titlePos + Vector2.One * 2f, Color.Black * 0.8f, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_font, title, titlePos, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);

        spriteBatch.DrawString(_font, sub, subPos + Vector2.One, Color.Black * 0.8f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_font, sub, subPos, Color.Gray, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

        spriteBatch.End();
    }
}
