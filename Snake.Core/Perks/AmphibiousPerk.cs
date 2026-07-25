namespace SnakeGameEngine.Perks;

// The speed effect is applied in GameState.GetTickMilliseconds.
public class AmphibiousPerk : Perk
{
    public override string Name => "Amphibious";

    public override string Description => "Water no longer slows you down - you swim 20% faster than you move on land.";

    // GameState.GetTickMilliseconds() only ever reads the host's PlayerPerks; the guest moves on
    // the same shared tick as the host, so a guest-owned speed perk would have zero effect.
    public override bool IsEligibleForGuest => false;
}
