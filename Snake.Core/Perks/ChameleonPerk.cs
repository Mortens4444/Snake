namespace SnakeGameEngine.Perks;

// The hunter brains check GameState.IsPlayerHiddenFromHunters.
public class ChameleonPerk : Perk
{
    public override string Name => "Chameleon";

    public override string Description => "While on grass or under trees, hunter snakes lose your scent.";

    // Hunter brains (Hard/Nightmare) hardcode PlayerSnake as their target and never hunt
    // GuestSnake at all, so hiding from hunters would be a no-op for a guest-owned copy.
    public override bool IsEligibleForGuest => false;
}
