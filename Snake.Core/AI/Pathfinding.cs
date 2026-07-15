using SnakeGameEngine.Moving;

namespace SnakeGameEngine.AI;

public static class Pathfinding
{
    private static readonly Direction[] AllDirections = { Direction.Up, Direction.Down, Direction.Left, Direction.Right };

    // Breadth-first search over free cells; returns the first step of a shortest path,
    // or null if the target is unreachable.
    public static Direction? GetFirstStepTowards((int X, int Y) start, (int X, int Y) target, HashSet<(int X, int Y)> blockedCells)
    {
        if (start == target)
        {
            return null;
        }

        var cameFrom = new Dictionary<(int X, int Y), (int X, int Y)> { [start] = start };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            foreach (var direction in AllDirections)
            {
                var neighbor = BrainHelper.GetNeighbor(cell, direction);
                if (cameFrom.ContainsKey(neighbor) || !BrainHelper.IsFree(neighbor, blockedCells))
                {
                    continue;
                }

                cameFrom[neighbor] = cell;
                if (neighbor == target)
                {
                    return GetFirstStep(start, target, cameFrom);
                }
                queue.Enqueue(neighbor);
            }
        }
        return null;
    }

    // Flood fill: how many free cells are reachable from the given cell (capped for speed).
    public static int MeasureFreeArea((int X, int Y) start, HashSet<(int X, int Y)> blockedCells, int cap)
    {
        if (!BrainHelper.IsFree(start, blockedCells))
        {
            return 0;
        }

        var visited = new HashSet<(int X, int Y)> { start };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(start);

        while (queue.Count > 0 && visited.Count < cap)
        {
            var cell = queue.Dequeue();
            foreach (var direction in AllDirections)
            {
                var neighbor = BrainHelper.GetNeighbor(cell, direction);
                if (!visited.Contains(neighbor) && BrainHelper.IsFree(neighbor, blockedCells))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
        return visited.Count;
    }

    private static Direction GetFirstStep((int X, int Y) start, (int X, int Y) target, Dictionary<(int X, int Y), (int X, int Y)> cameFrom)
    {
        var cell = target;
        while (cameFrom[cell] != start)
        {
            cell = cameFrom[cell];
        }

        if (cell.Y < start.Y)
        {
            return Direction.Up;
        }
        if (cell.Y > start.Y)
        {
            return Direction.Down;
        }
        return cell.X < start.X ? Direction.Left : Direction.Right;
    }
}
