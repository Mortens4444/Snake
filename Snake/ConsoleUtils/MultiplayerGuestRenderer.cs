using SnakeGameEngine.Multiplayer;

namespace SnakeGameEngine.ConsoleUtils;

// Draws what the host sends over the wire. The guest never runs a simulation of its own -
// it only knows the static background (sent once) and the sparse per-tick frame/vacated cells.
public class MultiplayerGuestRenderer
{
    private readonly Dictionary<(int X, int Y), CellDto> background;

    public MultiplayerGuestRenderer(HelloMessage hello)
    {
        background = hello.Background.ToDictionary(cell => (cell.X, cell.Y));
    }

    public void DrawInitial()
    {
        Console.Clear();
        ConsoleDrawer.DrawBorder();

        foreach (var cell in background.Values)
        {
            DrawCell(cell);
        }
    }

    public void DrawSnapshot(SnapshotMessage snapshot)
    {
        foreach (var point in snapshot.Vacated)
        {
            if (background.TryGetValue((point.X, point.Y), out var backgroundCell))
            {
                DrawCell(backgroundCell);
            }
            else
            {
                Console.SetCursorPosition(point.X + Constants.FieldOffsetX, point.Y + Constants.FieldOffsetY);
                Console.Write(' ');
            }
        }

        foreach (var cell in snapshot.Frame)
        {
            DrawCell(cell);
        }

        ConsoleDrawer.DrawStatusLine(snapshot.StatusText);
    }

    private static void DrawCell(CellDto cell)
    {
        Console.SetCursorPosition(cell.X + Constants.FieldOffsetX, cell.Y + Constants.FieldOffsetY);
        if (VirtualTerminal.IsEnabled)
        {
            Console.Write(GradientWriter.Colorize(cell.DisplayChar, (cell.R, cell.G, cell.B)));
        }
        else
        {
            Console.Write(cell.DisplayChar);
        }
    }
}
