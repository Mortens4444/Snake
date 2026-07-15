namespace SnakeGameEngine.Perks;

public class GhostPhasePerk : Perk
{
    public override string Name => "Ghost Phase";

    public override string Description => "Become a phantom: pass through yourself, enemy snakes, trees and even the wall (wrapping to the other side) for a short time.";

    public override ConsoleKey? ActivationKey => ConsoleKey.G;

    public override int CooldownTicks => 150;

    protected override void OnActivate(GameState gameState)
    {
        gameState.GhostTicksRemaining = 15;
    }
}
