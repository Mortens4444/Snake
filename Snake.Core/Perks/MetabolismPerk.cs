namespace SnakeGameEngine.Perks;

public class MetabolismPerk : Perk
{
    private bool skipNextGrowth;

    public override string Name => "Metabolism";

    public override string Description => "Only every second food makes you longer, but each one still counts fully towards levels.";

    public override int ModifyGrowth(int growth, GameState gameState)
    {
        var skip = skipNextGrowth;
        skipNextGrowth = !skipNextGrowth;
        return skip ? 0 : growth;
    }
}
