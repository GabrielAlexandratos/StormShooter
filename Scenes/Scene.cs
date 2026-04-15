using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StormShooter;

public abstract class Scene
{
    protected Game1 Game;

    public Scene(Game1 game)
    {
        Game = game;
    }

    public abstract void LoadContent();
    public abstract void Update(GameTime gameTime);
    public abstract void Draw(GameTime gameTime, SpriteBatch spriteBatch);
    
    public virtual void UnloadContent() { }
}