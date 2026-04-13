using System.Drawing;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace StormShooter;

public class AnimatedSprite
{
    private Texture2D _texture;
    private int _frameAmount;
    private int _frameWidth;
    private int _frameHeight;
    private float _fps;
    private float _timer;
    private int _currentFrame;

    public AnimatedSprite(Texture2D texture, int frameAmount, float fps)
    {
        _texture = texture;
        _frameAmount = frameAmount;
        _fps = fps;
        _frameWidth = _texture.Width / frameAmount;
        _frameHeight = _texture.Height;
    }

    public void Update(float dt)
    {
        _timer += dt;
        if (_timer > 1f / _fps)
        {
            _timer -= 1f / _fps;
            _currentFrame = (_currentFrame + 1) % _frameAmount;
        }
    }
    
    public Microsoft.Xna.Framework.Rectangle? GetSourceRect() => 
        new Rectangle(_currentFrame * _frameWidth, 0, _frameWidth, _frameHeight);
    
    public Texture2D Texture => _texture;
    public int FrameWidth => _frameWidth;
    public int FrameHeight => _frameHeight;
}