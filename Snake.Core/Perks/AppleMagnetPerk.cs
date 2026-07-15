using SnakeGameEngine.AI;
using SnakeGameEngine.Moving;

namespace SnakeGameEngine.Perks;

public class AppleMagnetPerk : Perk
{
    public override string Name => "Apple Magnet";

    public override string Description => "Nearby food crawls towards your head on its own.";

    public override void OnTick(GameState gameState)
    {
        var head = gameState.PlayerSnake.Head;
        foreach (var food in gameState.Foods)
        {
            var distance = BrainHelper.GetManhattanDistance(head, food.Location);
            if (distance < 2 || distance > 6)
            {
                continue;
            }

            var stepX = Math.Sign(head.X - food.Location.X);
            var stepY = Math.Sign(head.Y - food.Location.Y);
            var target = Math.Abs(head.X - food.Location.X) >= Math.Abs(head.Y - food.Location.Y)
                ? new Location(food.Location.X + stepX, food.Location.Y)
                : new Location(food.Location.X, food.Location.Y + stepY);

            if (gameState.IsCellFreeForFood(target))
            {
                gameState.VacatedLocations.Add(new Location(food.Location.X, food.Location.Y));
                food.Location = target;
            }
        }
    }
}
