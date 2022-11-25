using SnakeGameEngine.Moving;

namespace SnakeGameEngine.Actors;

public class FoodInfo : ElementInfo
{
    public override string DisplayChar => "🍎";

    public FoodInfo(Location location)
        : base(location, ConsoleColor.Red)
    {
    }
}
