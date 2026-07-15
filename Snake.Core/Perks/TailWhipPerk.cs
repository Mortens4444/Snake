namespace SnakeGameEngine.Perks;

public class TailWhipPerk : Perk
{
    public override string Name => "Tail Whip";

    public override string Description => "A sweeping whip chops the front ends off nearby enemy snakes.";

    public override ConsoleKey? ActivationKey => ConsoleKey.W;

    public override int CooldownTicks => 200;

    protected override void OnActivate(GameState gameState)
    {
        gameState.TailWhip();
    }
}
