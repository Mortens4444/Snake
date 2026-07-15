using SnakeGameEngine.Actors;
using SnakeGameEngine.Moving;

namespace SnakeGameEngine.AI;

public static class BrainHelper
{
    public static bool IsInBounds((int X, int Y) cell)
    {
        return cell.X >= 0 && cell.X < Settings.Current.MapWidth
            && cell.Y >= 0 && cell.Y < Settings.Current.MapHeight;
    }

    public static bool IsFree((int X, int Y) cell, HashSet<(int X, int Y)> blockedCells)
    {
        return IsInBounds(cell) && !blockedCells.Contains(cell);
    }

    public static (int X, int Y) GetNeighbor((int X, int Y) cell, Direction direction)
    {
        var (deltaX, deltaY) = GetDelta(direction);
        return (cell.X + deltaX, cell.Y + deltaY);
    }

    public static Location GetNeighborLocation(Location location, Direction direction)
    {
        var (deltaX, deltaY) = GetDelta(direction);
        return new Location(location.X + deltaX, location.Y + deltaY);
    }

    public static (int X, int Y) GetDelta(Direction direction)
    {
        return direction switch
        {
            Direction.Up => (0, -1),
            Direction.Down => (0, 1),
            Direction.Left => (-1, 0),
            _ => (1, 0)
        };
    }

    public static Direction GetOpposite(Direction direction)
    {
        return direction switch
        {
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            _ => Direction.Left
        };
    }

    // Every direction except the reverse (stepping backwards is always suicide).
    public static List<Direction> GetNonReverseDirections(Direction current)
    {
        var opposite = GetOpposite(current);
        return new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right }
            .Where(direction => direction != opposite)
            .ToList();
    }

    // Prefers to keep going straight, otherwise tries the side directions in random order.
    public static Direction? GetFirstFreeDirection(ObstacleSnake self, HashSet<(int X, int Y)> blockedCells)
    {
        var headKey = (self.Cells[0].Location.X, self.Cells[0].Location.Y);
        var candidates = new List<Direction> { self.Direction };
        candidates.AddRange(GetNonReverseDirections(self.Direction)
            .Where(direction => direction != self.Direction)
            .OrderBy(_ => Random.Shared.Next()));

        foreach (var direction in candidates)
        {
            if (IsFree(GetNeighbor(headKey, direction), blockedCells))
            {
                return direction;
            }
        }
        return null;
    }

    public static int GetManhattanDistance(Location a, Location b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }
}
