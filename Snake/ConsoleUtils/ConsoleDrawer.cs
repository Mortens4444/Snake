using SnakeGameEngine.Actors;
using SnakeGameEngine.Moving;

namespace SnakeGameEngine.ConsoleUtils;

public static class ConsoleDrawer
{
    public static void DrawScreen(FoodInfo foodInfo, Snake snake, List<ObstacleInfo> obstacles)
    {
        var hueOffset = Environment.TickCount / 10;

        foreach (var obstacle in obstacles)
        {
            DrawItem(obstacle, GetEmberColor(obstacle.Location, hueOffset));
        }

        for (int i = 0; i < snake.SnakeBodyParts.Count; i++)
        {
            DrawItem(snake.SnakeBodyParts[i], GradientWriter.GetRainbowColor(hueOffset + i * 12));
        }

        DrawItem(foodInfo, GradientWriter.GetRainbowColor(hueOffset + 180));
    }

    // Obstacles glow like embers: the hue sways between red (0) and yellow (60),
    // with a per-cell phase so the shimmer runs along each wall.
    private static (int R, int G, int B) GetEmberColor(Location location, int hueOffset)
    {
        var phase = ((hueOffset + location.X * 7 + location.Y * 11) % 120 + 120) % 120;
        var hue = phase <= 60 ? phase : 120 - phase;
        return GradientWriter.GetRainbowColor(hue);
    }

    public static void DrawStatusLine(string status)
    {
        Console.SetCursorPosition(0, Constants.StatusLineY);
        var paddedStatus = status.PadRight(Constants.MaxX - 1);
        if (VirtualTerminal.IsEnabled)
        {
            Console.Write(GradientWriter.BuildRainbow(paddedStatus, Environment.TickCount / 10));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(paddedStatus);
        }
    }

    public static void ClearLocation(Location location)
    {
        Console.SetCursorPosition(location.X, location.Y);
        Console.Write(" ");
    }

    private static void DrawItem(ElementInfo elementInfo, (int R, int G, int B)? rainbowColor = null)
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
            if (rainbowColor.HasValue && VirtualTerminal.IsEnabled)
            {
                Console.Write(GradientWriter.Colorize(elementInfo.DisplayChar, rainbowColor.Value));
            }
            else
            {
                Console.ForegroundColor = elementInfo.Color;
                Console.Write(elementInfo.DisplayChar);
            }
        }
    }
}
