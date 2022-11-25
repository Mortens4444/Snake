using SnakeGameEngine.Moving;

namespace SnakeGameEngine.Actors;

public class SnakeBodyPartInfo : ElementInfo
{
    public override string DisplayChar => "▀";

    public SnakeBodyPartInfo(Location location, ConsoleColor color)
        : base(location, color)
    {
    }
}
