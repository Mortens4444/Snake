using SnakeGameEngine.Actors.SnakeParts;
using SnakeGameEngine.Moving;

namespace SnakeGameEngine.Actors;

public class Snake
{
    public List<SnakeBodyPartInfo> SnakeBodyParts { get; set; }

    public Direction Direction { get; set; } = Direction.Up;

    // Used to spawn a second, network-controlled snake away from the host's starting cell.
    public Snake(Location spawnHead)
        : this(spawnHead, 2)
    {
    }

    // Used for the player's snake, whose starting length can persist between games
    // (see PlayerProgress.StartingLength) instead of always starting at the minimum.
    public Snake(Location spawnHead, int length)
    {
        SnakeBodyParts = new List<SnakeBodyPartInfo> { new Head(spawnHead) };
        for (int i = 1; i < Math.Max(length, 1); i++)
        {
            SnakeBodyParts.Add(new SnakeBodyPartInfo(new Location(spawnHead.X, spawnHead.Y + i)));
        }
    }

    public Location Head => SnakeBodyParts.First().Location;

    public Location Tail => SnakeBodyParts.Last().Location;

    public bool IsOutOfBounds()
    {
        return Head.X < 0 || Head.X >= Settings.Current.MapWidth || Head.Y < 0 || Head.Y >= Settings.Current.MapHeight;
    }

    public bool HasCollidedWithItself()
    {
        return SnakeBodyParts.Skip(1).Any(bodyPart => bodyPart.Location.Equals(Head));
    }

    public void Move()
    {
        if (SnakeBodyParts[0] is SnakeParts.Head headPart)
        {
            headPart.Direction = Direction;
        }

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

    // Reverts the last Move: used when a perk lets the player survive a fatal collision.
    public void UndoMove(Location previousTailLocation)
    {
        for (int i = 0; i < SnakeBodyParts.Count - 1; i++)
        {
            SnakeBodyParts[i].Location = SnakeBodyParts[i + 1].Location;
        }
        SnakeBodyParts[^1].Location = previousTailLocation;
    }

    public bool HasHitObstacle(List<ObstacleInfo> obstacles)
    {
        return obstacles.Any(obstacle => obstacle.Location.Equals(Head));
    }

    public void Grow()
    {
        var newTail = new SnakeBodyPartInfo(Tail);
        SnakeBodyParts.Insert(SnakeBodyParts.Count - 1, newTail);
    }

    public void SetDirection(GameAction action)
    {
        Direction = action switch
        {
            GameAction.MoveUp => Direction == Direction.Down ? Direction.Down : Direction.Up,
            GameAction.MoveDown => Direction == Direction.Up ? Direction.Up : Direction.Down,
            GameAction.MoveLeft => Direction == Direction.Right ? Direction.Right : Direction.Left,
            GameAction.MoveRight => Direction == Direction.Left ? Direction.Left : Direction.Right,
            _ => Direction
        };
    }
}
