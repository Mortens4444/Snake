namespace SnakeGameEngine.Multiplayer;

// Turns a GameState into the wire DTOs shared by LanHost. Kept separate from GameState itself so
// the Core's simulation code never needs to know the network protocol exists.
public static class SnapshotBuilder
{
    public static HelloMessage BuildHello(GameState gameState)
    {
        var backgroundCells = new List<CellDto>();
        for (int x = 0; x < Settings.Current.MapWidth; x++)
        {
            for (int y = 0; y < Settings.Current.MapHeight; y++)
            {
                var cell = gameState.Background.GetCellAt(x, y);
                if (cell != null)
                {
                    var (displayChar, color) = cell.Value;
                    backgroundCells.Add(new CellDto(x, y, displayChar, color.R, color.G, color.B));
                }
            }
        }
        return new HelloMessage(Settings.Current.MapWidth, Settings.Current.MapHeight, backgroundCells);
    }

    public static SnapshotMessage BuildSnapshot(GameState gameState, string statusText, string? endMessage = null)
    {
        var frame = gameState.BuildFrameCells()
            .Select(cell => new CellDto(cell.X, cell.Y, cell.DisplayChar, cell.Color.R, cell.Color.G, cell.Color.B))
            .ToList();
        var vacated = gameState.VacatedLocations
            .Select(location => new PointDto(location.X, location.Y))
            .ToList();

        return new SnapshotMessage(
            frame,
            vacated,
            statusText,
            gameState.Status == GameStatus.Running,
            gameState.Status == GameStatus.Won,
            gameState.WinnerName,
            gameState.GuestAlive,
            endMessage);
    }
}
