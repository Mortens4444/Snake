using SnakeGameEngine.Challenges;
using SnakeGameEngine.ConsoleUtils;
using SnakeGameEngine.Perks;

namespace SnakeGameEngine
{
    public static class GameEngine
    {
        public static void NewGame()
        {
            if (!FitsInConsole())
            {
                return;
            }

            var gameState = new GameState();
            IRenderer renderer = new ConsoleRenderer();
            renderer.BeginGame(gameState);

            while (gameState.Status == GameStatus.Running)
            {
                var action = InputReader.ReadAction(out var pressedKey);

                if (action == GameAction.Quit)
                {
                    break;
                }

                if (action == GameAction.Pause)
                {
                    Pause(gameState, renderer);
                    continue;
                }

                if (pressedKey is ConsoleKey.PrintScreen or ConsoleKey.F12)
                {
                    var fileName = ScreenshotWriter.Save(gameState);
                    ConsoleDrawer.DrawStatusLine($"Screenshot saved: {fileName}");
                    Thread.Sleep(800);
                }

                gameState.Tick(action, pressedKey);

                foreach (var soundEvent in gameState.SoundEvents)
                {
                    Sounds.Play(soundEvent);
                }
                gameState.SoundEvents.Clear();

                renderer.DrawFrame(gameState);

                if (gameState.Status == GameStatus.Running && gameState.PendingPerkChoice)
                {
                    gameState.PendingPerkChoice = false;
                    ShowPerkSelection(gameState, renderer);
                }

                Thread.Sleep(gameState.GetTickMilliseconds());
            }

            EnemyProfileStore.RecordGame(gameState);
            var newlyCompletedChallenges = RecordDailyChallengeProgress(gameState);

            if (gameState.Status == GameStatus.Won)
            {
                ShowWinScreen(gameState, newlyCompletedChallenges);
            }
            else if (gameState.Status == GameStatus.GameOver)
            {
                if (Settings.Current.LosePerksOnDeath)
                {
                    PlayerProgress.Reset();
                }
                var lastKey = ShowGameOverScreen(gameState, newlyCompletedChallenges);
                if (lastKey == ConsoleKey.R && gameState.ReplayFrames.Count > 0)
                {
                    renderer.PlayReplay(gameState);
                }
            }
        }

        // Compares before/after completion so the end screen can call out what was just achieved.
        private static List<string> RecordDailyChallengeProgress(GameState gameState)
        {
            var before = ChallengeProgressStore.LoadForToday();
            var after = ChallengeProgressStore.RecordGameResult(gameState);
            var tasks = DailyChallenge.GetTasksFor(after.DayKey);

            var newlyCompleted = new List<string>();
            for (int i = 0; i < tasks.Count && i < after.Completed.Length; i++)
            {
                var wasDone = i < before.Completed.Length && before.Completed[i];
                if (after.Completed[i] && !wasDone)
                {
                    newlyCompleted.Add(tasks[i].Description);
                }
            }
            return newlyCompleted;
        }

        internal static bool FitsInConsole()
        {
            var requiredWidth = Settings.Current.MapWidth + 2 * Constants.FieldOffsetX;
            var requiredHeight = Settings.Current.MapHeight + 2 * Constants.FieldOffsetY + 1;
            if (Console.WindowWidth >= requiredWidth && Console.WindowHeight >= requiredHeight)
            {
                return true;
            }

            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"The console window is too small for a {Settings.Current.MapWidth}x{Settings.Current.MapHeight} map.");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Needed: {requiredWidth}x{requiredHeight} characters, current: {Console.WindowWidth}x{Console.WindowHeight}.");
            Console.WriteLine("Enlarge the window or reduce the map size in Settings.");
            Console.WriteLine("Press any key to return to the main menu...");
            Console.ReadKey(true);
            return false;
        }

        private static void Pause(GameState gameState, IRenderer renderer)
        {
            gameState.Pause();
            renderer.ShowPaused();

            while (InputReader.WaitForAction() != GameAction.Pause)
            {
            }

            gameState.Resume();
        }

        // The roguelike level-up card: pick one of the offered perks.
        private static void ShowPerkSelection(GameState gameState, IRenderer renderer)
        {
            gameState.Pause();
            var choices = PerkFactory.GetRandomChoices(gameState.PlayerPerks, Settings.Current.PerkChoicesPerLevel);

            Console.Clear();
            GradientWriter.WriteRainbow($"PERK TIME!  (Level {gameState.Level})", ConsoleColor.Yellow);
            Console.WriteLine();

            if (choices.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("You already own every perk. Press any key to continue...");
                Console.ReadKey(true);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Choose a perk:");
                Console.WriteLine();
                for (int i = 0; i < choices.Count; i++)
                {
                    var perk = choices[i];
                    var activation = perk.ActivationKey == null ? "passive" : $"activate with {perk.ActivationKey}";
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($" {i + 1}. {perk.Name}  [{activation}]");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"    {perk.Description}");
                    Console.WriteLine();
                }
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"Press 1-{choices.Count} to choose, ESC to skip...");

                while (true)
                {
                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.Escape)
                    {
                        break;
                    }

                    var index = key switch
                    {
                        >= ConsoleKey.D1 and <= ConsoleKey.D9 => key - ConsoleKey.D1,
                        >= ConsoleKey.NumPad1 and <= ConsoleKey.NumPad9 => key - ConsoleKey.NumPad1,
                        _ => -1
                    };
                    if (index >= 0 && index < choices.Count)
                    {
                        gameState.PlayerPerks.Add(choices[index]);
                        SavePlayerPerks(gameState);
                        Sounds.Play(SoundEvent.PerkGained);
                        break;
                    }
                }
            }

            renderer.BeginGame(gameState);
            renderer.DrawFrame(gameState);
            gameState.Resume();
        }

        private static void SavePlayerPerks(GameState gameState)
        {
            new PlayerProgress { PerkNames = gameState.PlayerPerks.Select(perk => perk.Name).ToList() }.Save();
        }

        private static ConsoleKey ShowGameOverScreen(GameState gameState, List<string> newlyCompletedChallenges)
        {
            Console.Clear();
            ShowTitle(ConsoleColor.Red);
            Console.ForegroundColor = ConsoleColor.Red;
            var killedBy = gameState.PlayerKilledBy;
            Console.WriteLine(killedBy == null ? "Game over!" : $"Game over! {killedBy} got you!");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Your snake was {gameState.PlayerSnake.SnakeBodyParts.Count} parts long. Reach {Settings.Current.TargetSnakeLength} parts to win.");
            if (Settings.Current.LosePerksOnDeath)
            {
                Console.WriteLine("Your perks are lost.");
            }
            ShowFinalRanking(gameState);
            ShowNewlyCompletedChallenges(newlyCompletedChallenges);
            Console.WriteLine();
            Console.WriteLine("Press R to watch the replay, any other key to return to the menu...");
            return Console.ReadKey(true).Key;
        }

        private static void ShowNewlyCompletedChallenges(List<string> newlyCompletedChallenges)
        {
            if (newlyCompletedChallenges.Count == 0)
            {
                return;
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Daily challenge completed:");
            foreach (var description in newlyCompletedChallenges)
            {
                Console.WriteLine($" ✓ {description}");
            }
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void ShowWinScreen(GameState gameState, List<string> newlyCompletedChallenges)
        {
            Console.Clear();
            ShowTitle(ConsoleColor.Green);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Congratulations! You have won the game!");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Your score is: {gameState.Score}");
            ShowFinalRanking(gameState);
            ShowNewlyCompletedChallenges(newlyCompletedChallenges);

            var leaderboard = new Leaderboard("leaderboard.txt");
            leaderboard.AddScore(Settings.Current.PlayerName, gameState.Score);

            Console.WriteLine();
            Console.WriteLine("Press any key to return to the main menu...");
            Console.ReadKey(true);
        }

        private static void ShowTitle(ConsoleColor fallbackColor)
        {
            var title = EmbeddedResourceReader.Read("SnakeGameEngine.Resources.TitleArt.txt");
            GradientWriter.WriteRainbow(title, fallbackColor);
            Console.WriteLine();
        }

        private static void ShowFinalRanking(GameState gameState)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Final ranking:");

            var entries = new List<(string Name, int Length, bool IsAlive, bool IsPlayer)>
            {
                (Settings.Current.PlayerName, gameState.PlayerSnake.SnakeBodyParts.Count, gameState.Status == GameStatus.Won, true)
            };
            entries.AddRange(gameState.EnemySnakes.Select(enemySnake => (enemySnake.Personality.Name, enemySnake.Cells.Count, true, false)));
            entries.AddRange(gameState.DeadEnemyNames.Select(name => (name, 0, false, false)));

            var ranked = entries
                .OrderByDescending(entry => entry.IsAlive)
                .ThenByDescending(entry => entry.Length)
                .ToList();
            for (int i = 0; i < ranked.Count; i++)
            {
                var (name, length, isAlive, isPlayer) = ranked[i];
                Console.ForegroundColor = isPlayer ? ConsoleColor.Yellow : ConsoleColor.White;
                var state = isAlive ? $"{length} long" : length > 0 ? $"{length} long (died)" : "died";
                Console.WriteLine($" {i + 1}. {name} - {state}");
            }
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
