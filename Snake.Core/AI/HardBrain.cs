using SnakeGameEngine.Actors;
using SnakeGameEngine.Moving;

namespace SnakeGameEngine.AI;

// Aims at where the player is heading to cut off the route; falls back to hunting food.
public class HardBrain : ISnakeBrain
{
    private readonly NormalBrain fallback = new();

    public Direction? ChooseDirection(ObstacleSnake self, GameState gameState, HashSet<(int X, int Y)> blockedCells)
    {
        // A camouflaged player cannot be hunted.
        if (gameState.IsPlayerHiddenFromHunters)
        {
            return fallback.ChooseDirection(self, gameState, blockedCells);
        }

        var head = self.Cells[0].Location;
        var headKey = (head.X, head.Y);
        var player = gameState.PlayerSnake;
        var (deltaX, deltaY) = BrainHelper.GetDelta(player.Direction);
        var target = (player.Head.X + deltaX * 4, player.Head.Y + deltaY * 4);

        if (BrainHelper.IsFree(target, blockedCells))
        {
            var step = Pathfinding.GetFirstStepTowards(headKey, target, blockedCells);
            if (step != null)
            {
                return step;
            }
        }

        return fallback.ChooseDirection(self, gameState, blockedCells);
    }
}
