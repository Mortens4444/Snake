using SnakeGameEngine.Moving;

namespace SnakeGameEngine.Actors;

public class ObstacleInfo : ElementInfo
{
    public ConsoleColor FallbackColor { get; set; } = ConsoleColor.DarkYellow;

    public override ConsoleColor Color => FallbackColor;

    public override char DisplayChar => '█';

    public ObstacleInfo(Location location)
        : base(location)
    {
    }
}
