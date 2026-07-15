namespace SnakeGameEngine.Perks;

public class BerserkPerk : Perk
{
    public override string Name => "Berserk";

    public override string Description => "When no enemy snake is left alive, you move 40% faster.";

    public override int ModifyTickMilliseconds(int milliseconds, GameState gameState)
    {
        return gameState.EnemySnakes.Count == 0 ? (int)(milliseconds * 0.6) : milliseconds;
    }
}
