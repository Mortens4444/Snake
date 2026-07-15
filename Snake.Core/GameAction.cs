namespace SnakeGameEngine;

// Platform-independent player intents; each client (console, later MAUI) maps its own input to these.
public enum GameAction
{
    None,
    MoveUp,
    MoveDown,
    MoveLeft,
    MoveRight,
    Pause,
    Quit
}
