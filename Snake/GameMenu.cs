namespace SnakeGameEngine
{
    public static class GameMenu
    {
        public static void Show()
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("════════════════════════════════");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  New Game - Press Space");
            Console.WriteLine("  Leaderboard - Press Enter");
            Console.WriteLine("  Quit     - Press ESC");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("════════════════════════════════");

            var apple = EmbeddedResourceReader.Read("SnakeGameEngine.Resources.AppleArt.txt");
            var snake = EmbeddedResourceReader.Read("SnakeGameEngine.Resources.SnakeArt.txt");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(apple);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(snake);
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

                case ConsoleKey.Enter:
                    ShowLeaderboard();
                    break;
            };
            return consoleKeyInfo;
        }

        private static void ShowLeaderboard()
        {
            Console.Clear();
            Console.WriteLine("Leaderboard:");
            Console.WriteLine("------------");

            var leaderboard = new Leaderboard("leaderboard.txt");
            var topScores = leaderboard.GetTopScores(10);

            foreach (var entry in topScores)
            {
                Console.WriteLine($"{entry.Key}: {entry.Value}");
            }

            Console.WriteLine("\nPress any key to return to the main menu...");
            Console.ReadKey(true);
        }
    }
}
