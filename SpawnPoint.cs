using Microsoft.Xna.Framework;

namespace StormShooter;

public enum SpawnType
{
    Open,
    Edge,
    Group
}

public class SpawnPoint
{
    public Vector2 Position;
    public SpawnType Type;

    public SpawnPoint(Vector2 pos, SpawnType type)
    {
        Position = pos;
        Type = type;
    }
}
