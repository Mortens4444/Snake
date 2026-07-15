using SnakeGameEngine.Moving;

namespace SnakeGameEngine.Perks;

public class PoisonTrailPerk : Perk
{
    private static readonly (int X, int Y)[] CloudOffsets = { (0, 0), (1, 0), (-1, 0), (0, 1), (0, -1) };

    public override string Name => "Poison Trail";

    public override string Description => "Drop a poison cloud at your tail; enemies inside move at half speed.";

    public override ConsoleKey? ActivationKey => ConsoleKey.O;

    public override int CooldownTicks => 100;

    protected override void OnActivate(GameState gameState)
    {
        var tail = gameState.PlayerSnake.Tail;
        foreach (var (offsetX, offsetY) in CloudOffsets)
        {
            var location = new Location(tail.X + offsetX, tail.Y + offsetY);
            if (location.X >= 0 && location.X < Settings.Current.MapWidth
                && location.Y >= 0 && location.Y < Settings.Current.MapHeight)
            {
                gameState.PoisonClouds.Add(new PoisonCloud(location, gameState.TickNumber + 120));
            }
        }
    }
}
