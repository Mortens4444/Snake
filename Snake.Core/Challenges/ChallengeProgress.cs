using System.Text.Json;

namespace SnakeGameEngine.Challenges;

public class ChallengeProgress
{
    public string DayKey { get; set; } = "";

    public bool[] Completed { get; set; } = Array.Empty<bool>();
}

// One day's challenge completion state, persisted across games and app restarts.
public static class ChallengeProgressStore
{
    private const string FilePath = "dailychallenge.json";

    public static ChallengeProgress LoadForToday()
    {
        var today = DailyChallenge.TodayKey;
        if (File.Exists(FilePath))
        {
            try
            {
                var stored = JsonSerializer.Deserialize<ChallengeProgress>(File.ReadAllText(FilePath));
                if (stored != null && stored.DayKey == today)
                {
                    return stored;
                }
            }
            catch (JsonException)
            {
            }
        }

        // A new day (or first run): reset to a fresh, all-incomplete state.
        var fresh = new ChallengeProgress { DayKey = today, Completed = new bool[3] };
        Save(fresh);
        return fresh;
    }

    public static void Save(ChallengeProgress progress)
    {
        File.WriteAllText(FilePath, JsonSerializer.Serialize(progress, new JsonSerializerOptions { WriteIndented = true }));
    }

    // Merges a finished game's results in - a task stays completed once achieved, even across games.
    public static ChallengeProgress RecordGameResult(GameState gameState)
    {
        var progress = LoadForToday();
        var tasks = DailyChallenge.GetTasksFor(progress.DayKey);
        var justCompleted = DailyChallenge.EvaluateCompletion(tasks, gameState);

        for (int i = 0; i < progress.Completed.Length && i < justCompleted.Length; i++)
        {
            progress.Completed[i] |= justCompleted[i];
        }

        Save(progress);
        return progress;
    }
}
