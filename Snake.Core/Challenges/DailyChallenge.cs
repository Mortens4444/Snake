namespace SnakeGameEngine.Challenges;

public enum ChallengeKind
{
    SurviveMinutes,
    KillEnemiesByWall,
    EatFoods,
    NoActivePerkUsed
}

public sealed record ChallengeTask(ChallengeKind Kind, int Target, string Description);

// Deterministic per-day challenge set: same seed -> same three tasks for everyone on that date.
public static class DailyChallenge
{
    public static string TodayKey => DateTime.Now.ToString("yyyy-MM-dd");

    public static List<ChallengeTask> GetTasksFor(string dayKey)
    {
        var random = new Random(dayKey.GetHashCode());
        var allTasks = new List<ChallengeTask>
        {
            new(ChallengeKind.SurviveMinutes, 5, "Survive 5 minutes"),
            new(ChallengeKind.KillEnemiesByWall, 3, "Wall-crash 3 enemy snakes"),
            new(ChallengeKind.EatFoods, 100, "Eat 100 foods"),
            new(ChallengeKind.NoActivePerkUsed, 1, "Win without using an active perk")
        };

        return allTasks.OrderBy(_ => random.Next()).Take(3).ToList();
    }

    // Checks progress against a finished game's GameState; call once when a game ends.
    public static bool[] EvaluateCompletion(List<ChallengeTask> tasks, GameState gameState)
    {
        return tasks.Select(task => task.Kind switch
        {
            ChallengeKind.SurviveMinutes => gameState.Elapsed.TotalMinutes >= task.Target,
            ChallengeKind.KillEnemiesByWall => gameState.DeadEnemyNames.Count >= task.Target,
            ChallengeKind.EatFoods => gameState.FoodsEaten >= task.Target,
            ChallengeKind.NoActivePerkUsed => gameState.Status == GameStatus.Won && !gameState.HasUsedActivePerk,
            _ => false
        }).ToArray();
    }
}
