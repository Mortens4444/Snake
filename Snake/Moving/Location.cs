namespace SnakeGameEngine.Moving;

public class Location : IEquatable<Location>
{
    public Location(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; set; }

    public int Y { get; set; }

    public bool Equals(Location? other)
    {
        if (other == null)
        {
            return false;
        }

        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Location);
    }

    public override int GetHashCode()
    {
        return X.GetHashCode() ^ Y.GetHashCode();
    }
}
