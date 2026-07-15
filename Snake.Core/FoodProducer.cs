using SnakeGameEngine.Actors;
using SnakeGameEngine.Moving;

namespace SnakeGameEngine;

public class FoodProducer
{
    public static FoodInfo GetFood(Snake snake, List<ObstacleInfo> obstacles, List<FoodInfo> existingFoods, Background background)
    {
        Location location;
        do
        {
            location = new Location(Random.Shared.Next(Settings.Current.MapWidth), Random.Shared.Next(Settings.Current.MapHeight));
        } while (background.IsCanopyAt(location)
            || snake.SnakeBodyParts.Any(bodyPart => bodyPart.Location.Equals(location))
            || obstacles.Any(obstacle => obstacle.Location.Equals(location))
            || existingFoods.Any(food => food.Location.Equals(location)));

        return new FoodInfo(location, GetRandomType());
    }

    private static FoodType GetRandomType()
    {
        return Random.Shared.Next(100) switch
        {
            < 70 => FoodType.Red,
            < 85 => FoodType.Gold,
            < 90 => FoodType.Purple,
            < 95 => FoodType.Blue,
            _ => FoodType.Rainbow
        };
    }
}
