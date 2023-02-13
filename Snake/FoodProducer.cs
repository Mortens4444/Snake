using SnakeGameEngine.Actors;
using SnakeGameEngine.Moving;

namespace SnakeGameEngine;

public class FoodProducer
{
    public static FoodInfo GetFood()
    {
        return new FoodInfo(new Location(Random.Shared.Next(Constants.MaxX), Random.Shared.Next(Constants.MaxY)));
    }
}
