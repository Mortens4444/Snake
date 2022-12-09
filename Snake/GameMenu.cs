using System;

namespace SnakeGameEngine
{
    public class GameMenu
    {
        public void Show()
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Console.WriteLine("New Game - Space");
            Console.WriteLine("Quit - ESC");
        }

        public ConsoleKeyInfo Choose(GameEngine gameEngine)
        {
            var consoleKeyInfo = Console.ReadKey(true);

            switch (consoleKeyInfo.Key)
            {
                case ConsoleKey.Spacebar:
                    gameEngine.NewGame();
                    break;

                case ConsoleKey.Escape:
                    Environment.Exit(0);
                    break;
            };
            return consoleKeyInfo;
        }
    }
}
