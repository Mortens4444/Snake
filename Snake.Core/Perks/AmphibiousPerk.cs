namespace SnakeGameEngine.Perks;

// The speed effect is applied in GameState.GetTickMilliseconds.
public class AmphibiousPerk : Perk
{
    public override string Name => "Amphibious";

    public override string Description => "Water no longer slows you down - you swim 20% faster than you move on land.";
}
