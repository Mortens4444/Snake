using SnakeGameEngine.Actors.SnakeParts;
using SnakeGameEngine.Moving;

namespace SnakeGameEngine.Actors;

public class Snake
{
    public List<SnakeBodyPartInfo> SnakeBodyParts { get; set; } = new()
    {
        new Head(new Location(Constants.HalfOfMaxX, Constants.HalfOfMaxY)),
        new SnakeBodyPartInfo(new Location(Constants.HalfOfMaxX, Constants.HalfOfMaxY + 1))
    };

    public Direction Direction { get; set; } = Direction.Up;

    public Location Head => SnakeBodyParts.First().Location;

    public Location Tail => SnakeBodyParts.Last().Location;

    public bool IsOutOfBounds()
    {
        return Head.X < 0 || Head.X >= Constants.MaxX || Head.Y < 0 || Head.Y >= Constants.MaxY;
    }

    public bool HasCollidedWithItself()
    {
        return SnakeBodyParts.Skip(1).Any(bodyPart => bodyPart.Location.Equals(Head));
    }

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

        if (IsOutOfBounds() || HasCollidedWithItself())
        {
            throw new InvalidOperationException("Game over");
        }
    }

    public bool HasTouchedFood(FoodInfo foodInfo)
    {
        var result = Head.Equals(foodInfo.Location);
        if (result)
        {
            var newTail = new SnakeBodyPartInfo(Tail);
            SnakeBodyParts.Insert(SnakeBodyParts.Count - 1, newTail);
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
                ConsoleKey.UpArrow => Direction == Direction.Down ? Direction.Down : Direction.Up,
                ConsoleKey.DownArrow => Direction == Direction.Up ? Direction.Up : Direction.Down,
                ConsoleKey.LeftArrow => Direction == Direction.Right ? Direction.Right : Direction.Left,
                ConsoleKey.RightArrow => Direction == Direction.Left ? Direction.Left : Direction.Right,
                _ => Direction
            };

            return consoleKeyInfo;
        }
        return new();
    }
}