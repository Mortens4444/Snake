using SnakeGameEngine.Actors;
using SnakeGameEngine.Moving;

namespace SnakeGameEngine;

public class FoodProducer
{
    public static FoodInfo GetFood(Snake snake, List<ObstacleInfo> obstacles)
    {
        Location location;
        do
        {
            location = new Location(Random.Shared.Next(Constants.MaxX), Random.Shared.Next(Constants.MaxY));
        } while (snake.SnakeBodyParts.Any(bodyPart => bodyPart.Location.Equals(location))
            || obstacles.Any(obstacle => obstacle.Location.Equals(location)));

        return new FoodInfo(location);
    }
}
