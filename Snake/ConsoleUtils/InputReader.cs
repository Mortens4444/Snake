namespace SnakeGameEngine.ConsoleUtils;

public static class InputReader
{
    // Also reports the raw key so perk activation keys can be recognized.
    public static GameAction ReadAction(out ConsoleKey pressedKey)
    {
        pressedKey = default;
        if (!Console.KeyAvailable)
        {
            return GameAction.None;
        }

        pressedKey = Console.ReadKey(true).Key;
        return pressedKey switch
        {
            ConsoleKey.UpArrow => GameAction.MoveUp,
            ConsoleKey.DownArrow => GameAction.MoveDown,
            ConsoleKey.LeftArrow => GameAction.MoveLeft,
            ConsoleKey.RightArrow => GameAction.MoveRight,
            ConsoleKey.P => GameAction.Pause,
            ConsoleKey.Escape => GameAction.Quit,
            _ => GameAction.None
        };
    }

    public static GameAction WaitForAction()
    {
        while (true)
        {
            var action = ReadAction(out _);
            if (action != GameAction.None)
            {
                return action;
            }
            Thread.Sleep(50);
        }
    }
}
