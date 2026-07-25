using System.Text.Json;

namespace SnakeGameEngine;

// The player's perks survive between games (configurable via LosePerksOnDeath).
public class PlayerProgress
{
    private const string FilePath = "playerprogress.json";

    public List<string> PerkNames { get; set; } = new();

    // Carries the player's snake length into the next game, the same way perks do -
    // reset back to the minimum on death unless Settings.LoseLengthOnDeath is turned off.
    public int StartingLength { get; set; } = 2;

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
