namespace SnakeGameEngine.Multiplayer;

public sealed record CellDto(int X, int Y, char DisplayChar, int R, int G, int B);

public sealed record PointDto(int X, int Y);

// Sent once, right after the guest connects: the static scenery and field size.
public sealed record HelloMessage(int MapWidth, int MapHeight, List<CellDto> Background);

// Sent by the guest every time its intended direction changes.
public sealed record InputMessage(GameAction Action);

// Sent by the host after every tick: only what changed (drawn cells + vacated cells),
// so the wire payload stays small even on a large map.
public sealed record SnapshotMessage(
    List<CellDto> Frame,
    List<PointDto> Vacated,
    string StatusText,
    bool IsRunning,
    bool IsWon,
    string? WinnerName,
    bool GuestAlive,
    string? EndMessage);
