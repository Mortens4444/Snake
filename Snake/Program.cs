﻿namespace Snake
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
            var snake = new SnakeBody();
            do
            {
                if (Console.KeyAvailable)
                { 
                    consoleKeyInfo = Console.ReadKey(true);
                    if (consoleKeyInfo.Key == ConsoleKey.UpArrow)
                    {
                        snake.Direction = Direction.Up;
                    }
                    else if (consoleKeyInfo.Key == ConsoleKey.DownArrow)
                    {
                        snake.Direction = Direction.Down;
                    }
                    else if (consoleKeyInfo.Key == ConsoleKey.LeftArrow)
                    {
                        snake.Direction = Direction.Left;
                    }
                    else if (consoleKeyInfo.Key == ConsoleKey.RightArrow)
                    {
                        snake.Direction = Direction.Right;
                    }
                }
               
                if (snake.Head.Equals(foodLocation))
                {
                    foodGenerated = false;
                }

                for (int i = 0; i < snake.SnakeParts.Count; i++)
                {
                    DrawItem(snake.SnakeParts[i].Location, snake.SnakeParts[i].DisplayChar, snake.SnakeParts[i].Color);
                }

                if (!foodGenerated) 
                {
                    foodLocation = new Location(random.Next(Constants.MaxX), random.Next(Constants.MaxY));
                    foodGenerated = true;
                }
                DrawItem(foodLocation, 'O', ConsoleColor.Red);
                snake.Move();
                Thread.Sleep(100);
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