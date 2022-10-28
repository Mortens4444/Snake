using SnakeGameEngine.Moving;

namespace SnakeGameEngine.Actors;

public class FoodInfo : ElementInfo
{
    public override char DisplayChar => 'O';

    public FoodInfo(Location location)
        : base(location, ConsoleColor.Red)
    {
    }
}
