namespace SnakeGameEngine.Perks;

public class TimeWarpPerk : Perk
{
    public override string Name => "Time Warp";

    public override string Description => "Freeze every enemy snake for a few seconds.";

    public override ConsoleKey? ActivationKey => ConsoleKey.T;

    public override int CooldownTicks => 180;

    protected override void OnActivate(GameState gameState)
    {
        gameState.TimeWarpTicksRemaining = 30;
    }
}
