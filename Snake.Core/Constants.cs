namespace SnakeGameEngine;

// Fixed layout values; the gameplay tunables live in Settings.
public static class Constants
{
    // The playfield is drawn inside the border wall, so everything shifts by one cell.
    public const int FieldOffsetX = 1;

    public const int FieldOffsetY = 1;

    public static int StatusLineY => Settings.Current.MapHeight + FieldOffsetY + 1;

    // No obstacle may spawn this close to the snake's starting position.
    public const int ObstacleFreeZoneRadius = 6;
}
