using SnakeGameEngine.Actors;
using SnakeGameEngine.ConsoleUtils;
using System.Diagnostics;

namespace SnakeGameEngine
{
    public static class GameEngine
    {
        public static void NewGame() 
        {
            var foodInfo = FoodProducer.GetFood();
            ConsoleKeyInfo consoleKeyInfo;
            var snake = new Snake();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            do
            {
                try
                {
                    consoleKeyInfo = snake.SetDirection();

                    if (snake.HasTouchedFood(foodInfo))
                    {
                        ConsoleDrawer.ClearLocation(foodInfo.Location);
                        foodInfo = FoodProducer.GetFood();

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
                    ConsoleDrawer.DrawScreen(foodInfo, snake);
                    Thread.Sleep(100);
                }
                catch (InvalidOperationException)
                {
                    break;
                }
            } while (consoleKeyInfo.Key != ConsoleKey.Escape);
        }

        private static void ShowWinScreen(int score)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            var title = EmbeddedResourceReader.Read("SnakeGameEngine.Resources.TitleArt.txt");
            Console.WriteLine(title);
            Console.WriteLine();
            Console.WriteLine("Congratulations! You have won the game!");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Your score is: {score}");
            Console.Write("Enter your name: ");
            string? playerName = Console.ReadLine();

            var leaderboard = new Leaderboard("leaderboard.txt");
            leaderboard.AddScore(playerName ?? "Player", score);

            Console.WriteLine("Press any key to return to the main menu...");
            Console.ReadKey(true);
        }
    }
}
