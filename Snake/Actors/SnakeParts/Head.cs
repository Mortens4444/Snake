using SnakeGameEngine.Moving;

namespace SnakeGameEngine.Actors.SnakeParts;

public class Head : SnakeBodyPartInfo
{
    public override ConsoleColor Color => ConsoleColor.Blue;

    public override char DisplayChar => '@';

    public Head(Location location)
        : base(location)
    {
    }
}
