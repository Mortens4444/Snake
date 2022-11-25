using SnakeGameEngine.Actors;
using SnakeGameEngine.ConsoleUtils;
using SnakeGameEngine.Moving;
using System.Text;

namespace SnakeGameEngine
{
    class Program
    {
        public static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.CursorVisible = false;
            var foodInfo = GetFood();
            ConsoleKeyInfo consoleKeyInfo = new();
            var snake = new Snake();

            do
            {
                consoleKeyInfo = SetSnakeDirection(consoleKeyInfo, snake);

                if (snake.HasTouchedFood(foodInfo))
                {
                    foodInfo = GetFood();
                }

                snake.Move();
                ConsoleDrawer.DrawScreen(foodInfo, snake);
                Thread.Sleep(140);
            } while (consoleKeyInfo.Key != ConsoleKey.Escape);
        }

        private static FoodInfo GetFood()
        {
            return new FoodInfo(new Location(Random.Shared.Next(Constants.MaxX), Random.Shared.Next(Constants.MaxY)));
        }

        private static ConsoleKeyInfo SetSnakeDirection(ConsoleKeyInfo consoleKeyInfo, Snake snake)
        {
            if (Console.KeyAvailable)
            {
                consoleKeyInfo = Console.ReadKey(true);

                snake.Direction = consoleKeyInfo.Key switch
                {
                    ConsoleKey.UpArrow => Direction.Up,
                    ConsoleKey.DownArrow => Direction.Down,
                    ConsoleKey.LeftArrow => Direction.Left,
                    ConsoleKey.RightArrow => Direction.Right,
                    _ => snake.Direction
                };
            }

            return consoleKeyInfo;
        }
    }
}