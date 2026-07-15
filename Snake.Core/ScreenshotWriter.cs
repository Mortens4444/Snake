using SnakeGameEngine.Moving;
using System.Text.Json;

namespace SnakeGameEngine;

// F12 (or PrintScreen where the console passes it through) dumps the current game state
// to a JSON file, including a text rendering of the whole field in the "Screenshot" field.
public static class ScreenshotWriter
{
    public static string Save(GameState gameState)
    {
        var fileName = $"screenshot-{DateTime.Now:yyyyMMdd-HHmmss}.json";
        var payload = new
        {
            TakenAt = DateTime.Now,
            gameState.Level,
            gameState.LevelPoints,
            gameState.TickNumber,
            Elapsed = gameState.Elapsed.ToString(@"mm\:ss"),
            PlayerLength = gameState.PlayerSnake.SnakeBodyParts.Count,
            PlayerPerks = gameState.PlayerPerks.Select(perk => perk.Name).ToList(),
            gameState.ShieldCharges,
            Enemies = gameState.EnemySnakes.Select(enemySnake => new
            {
                enemySnake.Personality.Name,
                Length = enemySnake.Cells.Count,
                Perks = enemySnake.EnemyPerks
            }).ToList(),
            Foods = gameState.Foods.Select(food => new
            {
                Type = food.Type.ToString(),
                food.Location.X,
                food.Location.Y
            }).ToList(),
            Screenshot = BuildScreenshotRows(gameState)
        };

        File.WriteAllText(fileName, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        return fileName;
    }

    private static string[] BuildScreenshotRows(GameState gameState)
    {
        var width = Settings.Current.MapWidth;
        var height = Settings.Current.MapHeight;
        var grid = new char[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                grid[y, x] = gameState.Background.GetDisplayCharAt(x, y);
            }
        }

        void Put(Location location, char displayChar)
        {
            if (location.X >= 0 && location.X < width && location.Y >= 0 && location.Y < height)
            {
                grid[location.Y, location.X] = displayChar;
            }
        }

        foreach (var cloud in gameState.PoisonClouds)
        {
            Put(cloud.Location, '▒');
        }
        foreach (var enemySnake in gameState.EnemySnakes)
        {
            foreach (var cell in enemySnake.Cells)
            {
                Put(cell.Location, cell.DisplayChar);
            }
        }
        foreach (var bodyPart in gameState.PlayerSnake.SnakeBodyParts)
        {
            Put(bodyPart.Location, bodyPart.DisplayChar);
        }
        if (gameState.GuestSnake != null && gameState.GuestAlive)
        {
            foreach (var bodyPart in gameState.GuestSnake.SnakeBodyParts)
            {
                Put(bodyPart.Location, bodyPart.DisplayChar);
            }
        }
        foreach (var food in gameState.Foods)
        {
            Put(food.Location, food.DisplayChar);
        }
        if (gameState.BirdLocation != null)
        {
            Put(gameState.BirdLocation, 'V');
        }

        var rows = new string[height];
        for (int y = 0; y < height; y++)
        {
            var characters = new char[width];
            for (int x = 0; x < width; x++)
            {
                characters[x] = grid[y, x];
            }
            rows[y] = new string(characters);
        }
        return rows;
    }
}
