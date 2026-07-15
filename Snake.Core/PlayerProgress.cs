using System.Text.Json;

namespace SnakeGameEngine;

// The player's perks survive between games (configurable via LosePerksOnDeath).
public class PlayerProgress
{
    private const string FilePath = "playerprogress.json";

    public List<string> PerkNames { get; set; } = new();

    public static PlayerProgress Load()
    {
        if (!File.Exists(FilePath))
        {
            return new PlayerProgress();
        }

        try
        {
            return JsonSerializer.Deserialize<PlayerProgress>(File.ReadAllText(FilePath)) ?? new PlayerProgress();
        }
        catch (JsonException)
        {
            return new PlayerProgress();
        }
    }

    public void Save()
    {
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static void Reset()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
        }
    }
}
