namespace SnakeGameEngine.Actors;

public record EnemyPersonality(
    string Name,
    (int R, int G, int B) BodyColor,
    ConsoleColor FallbackColor,
    int GrowthPerFood,
    bool ShrinksInsteadOfDying,
    int SpeedDivisor,
    string FavoritePerk);

public static class EnemyRoster
{
    // The named snakes from the design table: own color, own traits, favorite perk (used from the perk phase on).
    private static readonly EnemyPersonality[] NamedPersonalities =
    {
        new("Viper", (57, 255, 20), ConsoleColor.Green, 1, false, 1, "Poison Trail"),
        new("Ghost", (200, 162, 255), ConsoleColor.Magenta, 1, false, 1, "Ghost Phase"),
        new("Titan", (40, 90, 220), ConsoleColor.Blue, 2, false, 2, "Iron Head"),
        new("Fang", (230, 40, 40), ConsoleColor.Red, 1, false, 1, "Spiky Tail"),
        new("Hydra", (30, 140, 60), ConsoleColor.DarkGreen, 1, true, 1, "Metabolism"),
    };

    public static EnemyPersonality GetPersonality(int index)
    {
        if (index < NamedPersonalities.Length)
        {
            return NamedPersonalities[index];
        }

        var color = RainbowColor.Get(Random.Shared.Next(360));
        return new EnemyPersonality($"Snake-{index + 1}", color, ConsoleColor.Gray, 1, false, 1, "None");
    }
}
