namespace SnakeGameEngine.Multiplayer;

public sealed record CellDto(int X, int Y, char DisplayChar, int R, int G, int B);

public sealed record PointDto(int X, int Y);

// Host pre-formats the label ("passive" vs. "activate with X") so the guest doesn't need to
// duplicate that formatting logic - it only ever draws what the host tells it to.
public sealed record PerkOptionDto(string Name, string Description, string ActivationKeyLabel);

// Sent once, right after the guest connects: the static scenery and field size.
public sealed record HelloMessage(int MapWidth, int MapHeight, List<CellDto> Background);

// Sent by the guest every time its intended direction changes, and also whenever a raw key is
// pressed that might matter for perk activation/choice (digits, ESC, activation letters) even
// though those all map to GameAction.None - Key carries that raw key alongside the steering action.
public sealed record InputMessage(GameAction Action, ConsoleKey Key);

// Sent by the guest when a perk-choice card is up: ChoiceIndex is the 0-based option picked,
// or -1 to skip (ESC).
public sealed record PerkPickMessage(int ChoiceIndex);

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
    string? EndMessage,
    List<PerkOptionDto>? GuestPerkChoices,
    int GuestLevel);
