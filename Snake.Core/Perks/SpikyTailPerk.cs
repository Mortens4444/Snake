namespace SnakeGameEngine.Perks;

// Triggered automatically by the collision handling in GameState.
public class SpikyTailPerk : Perk
{
    public override string Name => "Spiky Tail";

    public override string Description => "When you would die on an enemy snake, the enemy dies instead (with cooldown).";

    public override int CooldownTicks => 80;
}
