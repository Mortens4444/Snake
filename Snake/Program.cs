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
                GameMenu.Show();
                consoleKeyInfo = GameMenu.Choose();
            }
            while (consoleKeyInfo.Key != ConsoleKey.Escape);
        }
    }
}