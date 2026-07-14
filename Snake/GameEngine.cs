using SnakeGameEngine.Actors;
using SnakeGameEngine.ConsoleUtils;
using System.Diagnostics;

namespace SnakeGameEngine
{
    public static class GameEngine
    {
        public static void NewGame()
        {
            ConsoleKeyInfo consoleKeyInfo;
            var snake = new Snake();
            var obstacleSnakes = ObstacleProducer.GetObstacles();
            var obstacleCells = obstacleSnakes.SelectMany(obstacleSnake => obstacleSnake.Cells).ToList();
            var foodInfo = FoodProducer.GetFood(snake, obstacleCells);
            string? lastStatusLine = null;
            var tickNumber = 0;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            do
            {
                try
                {
                    consoleKeyInfo = snake.SetDirection();

                    if (consoleKeyInfo.Key == ConsoleKey.P)
                    {
                        Pause(stopwatch);
                        lastStatusLine = null;
                    }

                    if (snake.HasTouchedFood(foodInfo))
                    {
                        ConsoleDrawer.ClearLocation(foodInfo.Location);
                        foodInfo = FoodProducer.GetFood(snake, obstacleCells);

                        if (snake.SnakeBodyParts.Count >= Constants.TargetSnakeLength)
                        {
                            stopwatch.Stop();
                            var score = (int)Math.Round(snake.SnakeBodyParts.Count * 100000 / stopwatch.Elapsed.TotalSeconds);
                            ShowWinScreen(score);
                            break;
                        }
                    }

                    ConsoleDrawer.ClearLocation(snake.Tail);
                    snake.Move();

                    if (snake.HasHitObstacle(obstacleCells))
                    {
                        throw new InvalidOperationException("Game over");
                    }

                    tickNumber++;
                    if (tickNumber % Constants.ObstacleMoveEveryNthTick == 0)
                    {
                        foreach (var obstacleSnake in obstacleSnakes)
                        {
                            var vacatedTail = obstacleSnake.Move(obstacleCells, snake, foodInfo);
                            if (vacatedTail != null)
                            {
                                ConsoleDrawer.ClearLocation(vacatedTail);
                            }
                        }
                    }

                    ConsoleDrawer.DrawScreen(foodInfo, snake, obstacleCells);

                    var statusLine = $"Length: {snake.SnakeBodyParts.Count} / {Constants.TargetSnakeLength}   Time: {stopwatch.Elapsed:mm\\:ss}   P - Pause   ESC - Quit";
                    if (VirtualTerminal.IsEnabled || statusLine != lastStatusLine)
                    {
                        ConsoleDrawer.DrawStatusLine(statusLine);
                        lastStatusLine = statusLine;
                    }

                    Thread.Sleep(GetTickMilliseconds(snake));
                }
                catch (InvalidOperationException)
                {
                    stopwatch.Stop();
                    ShowGameOverScreen(snake.SnakeBodyParts.Count);
                    break;
                }
            } while (consoleKeyInfo.Key != ConsoleKey.Escape);
        }

        private static int GetTickMilliseconds(Snake snake)
        {
            var tick = Constants.InitialTickMilliseconds - snake.SnakeBodyParts.Count * Constants.SpeedUpPerBodyPart;
            return Math.Max(Constants.MinimumTickMilliseconds, tick);
        }

        private static void Pause(Stopwatch stopwatch)
        {
            stopwatch.Stop();
            ConsoleDrawer.DrawStatusLine("PAUSED - Press P to resume");

            while (Console.ReadKey(true).Key != ConsoleKey.P)
            {
            }

            stopwatch.Start();
        }

        private static void ShowGameOverScreen(int snakeLength)
        {
            Console.Clear();
            var title = EmbeddedResourceReader.Read("SnakeGameEngine.Resources.TitleArt.txt");
            GradientWriter.WriteRainbow(title, ConsoleColor.Red);
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Game over!");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Your snake was {snakeLength} parts long. Reach {Constants.TargetSnakeLength} parts to win.");
            Console.WriteLine("Press any key to return to the main menu...");
            Console.ReadKey(true);
        }

        private static void ShowWinScreen(int score)
        {
            Console.Clear();
            var title = EmbeddedResourceReader.Read("SnakeGameEngine.Resources.TitleArt.txt");
            GradientWriter.WriteRainbow(title, ConsoleColor.Green);
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Congratulations! You have won the game!");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Your score is: {score}");
            Console.Write("Enter your name: ");
            Console.CursorVisible = true;
            string? playerName = Console.ReadLine();
            Console.CursorVisible = false;

            var leaderboard = new Leaderboard("leaderboard.txt");
            leaderboard.AddScore(playerName ?? "Player", score);

            Console.WriteLine("Press any key to return to the main menu...");
            Console.ReadKey(true);
        }
    }
}
