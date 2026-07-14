using SnakeGameEngine.Moving;

namespace SnakeGameEngine.Actors;

public class ObstacleSnake
{
    public List<ObstacleInfo> Cells { get; }

    public Direction Direction { get; set; }

    public ObstacleSnake(List<ObstacleInfo> cells, Direction direction)
    {
        Cells = cells;
        Direction = direction;
    }

    // Slithers one cell forward, never onto the player, the food or another obstacle.
    // Returns the vacated tail location so it can be cleared from the screen,
    // or null if the obstacle is boxed in and stays put.
    public Location? Move(List<ObstacleInfo> allObstacleCells, Snake snake, FoodInfo foodInfo)
    {
        foreach (var direction in GetDirectionsToTry())
        {
            var newHeadLocation = GetNeighborLocation(Cells[0].Location, direction);
            if (IsFree(newHeadLocation, allObstacleCells, snake, foodInfo))
            {
                Direction = direction;
                var vacatedTail = Cells[^1].Location;
                for (int i = Cells.Count - 1; i > 0; i--)
                {
                    Cells[i].Location = Cells[i - 1].Location;
                }
                Cells[0].Location = newHeadLocation;
                return vacatedTail;
            }
        }
        return null;
    }

    private List<Direction> GetDirectionsToTry()
    {
        var otherDirections = new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right }
            .Where(direction => direction != Direction)
            .OrderBy(_ => Random.Shared.Next())
            .ToList();

        var directionsToTry = new List<Direction>();
        if (Random.Shared.Next(100) < Constants.ObstacleTurnChancePercent)
        {
            directionsToTry.AddRange(otherDirections);
            directionsToTry.Add(Direction);
        }
        else
        {
            directionsToTry.Add(Direction);
            directionsToTry.AddRange(otherDirections);
        }
        return directionsToTry;
    }

    private static Location GetNeighborLocation(Location location, Direction direction)
    {
        return direction switch
        {
            Direction.Up => new Location(location.X, location.Y - 1),
            Direction.Down => new Location(location.X, location.Y + 1),
            Direction.Left => new Location(location.X - 1, location.Y),
            _ => new Location(location.X + 1, location.Y)
        };
    }

    private static bool IsFree(Location location, List<ObstacleInfo> allObstacleCells, Snake snake, FoodInfo foodInfo)
    {
        var isInBounds = location.X >= 0 && location.X < Constants.MaxX
            && location.Y >= 0 && location.Y < Constants.MaxY;

        return isInBounds
            && !allObstacleCells.Any(cell => cell.Location.Equals(location))
            && !snake.SnakeBodyParts.Any(bodyPart => bodyPart.Location.Equals(location))
            && !foodInfo.Location.Equals(location);
    }
}
