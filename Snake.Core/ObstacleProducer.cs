using SnakeGameEngine.AI;
using SnakeGameEngine.Actors;
using SnakeGameEngine.Moving;

namespace SnakeGameEngine;

public static class ObstacleProducer
{
    public static List<ObstacleSnake> GetObstacles(Background background)
    {
        var obstacleSnakes = new List<ObstacleSnake>();
        var occupiedCells = new List<ObstacleInfo>();
        var profiles = EnemyProfileStore.Load();
        for (int i = 0; i < Settings.Current.ObstacleCount; i++)
        {
            var obstacleSnake = GetObstacleSnake(occupiedCells, EnemyRoster.GetPersonality(i), background);
            if (obstacleSnake != null)
            {
                var profile = profiles.FirstOrDefault(profile => profile.Name == obstacleSnake.Personality.Name);
                if (profile != null)
                {
                    obstacleSnake.EnemyPerks.AddRange(profile.PerkNames);
                }
                occupiedCells.AddRange(obstacleSnake.Cells);
                obstacleSnakes.Add(obstacleSnake);
            }
        }
        return obstacleSnakes;
    }

    private static ObstacleSnake? GetObstacleSnake(List<ObstacleInfo> existingObstacles, EnemyPersonality personality, Background background)
    {
        var mapWidth = Settings.Current.MapWidth;
        var mapHeight = Settings.Current.MapHeight;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            var length = Random.Shared.Next(Settings.Current.ObstacleMinLength, Settings.Current.ObstacleMaxLength + 1);
            length = Math.Min(length, Math.Min(mapWidth, mapHeight) - 2);
            var isHorizontal = Random.Shared.Next(2) == 0;
            var x = Random.Shared.Next(mapWidth - (isHorizontal ? length : 0));
            var y = Random.Shared.Next(mapHeight - (isHorizontal ? 0 : length));

            var wall = new List<ObstacleInfo>();
            for (int i = 0; i < length; i++)
            {
                wall.Add(new ObstacleInfo(new Location(isHorizontal ? x + i : x, isHorizontal ? y : y + i)));
            }

            if (wall.All(cell => IsAllowed(cell.Location, existingObstacles) && !background.IsCanopyAt(cell.Location)))
            {
                var brain = BrainFactory.Create(Settings.Current.EnemyDifficulty, personality);
                return new ObstacleSnake(wall, isHorizontal ? Direction.Left : Direction.Up, personality, brain);
            }
        }
        return null;
    }

    private static bool IsAllowed(Location location, List<ObstacleInfo> existingObstacles)
    {
        var isNearSnakeStart = Math.Abs(location.X - Settings.Current.MapWidth / 2) <= Constants.ObstacleFreeZoneRadius
            && Math.Abs(location.Y - Settings.Current.MapHeight / 2) <= Constants.ObstacleFreeZoneRadius;

        return !isNearSnakeStart && !existingObstacles.Any(obstacle => obstacle.Location.Equals(location));
    }
}
