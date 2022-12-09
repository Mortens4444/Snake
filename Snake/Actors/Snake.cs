using SnakeGameEngine.Moving;

namespace SnakeGameEngine.Actors;

public class Snake
{
    public List<SnakeBodyPartInfo> SnakeBodyParts { get; set; } = new()
    {
        new SnakeBodyPartInfo(new Location(Constants.HalfOfMaxX, Constants.HalfOfMaxY), ConsoleColor.DarkGreen),
        new SnakeBodyPartInfo(new Location(Constants.HalfOfMaxX, Constants.HalfOfMaxY + 1), ConsoleColor.Blue)
    };

    public Direction Direction { get; set; } = Direction.Up;

    public Location Head => SnakeBodyParts.First().Location;

    public Location Tail => SnakeBodyParts.Last().Location;

    public void Move()
    {
        for (int i = SnakeBodyParts.Count - 1; i > 0; i--)
        {
            SnakeBodyParts[i].Location = new Location(SnakeBodyParts[i - 1].Location.X, SnakeBodyParts[i - 1].Location.Y);
        }
        switch (Direction)
        {
            case Direction.Up:
                Head.Y -= 1;
                break;
            case Direction.Down:
                Head.Y += 1;
                break;
            case Direction.Left:
                Head.X -= 1;
                break;
            case Direction.Right:
                Head.X += 1;
                break;
        }
    }

    public bool HasTouchedFood(FoodInfo foodInfo) 
    {
        var result = Head.Equals(foodInfo?.Location);
        if (result)
        {
            SnakeBodyParts.Add(new SnakeBodyPartInfo(Tail, ConsoleColor.Blue));
        }
        return result;
    }

    public ConsoleKeyInfo SetDirection()
    {
        if (Console.KeyAvailable)
        {
            var consoleKeyInfo = Console.ReadKey(true);

            Direction = consoleKeyInfo.Key switch
            {
                ConsoleKey.UpArrow => Direction.Up,
                ConsoleKey.DownArrow => Direction.Down,
                ConsoleKey.LeftArrow => Direction.Left,
                ConsoleKey.RightArrow => Direction.Right,
                _ => Direction
            };

            return consoleKeyInfo;
        }
        return new();
    }
}