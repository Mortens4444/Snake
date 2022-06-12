namespace Snake
{
    class Program
    {
        public static void Main()
        {
            Console.CursorVisible = false;
            var foodGenerated = false;
            Location foodLocation = new(0, 0);
            ConsoleKeyInfo consoleKeyInfo = new();
            var random = new Random();
            do
            {
                if (Console.KeyAvailable)
                { 
                    consoleKeyInfo = Console.ReadKey(true);
                }
               
                var snake = new SnakeBody();
                for (int i = 0; i < snake.SnakeParts.Count; i++)
                {
                    DrawItem(snake.SnakeParts[i], 'X');
                }

                if (!foodGenerated) 
                {
                    foodLocation = new Location(random.Next(Constants.MaxX), random.Next(Constants.MaxY));
                    foodGenerated = true;
                }
                DrawItem(foodLocation, 'O');
            } while (consoleKeyInfo.Key != ConsoleKey.Escape);
        }

        static void DrawItem(Location location, char ch)
        {
            Program.DrawItem(location.X, location.Y, ch);
        }

        static void DrawItem(int x, int y, char ch)
        {
            Console.CursorLeft = 0;
            Console.CursorTop = 0;
            for (int i = 0; i < y - 1; i++)
            {
                Console.WriteLine();
            }
            Console.WriteLine(ch.ToString().PadLeft(x, ' '));
        }
    }
}