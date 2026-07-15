using SnakeGameEngine.Actors;
using SnakeGameEngine.Moving;

namespace SnakeGameEngine.AI;

// Beelines for the nearest food but does not check what is in the way.
public class EasyBrain : ISnakeBrain
{
    public Direction? ChooseDirection(ObstacleSnake self, GameState gameState, HashSet<(int X, int Y)> blockedCells)
    {
        var head = self.Cells[0].Location;
        var food = gameState.Foods.MinBy(food => BrainHelper.GetManhattanDistance(head, food.Location));
        if (food == null)
        {
            return self.Direction;
        }

        var deltaX = food.Location.X - head.X;
        var deltaY = food.Location.Y - head.Y;
        var horizontal = deltaX > 0 ? Direction.Right : Direction.Left;
        var vertical = deltaY > 0 ? Direction.Down : Direction.Up;

        var preferences = new List<Direction>();
        if (Math.Abs(deltaX) >= Math.Abs(deltaY))
        {
            if (deltaX != 0)
            {
                preferences.Add(horizontal);
            }
            if (deltaY != 0)
            {
                preferences.Add(vertical);
            }
        }
        else
        {
            if (deltaY != 0)
            {
                preferences.Add(vertical);
            }
            if (deltaX != 0)
            {
                preferences.Add(horizontal);
            }
        }
        preferences.Add(self.Direction);

        var opposite = BrainHelper.GetOpposite(self.Direction);
        return preferences.First(direction => direction != opposite);
    }
}
