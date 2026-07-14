using SnakeGameEngine.Actors;
using SnakeGameEngine.Moving;

namespace SnakeGameEngine;

public static class ObstacleProducer
{
    public static List<ObstacleSnake> GetObstacles()
    {
        var obstacleSnakes = new List<ObstacleSnake>();
        var occupiedCells = new List<ObstacleInfo>();
        for (int i = 0; i < Constants.ObstacleCount; i++)
        {
            var obstacleSnake = GetObstacleSnake(occupiedCells);
            if (obstacleSnake != null)
            {
                occupiedCells.AddRange(obstacleSnake.Cells);
                obstacleSnakes.Add(obstacleSnake);
            }
        }
        return obstacleSnakes;
    }

    private static ObstacleSnake? GetObstacleSnake(List<ObstacleInfo> existingObstacles)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            var length = Random.Shared.Next(Constants.ObstacleMinLength, Constants.ObstacleMaxLength + 1);
            var isHorizontal = Random.Shared.Next(2) == 0;
            var x = Random.Shared.Next(Constants.MaxX - (isHorizontal ? length : 0));
            var y = Random.Shared.Next(Constants.MaxY - (isHorizontal ? 0 : length));

            var wall = new List<ObstacleInfo>();
            for (int i = 0; i < length; i++)
            {
                wall.Add(new ObstacleInfo(new Location(isHorizontal ? x + i : x, isHorizontal ? y : y + i)));
            }

            if (wall.All(cell => IsAllowed(cell.Location, existingObstacles)))
            {
                return new ObstacleSnake(wall, isHorizontal ? Direction.Left : Direction.Up);
            }
        }
        return null;
    }

    private static bool IsAllowed(Location location, List<ObstacleInfo> existingObstacles)
    {
        var isNearSnakeStart = Math.Abs(location.X - Constants.HalfOfMaxX) <= Constants.ObstacleFreeZoneRadius
            && Math.Abs(location.Y - Constants.HalfOfMaxY) <= Constants.ObstacleFreeZoneRadius;

        return !isNearSnakeStart && !existingObstacles.Any(obstacle => obstacle.Location.Equals(location));
    }
}
