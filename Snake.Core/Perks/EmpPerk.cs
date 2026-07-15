namespace SnakeGameEngine.Perks;

public class EmpPerk : Perk
{
    public override string Name => "EMP";

    public override string Description => "Enemy snakes lose their AI for a while and wander blindly.";

    public override ConsoleKey? ActivationKey => ConsoleKey.E;

    public override int CooldownTicks => 180;

    protected override void OnActivate(GameState gameState)
    {
        gameState.EmpTicksRemaining = 25;
    }
}
