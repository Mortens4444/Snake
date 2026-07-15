using SnakeGameEngine.Moving;

namespace SnakeGameEngine.Actors;

public enum FoodType
{
    Red,      // +1 point
    Gold,     // +3 points
    Purple,   // instant perk choice
    Blue,     // +1 shield charge
    Rainbow   // random effect
}

public class FoodInfo : ElementInfo
{
    public FoodType Type { get; }

    public override ConsoleColor Color => Type switch
    {
        FoodType.Gold => ConsoleColor.Yellow,
        FoodType.Purple => ConsoleColor.Magenta,
        FoodType.Blue => ConsoleColor.Blue,
        FoodType.Rainbow => ConsoleColor.White,
        _ => ConsoleColor.Red
    };

    public override char DisplayChar => '★';

    public FoodInfo(Location location, FoodType type = FoodType.Red)
        : base(location)
    {
        Type = type;
    }
}
