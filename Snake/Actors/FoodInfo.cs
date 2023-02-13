using SnakeGameEngine.Moving;

namespace SnakeGameEngine.Actors;

public class FoodInfo : ElementInfo
{
    public override ConsoleColor Color => ConsoleColor.Red;

    public override char DisplayChar => 'Ó';

    public FoodInfo(Location location)
        : base(location)
    {
    }
}
