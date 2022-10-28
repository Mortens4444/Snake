using SnakeGameEngine.Moving;

namespace SnakeGameEngine.Actors;

public class SnakeBodyPartInfo : ElementInfo
{
    public override char DisplayChar => 'X';

    public SnakeBodyPartInfo(Location location, ConsoleColor color)
        : base(location, color)
    {
    }
}
