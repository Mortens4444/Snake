using System.Text.Json;

namespace SnakeGameEngine;

public class EnemyProfile
{
    public string Name { get; set; } = "";

    public int Deaths { get; set; }

    public int Survivals { get; set; }

    public int PlayerKills { get; set; }

    public List<string> PerkNames { get; set; } = new();
}

// Persists the enemy snakes' career statistics across games.
public static class EnemyProfileStore
{
    private const string FilePath = "profiles.json";

    public static List<EnemyProfile> Load()
    {
        if (!File.Exists(FilePath))
        {
            return new List<EnemyProfile>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<EnemyProfile>>(File.ReadAllText(FilePath)) ?? new List<EnemyProfile>();
        }
        catch (JsonException)
        {
            return new List<EnemyProfile>();
        }
    }

    public static void RecordGame(GameState gameState)
    {
        var profiles = Load();

        // Dying resets an enemy's earned perks; the survivors keep and grow theirs.
        foreach (var name in gameState.DeadEnemyNames)
        {
            var profile = GetOrAdd(profiles, name);
            profile.Deaths++;
            profile.PerkNames.Clear();
        }
        foreach (var enemySnake in gameState.EnemySnakes)
        {
            var profile = GetOrAdd(profiles, enemySnake.Personality.Name);
            profile.Survivals++;
            profile.PerkNames = enemySnake.EnemyPerks.ToList();
        }
        if (gameState.PlayerKilledBy != null)
        {
            GetOrAdd(profiles, gameState.PlayerKilledBy).PlayerKills++;
        }

        File.WriteAllText(FilePath, JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static void Reset()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
        }
    }

    private static EnemyProfile GetOrAdd(List<EnemyProfile> profiles, string name)
    {
        var profile = profiles.FirstOrDefault(profile => profile.Name == name);
        if (profile == null)
        {
            profile = new EnemyProfile { Name = name };
            profiles.Add(profile);
        }
        return profile;
    }
}
