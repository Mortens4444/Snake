using SnakeGameEngine.Actors;
using SnakeGameEngine.Moving;

namespace SnakeGameEngine.AI;

// Path-finds to the nearest reachable food and never steps onto a blocked cell.
public class NormalBrain : ISnakeBrain
{
    public Direction? ChooseDirection(ObstacleSnake self, GameState gameState, HashSet<(int X, int Y)> blockedCells)
    {
        var head = self.Cells[0].Location;
        var headKey = (head.X, head.Y);

        foreach (var food in gameState.Foods.OrderBy(food => BrainHelper.GetManhattanDistance(head, food.Location)).Take(3))
        {
            var step = Pathfinding.GetFirstStepTowards(headKey, (food.Location.X, food.Location.Y), blockedCells);
            if (step != null)
            {
                return step;
            }
        }

        return BrainHelper.GetFirstFreeDirection(self, blockedCells);
    }
}
