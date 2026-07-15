using System.Text.Json;

namespace SnakeGameEngine;

public class Settings
{
    private const string FilePath = "settings.json";

    public static Settings Current { get; private set; } = new();

    // The playfield size in cells; the border wall and the status line need 2 more columns and 3 more rows.
    public int MapWidth { get; set; } = 118;

    public int MapHeight { get; set; } = 26;

    public int TargetSnakeLength { get; set; } = 50;

    public int InitialTickMilliseconds { get; set; } = 100;

    public int MinimumTickMilliseconds { get; set; } = 50;

    public int SpeedUpPerBodyPart { get; set; } = 1;

    public int ObstacleCount { get; set; } = 8;

    public int ObstacleMinLength { get; set; } = 3;

    public int ObstacleMaxLength { get; set; } = 8;

    public int ObstacleMoveEveryNthTick { get; set; } = 2;

    public int ObstacleTurnChancePercent { get; set; } = 20;

    // 0 Random, 1 Easy, 2 Normal, 3 Hard, 4 Expert, 5 Nightmare
    public int EnemyDifficulty { get; set; } = 2;

    public int PointsPerLevel { get; set; } = 10;

    public int PerkChoicesPerLevel { get; set; } = 3;

    public bool LosePerksOnDeath { get; set; }

    // How often the catchable bird flies across; 0 = never.
    public int BirdIntervalMinutes { get; set; } = 3;

    public bool SoundEnabled { get; set; } = true;

    public bool CheatsEnabled { get; set; } = true;

    // Shown in the final ranking, the leaderboard, and to LAN guests when you win or kill them.
    public string PlayerName { get; set; } = "Player";

    public static void Load()
    {
        if (!File.Exists(FilePath))
        {
            return;
        }

        try
        {
            Current = JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new Settings();
        }
        catch (JsonException)
        {
            Current = new Settings();
        }
    }

    public void Save()
    {
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }

    // Distinct from PlayerProgress/EnemyProfileStore's Reset(): those delete a file that gets
    // re-read from scratch next time. Current is a live singleton read all over the app, so
    // resetting it also has to replace the in-memory instance, not just the file on disk.
    public static void ResetToDefaults()
    {
        Current = new Settings();
        Current.Save();
    }
}
