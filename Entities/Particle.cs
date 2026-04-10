using Microsoft.Xna.Framework;

namespace StormShooter;

public class Particle
{
    public Vector2 Position;
    public Vector2 Velocity;

    public float Life;
    public float MaxLife;

    public float Size;
    public float Rotation;

    public Color Color;

    // slow the particles down
    public float Drag = 0f;
    
    public float Gravity = 0f;

    public void Update(float deltaTime)
    {
        Life -= deltaTime;

        float lifeRatio = Life / MaxLife;
        float dragFactor = MathHelper.Max(0f, 1f - Drag * deltaTime);
        Velocity *= dragFactor * (0.85f + 0.15f * lifeRatio);
        
        Velocity.Y += Gravity * deltaTime;

        Position += Velocity * deltaTime;
    }

    public float T => 1f - (Life / MaxLife);
}