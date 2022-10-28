using SnakeGameEngine.Actors;
using SnakeGameEngine.ConsoleUtils;
using SnakeGameEngine.Moving;

namespace SnakeGameEngine
{
    class Program
    {
        public static void Main()
        {
            Console.CursorVisible = false;
            var foodInfo = GetFood();
            ConsoleKeyInfo consoleKeyInfo = new();
            var snake = new Snake();

            do
            {
                consoleKeyInfo = SetSnakeDirection(consoleKeyInfo, snake);

                if (snake.Head.Equals(foodInfo?.Location))
                {
                    foodInfo = GetFood();
                }

                snake.Move();
                ConsoleDrawer.DrawScreen(foodInfo, snake);
                Thread.Sleep(100);
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