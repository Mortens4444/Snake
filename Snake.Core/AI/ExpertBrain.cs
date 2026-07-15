using SnakeGameEngine.Actors;
using SnakeGameEngine.Moving;

namespace SnakeGameEngine.AI;

// Hunts food like NormalBrain, but flood-fills ahead and refuses to enter pockets
// that are too small to escape from.
public class ExpertBrain : ISnakeBrain
{
    public Direction? ChooseDirection(ObstacleSnake self, GameState gameState, HashSet<(int X, int Y)> blockedCells)
    {
        var head = self.Cells[0].Location;
        var headKey = (head.X, head.Y);

        var candidates = BrainHelper.GetNonReverseDirections(self.Direction)
            .Where(direction => BrainHelper.IsFree(BrainHelper.GetNeighbor(headKey, direction), blockedCells))
            .ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        var requiredSpace = self.Cells.Count + 2;
        var areaByDirection = candidates.ToDictionary(
            direction => direction,
            direction => Pathfinding.MeasureFreeArea(BrainHelper.GetNeighbor(headKey, direction), blockedCells, requiredSpace * 2));

        foreach (var food in gameState.Foods.OrderBy(food => BrainHelper.GetManhattanDistance(head, food.Location)).Take(3))
        {
            var step = Pathfinding.GetFirstStepTowards(headKey, (food.Location.X, food.Location.Y), blockedCells);
            if (step != null && areaByDirection.TryGetValue(step.Value, out var area) && area >= requiredSpace)
            {
                return step;
            }
        }

        return areaByDirection.OrderByDescending(pair => pair.Value).First().Key;
    }
}
