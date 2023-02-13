using SnakeGameEngine.Moving;

namespace SnakeGameEngine.Actors.SnakeParts;

public class SnakeBodyPartInfo : ElementInfo
{
    public override ConsoleColor Color => ConsoleColor.DarkGreen;

    public override char DisplayChar => '■';

    public SnakeBodyPartInfo(Location location)
        : base(location)
    {
    }
}

