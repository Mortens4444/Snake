namespace SnakeGameEngine.Perks;

// The collision exemption is applied in GameState.TrySurviveCollisions.
public class WoodpeckerPerk : Perk
{
    public override string Name => "Woodpecker";

    public override string Description => "You chew through tree canopies instead of dying in them.";
}
