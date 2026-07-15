namespace SnakeGameEngine.Perks;

public class IronHeadPerk : Perk
{
    public override string Name => "Iron Head";

    public override string Description => "Survive one fatal collision; the shield breaks and recharges on level-up.";

    public bool ShieldAvailable { get; set; } = true;

    public override void OnLevelUp(GameState gameState)
    {
        ShieldAvailable = true;
    }
}
