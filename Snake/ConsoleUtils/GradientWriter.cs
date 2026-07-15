using System.Text;

namespace SnakeGameEngine.ConsoleUtils;

public static class GradientWriter
{
    public static void WriteRainbow(string text, ConsoleColor fallbackColor)
    {
        if (!VirtualTerminal.IsEnabled)
        {
            Console.ForegroundColor = fallbackColor;
            Console.WriteLine(text);
            return;
        }

        Console.WriteLine(BuildRainbow(text, 0));
    }

    public static string BuildRainbow(string text, int hueOffset)
    {
        var stringBuilder = new StringBuilder();
        var row = 0;
        foreach (var line in text.Split('\n'))
        {
            if (row > 0)
            {
                stringBuilder.Append(Environment.NewLine);
            }

            var trimmedLine = line.TrimEnd('\r');
            for (int column = 0; column < trimmedLine.Length; column++)
            {
                stringBuilder.Append(Colorize(trimmedLine[column], GetRainbowColor(hueOffset + column * 6 + row * 15)));
            }
            row++;
        }
        return stringBuilder.ToString();
    }

    public static string Colorize(char character, (int R, int G, int B) color)
    {
        return $"\x1b[38;2;{color.R};{color.G};{color.B}m{character}\x1b[0m";
    }

    public static (int R, int G, int B) GetRainbowColor(int hue)
    {
        return RainbowColor.Get(hue);
    }
}
