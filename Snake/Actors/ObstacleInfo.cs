using SnakeGameEngine.Moving;

namespace SnakeGameEngine.Actors;

public class ObstacleInfo : ElementInfo
{
    public override ConsoleColor Color => ConsoleColor.DarkYellow;

    public override char DisplayChar => '█';

    public ObstacleInfo(Location location)
        : base(location)
    {
    }
}
