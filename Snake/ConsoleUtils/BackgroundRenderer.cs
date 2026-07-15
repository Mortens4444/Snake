using SnakeGameEngine.Moving;
using System.Text;

namespace SnakeGameEngine.ConsoleUtils;

// Console rendering of the Background model (which itself is pure data in the Core).
public static class BackgroundRenderer
{
    public static void Draw(Background background)
    {
        if (!VirtualTerminal.IsEnabled)
        {
            return;
        }

        for (int y = 0; y < Settings.Current.MapHeight; y++)
        {
            Console.SetCursorPosition(Constants.FieldOffsetX, Constants.FieldOffsetY + y);
            var stringBuilder = new StringBuilder();
            for (int x = 0; x < Settings.Current.MapWidth; x++)
            {
                var cell = background.GetCellAt(x, y);
                stringBuilder.Append(cell == null ? " " : GradientWriter.Colorize(cell.Value.DisplayChar, cell.Value.Color));
            }
            Console.Write(stringBuilder);
        }
    }

    // Restores the scenery under a cell that an actor has just left.
    public static void RestoreLocation(Background background, Location location)
    {
        Console.SetCursorPosition(location.X + Constants.FieldOffsetX, location.Y + Constants.FieldOffsetY);
        var cell = background.GetCellAt(location);
        if (cell != null && VirtualTerminal.IsEnabled)
        {
            Console.Write(GradientWriter.Colorize(cell.Value.DisplayChar, cell.Value.Color));
        }
        else
        {
            Console.Write(' ');
        }
    }

    // Gentle ambient animation: the lake ripples and a few leaves rustle each frame.
    // Actors are drawn right after this, so they always end up on top.
    public static void Animate(Background background, int tickNumber)
    {
        if (!VirtualTerminal.IsEnabled)
        {
            return;
        }

        foreach (var (x, y) in background.WaterCells)
        {
            var cell = background.GetCellAt(x, y);
            if (cell == null)
            {
                continue;
            }
            var (r, g, b) = cell.Value.Color;
            var wave = 0.7 + 0.3 * Math.Sin(tickNumber / 4.0 + x * 0.6 + y * 1.1);
            var color = ((int)(r * wave), (int)(g * wave), (int)(b * wave));
            var displayChar = (tickNumber / 6 + x + y) % 7 == 0 ? '≈' : cell.Value.DisplayChar;
            Console.SetCursorPosition(x + Constants.FieldOffsetX, y + Constants.FieldOffsetY);
            Console.Write(GradientWriter.Colorize(displayChar, color));
        }

        for (int i = 0; i < 10 && background.LeafCells.Count > 0; i++)
        {
            var (x, y) = background.LeafCells[Random.Shared.Next(background.LeafCells.Count)];
            var cell = background.GetCellAt(x, y);
            if (cell == null)
            {
                continue;
            }
            var (r, g, b) = cell.Value.Color;
            var shimmer = 0.8 + Random.Shared.NextDouble() * 0.35;
            var color = (
                Math.Min(255, (int)(r * shimmer)),
                Math.Min(255, (int)(g * shimmer)),
                Math.Min(255, (int)(b * shimmer)));
            Console.SetCursorPosition(x + Constants.FieldOffsetX, y + Constants.FieldOffsetY);
            Console.Write(GradientWriter.Colorize(cell.Value.DisplayChar, color));
        }
    }
}
