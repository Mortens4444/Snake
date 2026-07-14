using SnakeGameEngine.ConsoleUtils;

namespace SnakeGameEngine
{
    public static class GameMenu
    {
        private const string Border = "════════════════════════════════";

        private static readonly string Options = string.Join(Environment.NewLine,
            "  New Game - Press Space",
            "  Leaderboard - Press Enter",
            "  Quit     - Press ESC");

        private static readonly string AppleArt = EmbeddedResourceReader.Read("SnakeGameEngine.Resources.AppleArt.txt");

        private static readonly string SnakeArt = EmbeddedResourceReader.Read("SnakeGameEngine.Resources.SnakeArt.txt");

        private static readonly string MenuText = string.Join(Environment.NewLine, Border, Options, Border, AppleArt, SnakeArt);

        public static void Show()
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);

            if (!VirtualTerminal.IsEnabled)
            {
                ShowWithoutAnimation();
                return;
            }

            while (!Console.KeyAvailable)
            {
                Console.SetCursorPosition(0, 0);
                Console.Write(GradientWriter.BuildRainbow(MenuText, Environment.TickCount / 10));
                Thread.Sleep(50);
            }
        }

        private static void ShowWithoutAnimation()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(Border);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(Options);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(Border);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(AppleArt);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(SnakeArt);
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
