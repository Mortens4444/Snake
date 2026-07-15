using SnakeGameEngine.Actors;
using SnakeGameEngine.Moving;

namespace SnakeGameEngine.AI;

// Wanders aimlessly and never looks where it is going - crashes are its own problem.
public class RandomBrain : ISnakeBrain
{
    public Direction? ChooseDirection(ObstacleSnake self, GameState gameState, HashSet<(int X, int Y)> blockedCells)
    {
        if (Random.Shared.Next(100) < Settings.Current.ObstacleTurnChancePercent)
        {
            var options = BrainHelper.GetNonReverseDirections(self.Direction);
            return options[Random.Shared.Next(options.Count)];
        }
        return self.Direction;
    }
}
