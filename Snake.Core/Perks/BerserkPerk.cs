namespace SnakeGameEngine.Perks;

public class BerserkPerk : Perk
{
    public override string Name => "Berserk";

    public override string Description => "When no enemy snake is left alive, you move 40% faster.";

    public override int ModifyTickMilliseconds(int milliseconds, GameState gameState)
    {
        return gameState.EnemySnakes.Count == 0 ? (int)(milliseconds * 0.6) : milliseconds;
    }

    // GameState.GetTickMilliseconds() only ever reads the host's PlayerPerks; the guest moves on
    // the same shared tick as the host, so a guest-owned speed perk would have zero effect.
    public override bool IsEligibleForGuest => false;
}
