namespace StormShooter;

public enum TileType
{
    Empty,
    Wall,
    Cover
}

public struct Tile
{ 
    public TileType Type;
    public int Variant;
}
