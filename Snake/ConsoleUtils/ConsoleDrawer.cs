using SnakeGameEngine.Actors;
using System.Text;

namespace SnakeGameEngine.ConsoleUtils
{
    public static class ConsoleDrawer
    {
        private static readonly string ClearStringText;

        static ConsoleDrawer()
        {
            var stringBuilder = new StringBuilder();
            var line = String.Empty.PadLeft(Constants.MaxX, ' ');
            for (int i = 0; i < Constants.MaxY; i++)
            {
                stringBuilder.AppendLine(line);
            }

            ClearStringText = stringBuilder.ToString();
        }

        public static void DrawScreen(FoodInfo? foodInfo, Snake snake)
        {
            ClearScreen();
            for (int i = 0; i < snake.SnakeBodyParts.Count; i++)
            {
                DrawItem(snake.SnakeBodyParts[i]);
            }

            DrawItem(foodInfo);
        }

        private static void ClearScreen()
        {
            Console.CursorLeft = 0;
            Console.CursorTop = 0;
            Console.Write(ClearStringText);
        }

        private static void DrawItem(ElementInfo? elementInfo)
        {
            if (elementInfo != null)
            {
                Console.CursorLeft = elementInfo.Location.X;
                Console.CursorTop = elementInfo.Location.Y;
                Console.ForegroundColor = elementInfo.Color;
                Console.Write(elementInfo.DisplayChar);
            }
        }
    }
}
