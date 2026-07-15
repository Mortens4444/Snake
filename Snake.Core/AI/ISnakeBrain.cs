using SnakeGameEngine.Actors;
using SnakeGameEngine.Moving;

namespace SnakeGameEngine.AI;

// Decides where an enemy snake moves next. Returning null means the snake sees no move at all
// (boxed in); returning a direction into a blocked cell is a fatal blunder - dumb brains do that.
public interface ISnakeBrain
{
    Direction? ChooseDirection(ObstacleSnake self, GameState gameState, HashSet<(int X, int Y)> blockedCells);
}
