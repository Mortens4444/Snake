using SnakeGameEngine.Multiplayer;

namespace SnakeGameEngine.Maui.Multiplayer;

// Draws what the host sends over Bluetooth - mirrors Snake/ConsoleUtils/MultiplayerGuestRenderer.cs,
// but a GraphicsView repaints everything from scratch every frame, so unlike the console client
// there's no need to track/restore "vacated" cells - only the latest Snapshot.Frame needs drawing.
// The guest's own perk-choice card uses the same native DisplayActionSheetAsync the single-player
// flow already uses (MainPage.ShowPerkChoiceAsync), not a custom-drawn overlay like the console.
public class GuestSnapshotDrawable : IDrawable
{
    private readonly Dictionary<(int X, int Y), CellDto> background;

    public SnapshotMessage? Snapshot { get; set; }

    public GuestSnapshotDrawable(HelloMessage hello)
    {
        background = hello.Background.ToDictionary(cell => (cell.X, cell.Y));
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Color.FromRgb(6, 20, 10);
        canvas.FillRectangle(dirtyRect);

        var snapshot = Snapshot;
        if (snapshot == null)
        {
            return;
        }

        var mapWidth = Settings.Current.MapWidth;
        var mapHeight = Settings.Current.MapHeight;
        var cellSize = Math.Min(dirtyRect.Width / mapWidth, dirtyRect.Height / mapHeight);
        var offsetX = dirtyRect.X + (dirtyRect.Width - cellSize * mapWidth) / 2;
        var offsetY = dirtyRect.Y + (dirtyRect.Height - cellSize * mapHeight) / 2;

        RectF CellRect(int x, int y) => new(offsetX + x * cellSize, offsetY + y * cellSize, cellSize, cellSize);

        foreach (var cell in background.Values)
        {
            canvas.FillColor = Color.FromRgb((int)(cell.R * 0.55), (int)(cell.G * 0.55), (int)(cell.B * 0.55));
            canvas.FillRectangle(CellRect(cell.X, cell.Y));
        }

        canvas.StrokeColor = Colors.Gray;
        canvas.StrokeSize = (float)Math.Max(2, cellSize / 3);
        canvas.DrawRectangle(offsetX, offsetY, cellSize * mapWidth, cellSize * mapHeight);

        foreach (var cell in snapshot.Frame)
        {
            canvas.FillColor = Color.FromRgb(cell.R, cell.G, cell.B);
            canvas.FillRoundedRectangle(CellRect(cell.X, cell.Y), cellSize / 4);
        }
    }
}
