using System.Text;

namespace SnakeGameEngine
{
    class Program
    {
        public static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.CursorVisible = false;
            ConsoleKeyInfo consoleKeyInfo;
            do
            {
                var gameEngine = new GameEngine();
                var menu = new GameMenu();
                menu.Show();
                consoleKeyInfo = menu.Choose(gameEngine);
            }
            while (consoleKeyInfo.Key != ConsoleKey.Escape) ;
        }
    }
}