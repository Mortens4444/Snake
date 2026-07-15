using SnakeGameEngine.Actors;
using SnakeGameEngine.Moving;

namespace SnakeGameEngine.AI;

// Pack hunting: each pack member closes in on a different flank of the player's
// predicted position, so together they encircle the player.
public class NightmareBrain : ISnakeBrain
{
    private static readonly (int X, int Y)[] FlankOffsets = { (0, -3), (3, 0), (0, 3), (-3, 0) };

    private readonly ExpertBrain fallback = new();

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
        var flank = FlankOffsets[Math.Max(0, gameState.EnemySnakes.IndexOf(self)) % FlankOffsets.Length];
        var target = (player.Head.X + deltaX * 3 + flank.X, player.Head.Y + deltaY * 3 + flank.Y);

        if (BrainHelper.IsFree(target, blockedCells))
        {
            var step = Pathfinding.GetFirstStepTowards(headKey, target, blockedCells);
            if (step != null && IsSafeStep(self, headKey, step.Value, blockedCells))
            {
                return step;
            }
        }

        return fallback.ChooseDirection(self, gameState, blockedCells);
    }

    private static bool IsSafeStep(ObstacleSnake self, (int X, int Y) headKey, Direction direction, HashSet<(int X, int Y)> blockedCells)
    {
        var requiredSpace = self.Cells.Count + 2;
        var area = Pathfinding.MeasureFreeArea(BrainHelper.GetNeighbor(headKey, direction), blockedCells, requiredSpace * 2);
        return area >= requiredSpace;
    }
}
