using SnakeGameEngine.Moving;

namespace SnakeGameEngine;

// The generated scenery as pure data; clients render it themselves.
public class Background
{
    private const int GrassTuftPercent = 15;
    private const int TreeCount = 5;
    private const int BushCount = 12;

    private static readonly char[] GrassChars = { '.', ',', '\'', '`' };
    private static readonly char[] WaveChars = { '~', '≈' };

    private sealed record Cell(char DisplayChar, (int R, int G, int B) Color);

    private readonly int mapWidth = Settings.Current.MapWidth;
    private readonly int mapHeight = Settings.Current.MapHeight;
    private readonly Cell?[,] cells;
    private readonly List<(int X, int Y)> waterCells = new();
    private readonly List<(int X, int Y)> leafCells = new();
    private readonly HashSet<(int X, int Y)> waterSet = new();
    private readonly HashSet<(int X, int Y)> canopySet = new();
    private readonly HashSet<(int X, int Y)> grassSet = new();

    public IReadOnlyList<(int X, int Y)> WaterCells => waterCells;

    public IReadOnlyList<(int X, int Y)> LeafCells => leafCells;

    public IReadOnlyCollection<(int X, int Y)> CanopyCells => canopySet;

    private Background()
    {
        cells = new Cell?[mapWidth, mapHeight];
    }

    public static Background Generate()
    {
        var background = new Background();
        background.AddGrass();
        background.AddLake();
        background.AddTrees();
        background.AddBushes();
        return background;
    }

    public (char DisplayChar, (int R, int G, int B) Color)? GetCellAt(int x, int y)
    {
        var cell = cells[x, y];
        return cell == null ? null : (cell.DisplayChar, cell.Color);
    }

    public (char DisplayChar, (int R, int G, int B) Color)? GetCellAt(Location location)
    {
        return GetCellAt(location.X, location.Y);
    }

    public char GetDisplayCharAt(int x, int y)
    {
        return cells[x, y]?.DisplayChar ?? ' ';
    }

    // Terrain queries - the scenery is a gameplay element too.
    public bool IsWaterAt(Location location)
    {
        return waterSet.Contains((location.X, location.Y));
    }

    public bool IsCanopyAt(Location location)
    {
        return canopySet.Contains((location.X, location.Y));
    }

    public bool IsCanopyAt(int x, int y)
    {
        return canopySet.Contains((x, y));
    }

    public bool IsGrassAt(Location location)
    {
        return grassSet.Contains((location.X, location.Y));
    }

    private void AddGrass()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (Random.Shared.Next(100) < GrassTuftPercent)
                {
                    var color = (Random.Shared.Next(10, 35), Random.Shared.Next(60, 110), Random.Shared.Next(10, 35));
                    cells[x, y] = new Cell(GrassChars[Random.Shared.Next(GrassChars.Length)], color);
                    grassSet.Add((x, y));
                }
            }
        }
    }

    private void AddLake()
    {
        var radiusX = Math.Max(3, Random.Shared.Next(mapWidth / 12, mapWidth / 7 + 1));
        var radiusY = Math.Max(2, Random.Shared.Next(mapHeight / 7, mapHeight / 4 + 1));
        var centerX = Random.Shared.Next(radiusX, Math.Max(radiusX + 1, mapWidth - radiusX));
        var centerY = Random.Shared.Next(radiusY, Math.Max(radiusY + 1, mapHeight - radiusY));

        for (int x = Math.Max(0, centerX - radiusX); x <= Math.Min(mapWidth - 1, centerX + radiusX); x++)
        {
            for (int y = Math.Max(0, centerY - radiusY); y <= Math.Min(mapHeight - 1, centerY + radiusY); y++)
            {
                var normalizedDistance = Math.Pow((x - centerX) / (double)radiusX, 2) + Math.Pow((y - centerY) / (double)radiusY, 2);
                var wobble = Random.Shared.NextDouble() * 0.2 - 0.1;
                if (normalizedDistance <= 1 + wobble)
                {
                    var depth = 1 - Math.Min(normalizedDistance, 1);
                    var color = (30, (int)(90 + depth * 50), (int)(140 + depth * 70));
                    cells[x, y] = new Cell(WaveChars[Random.Shared.Next(WaveChars.Length)], color);
                    waterCells.Add((x, y));
                    waterSet.Add((x, y));
                    grassSet.Remove((x, y));
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

            for (int x = Math.Max(0, centerX - radius); x <= Math.Min(mapWidth - 1, centerX + radius); x++)
            {
                for (int y = Math.Max(0, centerY - radius); y <= Math.Min(mapHeight - 1, centerY + radius); y++)
                {
                    var deltaX = x - centerX;
                    var deltaY = y - centerY;
                    // The snake spawn area must stay tree-free, since canopies are deadly.
                    var isNearSnakeStart = Math.Abs(x - mapWidth / 2) <= Constants.ObstacleFreeZoneRadius
                        && Math.Abs(y - mapHeight / 2) <= Constants.ObstacleFreeZoneRadius;
                    // Console cells are taller than wide, so the crown is squashed vertically to look round.
                    if (deltaX * deltaX + deltaY * deltaY * 4 <= radius * radius && !IsWater(x, y) && !isNearSnakeStart)
                    {
                        var color = (20, Random.Shared.Next(70, 120), Random.Shared.Next(15, 40));
                        cells[x, y] = new Cell('♣', color);
                        leafCells.Add((x, y));
                        canopySet.Add((x, y));
                        grassSet.Remove((x, y));
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
            leafCells.Add((x, y));
            grassSet.Add((x, y));
        }
    }

    private (int X, int Y) FindDryLocation()
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            var x = Random.Shared.Next(mapWidth);
            var y = Random.Shared.Next(mapHeight);
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
