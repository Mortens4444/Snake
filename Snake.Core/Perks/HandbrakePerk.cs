namespace SnakeGameEngine.Perks;

public class HandbrakePerk : Perk
{
    public override string Name => "Handbrake";

    public override string Description => "Slow yourself to half speed for a short time to thread tight corners.";

    public override ConsoleKey? ActivationKey => ConsoleKey.B;

    public override int CooldownTicks => 100;

    protected override void OnActivate(GameState gameState)
    {
        gameState.SlowdownTicksRemaining = 25;
    }

    public override int ModifyTickMilliseconds(int milliseconds, GameState gameState)
    {
        return gameState.SlowdownTicksRemaining > 0 ? milliseconds * 2 : milliseconds;
    }

    // GameState.GetTickMilliseconds() only ever reads the host's PlayerPerks; the guest moves on
    // the same shared tick as the host, so a guest-owned speed perk would have zero effect.
    public override bool IsEligibleForGuest => false;
}
