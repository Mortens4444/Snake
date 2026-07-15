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
}
