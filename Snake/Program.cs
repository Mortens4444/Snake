namespace Snake
{
    class Program
    {
        public static void Main()
        {
            Console.CursorVisible = false;
            var foodGenerated = false;
            Location? foodLocation = null;
            ConsoleKeyInfo consoleKeyInfo = new();
            var random = new Random();
            do
            {
                if (Console.KeyAvailable)
                { 
                    consoleKeyInfo = Console.ReadKey(true);
                }
               
                var snake = new SnakeBody();
                if (snake.Head.Equals(foodLocation))
                {
                    foodGenerated = false;
                }

                for (int i = 0; i < snake.SnakeParts.Count; i++)
                {
                    DrawItem(snake.SnakeParts[i].Item1, 'X', snake.SnakeParts[i].Item2);
                }

                if (!foodGenerated) 
                {
                    foodLocation = new Location(random.Next(Constants.MaxX), random.Next(Constants.MaxY));
                    foodGenerated = true;
                }
                DrawItem(foodLocation, 'O', ConsoleColor.Red);
            } while (consoleKeyInfo.Key != ConsoleKey.Escape);
        }

        static void DrawItem(Location? location, char ch, ConsoleColor color)
        {
            if (location == null)
            {
                return;
            }
            Program.DrawItem(location.X, location.Y, ch, color);
        }

        static void DrawItem(int x, int y, char ch, ConsoleColor color)
        {
            Console.CursorLeft = 0;
            Console.CursorTop = 0;
            for (int i = 0; i < y - 1; i++)
            {
                Console.WriteLine();
            }
            Console.ForegroundColor = color;
            Console.WriteLine(ch.ToString().PadLeft(x, ' '));
        }
    }
}