using SnakeGameEngine.Moving;

namespace SnakeGameEngine.Actors.SnakeParts;

public class Head : SnakeBodyPartInfo
{
    public Direction Direction { get; set; } = Direction.Up;

    public override ConsoleColor Color => ConsoleColor.Blue;

    public override char DisplayChar => Direction switch
    {
        Direction.Up => '▲',
        Direction.Down => '▼',
        Direction.Left => '◄',
        _ => '►'
    };

    public Head(Location location)
        : base(location)
    {
    }
}
