using SnakeGameEngine.Actors;

namespace SnakeGameEngine.AI;

public static class BrainFactory
{
    public static readonly string[] DifficultyNames = { "Random", "Easy", "Normal", "Hard", "Expert", "Nightmare" };

    public static ISnakeBrain Create(int difficulty, EnemyPersonality personality)
    {
        difficulty = Math.Clamp(difficulty, 0, DifficultyNames.Length - 1);

        // Viper hunts the player as soon as the pack is smart enough to path-find.
        if (personality.Name == "Viper" && difficulty is 2 or 3 or 4)
        {
            return new HardBrain();
        }

        return difficulty switch
        {
            0 => new RandomBrain(),
            1 => new EasyBrain(),
            2 => new NormalBrain(),
            3 => new HardBrain(),
            4 => new ExpertBrain(),
            _ => new NightmareBrain()
        };
    }
}
