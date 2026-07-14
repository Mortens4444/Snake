using SnakeGameEngine.ConsoleUtils;
using SnakeGameEngine.Moving;
using System.Text;

namespace SnakeGameEngine;

public class Background
{
    private const int GrassTuftPercent = 15;
    private const int TreeCount = 5;
    private const int BushCount = 12;

    private static readonly char[] GrassChars = { '.', ',', '\'', '`' };
    private static readonly char[] WaveChars = { '~', '≈' };

    private sealed record Cell(char DisplayChar, (int R, int G, int B) Color);

    private readonly Cell?[,] cells = new Cell?[Constants.MaxX, Constants.MaxY];

    public static Background Generate()
    {
        var background = new Background();
        background.AddGrass();
        background.AddLake();
        background.AddTrees();
        background.AddBushes();
        return background;
    }

    public void Draw()
    {
        if (!VirtualTerminal.IsEnabled)
        {
            return;
        }

        Console.SetCursorPosition(0, 0);
        var stringBuilder = new StringBuilder();
        for (int y = 0; y < Constants.MaxY; y++)
        {
            if (y > 0)
            {
                stringBuilder.Append(Environment.NewLine);
            }
            for (int x = 0; x < Constants.MaxX; x++)
            {
                var cell = cells[x, y];
                stringBuilder.Append(cell == null ? " " : GradientWriter.Colorize(cell.DisplayChar, cell.Color));
            }
        }
        Console.Write(stringBuilder);
    }

    // Restores the scenery under a cell that an actor has just left.
    public void RestoreLocation(Location location)
    {
        Console.SetCursorPosition(location.X, location.Y);
        var cell = cells[location.X, location.Y];
        if (cell != null && VirtualTerminal.IsEnabled)
        {
            Console.Write(GradientWriter.Colorize(cell.DisplayChar, cell.Color));
        }
        else
        {
            Console.Write(' ');
        }
    }

    private void AddGrass()
    {
        for (int x = 0; x < Constants.MaxX; x++)
        {
            for (int y = 0; y < Constants.MaxY; y++)
            {
                if (Random.Shared.Next(100) < GrassTuftPercent)
                {
                    var color = (Random.Shared.Next(10, 35), Random.Shared.Next(60, 110), Random.Shared.Next(10, 35));
                    cells[x, y] = new Cell(GrassChars[Random.Shared.Next(GrassChars.Length)], color);
                }
            }
        }
    }

    private void AddLake()
    {
        var centerX = Random.Shared.Next(18, Constants.MaxX - 18);
        var centerY = Random.Shared.Next(7, Constants.MaxY - 7);
        var radiusX = Random.Shared.Next(10, 17);
        var radiusY = Random.Shared.Next(4, 7);

        for (int x = Math.Max(0, centerX - radiusX); x <= Math.Min(Constants.MaxX - 1, centerX + radiusX); x++)
        {
            for (int y = Math.Max(0, centerY - radiusY); y <= Math.Min(Constants.MaxY - 1, centerY + radiusY); y++)
            {
                var normalizedDistance = Math.Pow((x - centerX) / (double)radiusX, 2) + Math.Pow((y - centerY) / (double)radiusY, 2);
                var wobble = Random.Shared.NextDouble() * 0.2 - 0.1;
                if (normalizedDistance <= 1 + wobble)
                {
                    var depth = 1 - Math.Min(normalizedDistance, 1);
                    var color = (30, (int)(90 + depth * 50), (int)(140 + depth * 70));
                    cells[x, y] = new Cell(WaveChars[Random.Shared.Next(WaveChars.Length)], color);
                }
            }
        }
    }

    private void AddTrees()
    {
        for (int i = 0; i < TreeCount; i++)
        {
            var (centerX, centerY) = FindDryLocation();
            var radius = Random.Shared.Next(2, 5);

            for (int x = Math.Max(0, centerX - radius); x <= Math.Min(Constants.MaxX - 1, centerX + radius); x++)
            {
                for (int y = Math.Max(0, centerY - radius); y <= Math.Min(Constants.MaxY - 1, centerY + radius); y++)
                {
                    var deltaX = x - centerX;
                    var deltaY = y - centerY;
                    // Console cells are taller than wide, so the crown is squashed vertically to look round.
                    if (deltaX * deltaX + deltaY * deltaY * 4 <= radius * radius && !IsWater(x, y))
                    {
                        var color = (20, Random.Shared.Next(70, 120), Random.Shared.Next(15, 40));
                        cells[x, y] = new Cell('♣', color);
                    }
                }
            }
        }
    }

    private void AddBushes()
    {
        for (int i = 0; i < BushCount; i++)
        {
            var (x, y) = FindDryLocation();
            var color = (50, Random.Shared.Next(130, 180), 50);
            cells[x, y] = new Cell('♣', color);
        }
    }

    private (int X, int Y) FindDryLocation()
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            var x = Random.Shared.Next(Constants.MaxX);
            var y = Random.Shared.Next(Constants.MaxY);
            if (!IsWater(x, y))
            {
                return (x, y);
            }
        }
        return (0, 0);
    }

    private bool IsWater(int x, int y)
    {
        return cells[x, y] is { } cell && (cell.DisplayChar == '~' || cell.DisplayChar == '≈');
    }
}
