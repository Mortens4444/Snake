namespace SnakeGameEngine
{
    public static class GameMenu
    {
        public static void Show()
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Console.WriteLine("New Game - Space");
            Console.WriteLine("Quit - ESC");
        }

        public static ConsoleKeyInfo Choose()
        {
            var consoleKeyInfo = Console.ReadKey(true);

            switch (consoleKeyInfo.Key)
            {
                case ConsoleKey.Spacebar:
                    Console.Clear();
                    GameEngine.NewGame();
                    break;

                case ConsoleKey.Escape:
                    Environment.Exit(0);
                    break;
            };
            return consoleKeyInfo;
        }
    }
}
