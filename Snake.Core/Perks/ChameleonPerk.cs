namespace SnakeGameEngine.Perks;

// The hunter brains check GameState.IsPlayerHiddenFromHunters.
public class ChameleonPerk : Perk
{
    public override string Name => "Chameleon";

    public override string Description => "While on grass or under trees, hunter snakes lose your scent.";
}
