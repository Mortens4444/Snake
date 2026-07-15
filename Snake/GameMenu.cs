using SnakeGameEngine.AI;
using SnakeGameEngine.Challenges;
using SnakeGameEngine.ConsoleUtils;

namespace SnakeGameEngine
{
    public static class GameMenu
    {
        private const string Border = "════════════════════════════════";

        private static readonly string Options = string.Join(Environment.NewLine,
            "  New Game - Press Space",
            "  LAN Multiplayer - Press M",
            "  Leaderboard - Press Enter",
            "  Daily Challenge - Press D",
            "  Settings - Press S",
            "  Quit     - Press ESC");

        private const string Title = "  S N A K E   R E L O A D E D";

        private static readonly string AppleArt = EmbeddedResourceReader.Read("SnakeGameEngine.Resources.AppleArt.txt");

        private static readonly string SnakeArt = EmbeddedResourceReader.Read("SnakeGameEngine.Resources.SnakeArt.txt");

        private static readonly string[] SnakeArtLines = SnakeArt.Split('\n').Select(line => line.TrimEnd('\r')).ToArray();

        private static readonly string MenuTop = string.Join(Environment.NewLine, Title, Border, Options, AppleArt);

        // How tall/wide the animated menu actually is, so callers can make sure the console
        // window is big enough before it gets drawn (otherwise writing it scrolls the buffer).
        // Computed piece by piece rather than by splitting MenuTop itself, since Options uses
        // Environment.NewLine internally while AppleArt/SnakeArt (raw embedded resource text)
        // use plain '\n' - a single Split(Environment.NewLine) over the mixed blob would
        // undercount the resource-art lines.
        public const int RequiredWidth = 120;

        public static readonly int RequiredHeight =
            2 // Title + Border
            + Options.Split(Environment.NewLine).Length
            + AppleArt.Split('\n').Length
            + 1 // separator before the snake art
            + SnakeArtLines.Length;

        public static void Show()
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);

            // If the window still doesn't fit the animation (resize failed or was refused by the
            // terminal), fall back to a single static draw instead of scrolling every frame.
            if (!VirtualTerminal.IsEnabled || Console.WindowHeight < RequiredHeight || Console.WindowWidth < RequiredWidth)
            {
                ShowWithoutAnimation();
                return;
            }

            // do-while, not while: a stale buffered keypress (e.g. from key repeat on the
            // previous "press any key to continue" screen) would otherwise make KeyAvailable
            // true before the very first frame is ever drawn, so the menu - title included -
            // silently skips rendering entirely and jumps straight to Choose(). That looked
            // like the title "only sometimes" showing up.
            do
            {
                // The art is a single rigid picture (head, eyes and body all need to stay lined
                // up), so every row gets the SAME offset - the whole snake sways left and right
                // as one piece. Shifting each row independently (an earlier attempt) tore the
                // head visually apart from the body instead of looking like a slither.
                var slide = (int)Math.Round(6 + 5 * Math.Sin(Environment.TickCount / 700.0));
                var padding = new string(' ', slide);
                var slidingSnakeArt = string.Join(Environment.NewLine,
                    SnakeArtLines.Select(line => (padding + line).PadRight(24)));

                Console.SetCursorPosition(0, 0);
                Console.Write(GradientWriter.BuildRainbow(MenuTop + Environment.NewLine + slidingSnakeArt, Environment.TickCount / 10));
                Thread.Sleep(50);
            }
            while (!Console.KeyAvailable);
        }

        private static void ShowWithoutAnimation()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(Title);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(Border);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(Options);
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

                case ConsoleKey.D:
                    ShowDailyChallenge();
                    break;

                case ConsoleKey.M:
                    ShowMultiplayerMenu();
                    break;

                case ConsoleKey.S:
                    ShowSettings();
                    break;
            };
            return consoleKeyInfo;
        }

        private static void ShowMultiplayerMenu()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("LAN Multiplayer");
            Console.WriteLine("---------------");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Host a game  - Press H");
            Console.WriteLine("  Join a game  - Press J");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Console.WriteLine("A guest connects as a second, network-controlled snake sharing your field.");
            Console.WriteLine("Press ESC to return...");

            var key = Console.ReadKey(true).Key;
            switch (key)
            {
                case ConsoleKey.H:
                    MultiplayerEngine.HostGame();
                    break;
                case ConsoleKey.J:
                    MultiplayerEngine.JoinGame();
                    break;
            }
        }

        private static void ShowDailyChallenge()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Daily Challenge - {DailyChallenge.TodayKey}");
            Console.WriteLine("---------------------------------");
            Console.ForegroundColor = ConsoleColor.White;

            var progress = ChallengeProgressStore.LoadForToday();
            var tasks = DailyChallenge.GetTasksFor(progress.DayKey);
            for (int i = 0; i < tasks.Count; i++)
            {
                var isDone = i < progress.Completed.Length && progress.Completed[i];
                Console.ForegroundColor = isDone ? ConsoleColor.Green : ConsoleColor.Gray;
                Console.WriteLine($" [{(isDone ? "x" : " ")}] {tasks[i].Description}");
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Console.WriteLine("A new set of tasks appears every day. Progress is checked automatically after each game.");
            Console.WriteLine("\nPress any key to return to the main menu...");
            Console.ReadKey(true);
        }

        private static void ShowSettings()
        {
            while (true)
            {
                var settings = Settings.Current;
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Settings:");
                Console.WriteLine("---------");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($" N. Snake name: {settings.PlayerName}");
                Console.WriteLine($" W. Map width: {settings.MapWidth}");
                Console.WriteLine($" H. Map height: {settings.MapHeight}");
                Console.WriteLine($" 1. Target snake length: {settings.TargetSnakeLength}");
                Console.WriteLine($" 2. Initial tick (ms, lower = faster): {settings.InitialTickMilliseconds}");
                Console.WriteLine($" 3. Minimum tick (ms, top speed): {settings.MinimumTickMilliseconds}");
                Console.WriteLine($" 4. Speed-up per body part (ms): {settings.SpeedUpPerBodyPart}");
                Console.WriteLine($" 5. Obstacle count: {settings.ObstacleCount}");
                Console.WriteLine($" 6. Obstacle min length: {settings.ObstacleMinLength}");
                Console.WriteLine($" 7. Obstacle max length: {settings.ObstacleMaxLength}");
                Console.WriteLine($" 8. Obstacles move every Nth tick: {settings.ObstacleMoveEveryNthTick}");
                Console.WriteLine($" 9. Obstacle turn chance (%): {settings.ObstacleTurnChancePercent}");
                var difficultyName = BrainFactory.DifficultyNames[Math.Clamp(settings.EnemyDifficulty, 0, BrainFactory.DifficultyNames.Length - 1)];
                Console.WriteLine($" D. Enemy difficulty: {difficultyName}");
                Console.WriteLine($" P. Points per level-up: {settings.PointsPerLevel}");
                Console.WriteLine($" C. Perk choices per level-up: {settings.PerkChoicesPerLevel}");
                Console.WriteLine($" L. Lose perks on death: {(settings.LosePerksOnDeath ? "On" : "Off")}");
                Console.WriteLine($" B. Bird visits every N minutes (0 = never): {settings.BirdIntervalMinutes}");
                Console.WriteLine($" S. Sound: {(settings.SoundEnabled ? "On" : "Off")}");
                Console.WriteLine($" E. Cheat codes: {(settings.CheatsEnabled ? "On" : "Off")}");
                Console.WriteLine(" R. Reset progress (perks + enemy profiles)");
                Console.WriteLine(" X. Reset ALL settings to defaults");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine();
                Console.WriteLine("Press a key to change a value (D/L/S/E/R/X act instantly), ESC to return...");

                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.Escape)
                {
                    return;
                }

                if (key == ConsoleKey.N)
                {
                    ShowSnakeNamePrompt(settings);
                    continue;
                }

                if (key == ConsoleKey.D)
                {
                    settings.EnemyDifficulty = (settings.EnemyDifficulty + 1) % BrainFactory.DifficultyNames.Length;
                    settings.Save();
                    continue;
                }

                if (key == ConsoleKey.L)
                {
                    settings.LosePerksOnDeath = !settings.LosePerksOnDeath;
                    settings.Save();
                    continue;
                }

                if (key == ConsoleKey.S)
                {
                    settings.SoundEnabled = !settings.SoundEnabled;
                    settings.Save();
                    continue;
                }

                if (key == ConsoleKey.E)
                {
                    settings.CheatsEnabled = !settings.CheatsEnabled;
                    settings.Save();
                    continue;
                }

                if (key == ConsoleKey.R)
                {
                    PlayerProgress.Reset();
                    EnemyProfileStore.Reset();
                    continue;
                }

                if (key == ConsoleKey.X)
                {
                    Settings.ResetToDefaults();
                    continue;
                }

                var settingNumber = key switch
                {
                    ConsoleKey.D1 or ConsoleKey.NumPad1 => 1,
                    ConsoleKey.D2 or ConsoleKey.NumPad2 => 2,
                    ConsoleKey.D3 or ConsoleKey.NumPad3 => 3,
                    ConsoleKey.D4 or ConsoleKey.NumPad4 => 4,
                    ConsoleKey.D5 or ConsoleKey.NumPad5 => 5,
                    ConsoleKey.D6 or ConsoleKey.NumPad6 => 6,
                    ConsoleKey.D7 or ConsoleKey.NumPad7 => 7,
                    ConsoleKey.D8 or ConsoleKey.NumPad8 => 8,
                    ConsoleKey.D9 or ConsoleKey.NumPad9 => 9,
                    ConsoleKey.W => 10,
                    ConsoleKey.H => 11,
                    ConsoleKey.P => 12,
                    ConsoleKey.C => 13,
                    ConsoleKey.B => 14,
                    _ => 0
                };
                if (settingNumber == 0)
                {
                    continue;
                }

                Console.Write("New value: ");
                Console.CursorVisible = true;
                var input = Console.ReadLine();
                Console.CursorVisible = false;
                if (int.TryParse(input, out var value))
                {
                    ApplySetting(settingNumber, value);
                    settings.Save();
                }
            }
        }

        private static void ShowSnakeNamePrompt(Settings settings)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Snake Name");
            Console.WriteLine("----------");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Shown in the final ranking, the leaderboard, and to LAN guests when you win or catch them.");
            Console.WriteLine();
            Console.Write("Current name: ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(settings.PlayerName);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Console.Write("New name (leave empty to keep it): ");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.CursorVisible = true;
            var nameInput = Console.ReadLine();
            Console.CursorVisible = false;
            Console.ForegroundColor = ConsoleColor.White;

            if (!string.IsNullOrWhiteSpace(nameInput))
            {
                // Commas would corrupt the leaderboard file's "name,score" line format.
                settings.PlayerName = nameInput.Trim().Replace(",", "");
                settings.Save();
            }
        }

        private static void ApplySetting(int settingNumber, int value)
        {
            var settings = Settings.Current;
            switch (settingNumber)
            {
                case 1:
                    settings.TargetSnakeLength = Math.Max(3, value);
                    break;
                case 2:
                    settings.InitialTickMilliseconds = Math.Max(10, value);
                    break;
                case 3:
                    settings.MinimumTickMilliseconds = Math.Max(10, value);
                    break;
                case 4:
                    settings.SpeedUpPerBodyPart = Math.Max(0, value);
                    break;
                case 5:
                    settings.ObstacleCount = Math.Max(0, value);
                    break;
                case 6:
                    settings.ObstacleMinLength = Math.Max(1, value);
                    settings.ObstacleMaxLength = Math.Max(settings.ObstacleMaxLength, settings.ObstacleMinLength);
                    break;
                case 7:
                    settings.ObstacleMaxLength = Math.Max(settings.ObstacleMinLength, value);
                    break;
                case 8:
                    settings.ObstacleMoveEveryNthTick = Math.Max(1, value);
                    break;
                case 9:
                    settings.ObstacleTurnChancePercent = Math.Clamp(value, 0, 100);
                    break;
                case 10:
                    settings.MapWidth = Math.Clamp(value, 20, 300);
                    break;
                case 11:
                    settings.MapHeight = Math.Clamp(value, 10, 100);
                    break;
                case 12:
                    settings.PointsPerLevel = Math.Max(1, value);
                    break;
                case 13:
                    settings.PerkChoicesPerLevel = Math.Clamp(value, 1, 5);
                    break;
                case 14:
                    settings.BirdIntervalMinutes = Math.Clamp(value, 0, 60);
                    break;
            }
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

            var profiles = EnemyProfileStore.Load();
            if (profiles.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Enemy snakes:");
                Console.WriteLine("-------------");
                foreach (var profile in profiles.OrderByDescending(profile => profile.PlayerKills).ThenByDescending(profile => profile.Survivals))
                {
                    Console.WriteLine($"{profile.Name}: player kills: {profile.PlayerKills}, survivals: {profile.Survivals}, deaths: {profile.Deaths}");
                    if (profile.PerkNames.Count > 0)
                    {
                        Console.WriteLine($"   perks: {string.Join(", ", profile.PerkNames)}");
                    }
                }
            }

            Console.WriteLine("\nPress any key to return to the main menu...");
            Console.ReadKey(true);
        }
    }
}
