using SnakeGameEngine.Actors;
using SnakeGameEngine.Moving;

namespace SnakeGameEngine.ConsoleUtils;

public static class ConsoleDrawer
{
    public static void DrawScreen(FoodInfo foodInfo, Snake snake)
    {
        for (int i = 0; i < snake.SnakeBodyParts.Count; i++)
        {
            DrawItem(snake.SnakeBodyParts[i]);
        }

        DrawItem(foodInfo);
    }

    public static void ClearLocation(Location location)
    {
        Console.SetCursorPosition(location.X, location.Y);
        Console.Write(" ");
    }

    private static void DrawItem(ElementInfo elementInfo)
    {
        if (elementInfo != null)
        {
            if (elementInfo.Location.X == -1)
            {
                elementInfo.Location.X = Constants.MaxX - 1;
            }
            if (elementInfo.Location.Y == -1)
            {
                elementInfo.Location.Y = Constants.MaxY - 1;
            }
            if (elementInfo.Location.X == Constants.MaxX)
            {
                elementInfo.Location.X = 0;
            }
            if (elementInfo.Location.Y == Constants.MaxY)
            {
                elementInfo.Location.Y = 0;
            }
            Console.CursorLeft = elementInfo.Location.X;
            Console.CursorTop = elementInfo.Location.Y;
            Console.ForegroundColor = elementInfo.Color;
            Console.Write(elementInfo.DisplayChar);
        }
    }
}
