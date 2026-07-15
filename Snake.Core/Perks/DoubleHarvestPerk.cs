namespace SnakeGameEngine.Perks;

public class DoubleHarvestPerk : Perk
{
    public override string Name => "Double Harvest";

    public override string Description => "15% chance that a food counts twice towards leveling.";

    public override int ModifyPoints(int points, GameState gameState)
    {
        return Random.Shared.Next(100) < 15 ? points * 2 : points;
    }
}
