using SnakeGameEngine.Actors;
using SnakeGameEngine.Moving;
using SnakeGameEngine.Perks;

namespace SnakeGameEngine.ConsoleUtils;

public static class ConsoleDrawer
{
    // Non-blocking multiplayer perk card: unlike GameEngine.ShowPerkSelection (which owns the
    // whole screen via Console.Clear()), this draws an overlay box on top of the live field and
    // must be called again every iteration while the choice is pending - DrawFrame repaints the
    // field underneath it every tick.
    public static void DrawPerkCard(List<Perk> choices, int level)
    {
        var options = choices
            .Select(perk => (perk.Name, perk.Description,
                perk.ActivationKey == null ? "passive" : $"activate with {perk.ActivationKey}"))
            .ToList();
        DrawPerkCard(options, level);
    }

    internal static void DrawPerkCard(List<(string Name, string Description, string ActivationLabel)> options, int level)
    {
        var lines = new List<(string Text, ConsoleColor Color)>
        {
            ($"PERK TIME!  (Level {level})", ConsoleColor.Yellow)
        };

        if (options.Count == 0)
        {
            lines.Add(("You already own every perk.", ConsoleColor.White));
        }
        else
        {
            lines.Add(("Choose a perk:", ConsoleColor.White));
            for (int i = 0; i < options.Count; i++)
            {
                var (name, description, activationLabel) = options[i];
                lines.Add(($" {i + 1}. {name}  [{activationLabel}]", ConsoleColor.Cyan));
                lines.Add(($"    {description}", ConsoleColor.Gray));
            }
            lines.Add(($"Press 1-{options.Count} to choose, ESC to skip", ConsoleColor.White));
        }

        var maxTotalWidth = Settings.Current.MapWidth;
        var contentWidth = Math.Clamp(lines.Max(line => line.Text.Length), 10, maxTotalWidth - 4);
        var totalWidth = contentWidth + 4;
        var totalHeight = lines.Count + 2;

        var left = Constants.FieldOffsetX + Math.Max(0, (Settings.Current.MapWidth - totalWidth) / 2);
        var top = Constants.FieldOffsetY + Math.Max(0, (Settings.Current.MapHeight - totalHeight) / 2);

        DrawCardBorder(left, top, contentWidth, '╔', '╗');
        for (int i = 0; i < lines.Count; i++)
        {
            DrawCardContentLine(left, top + 1 + i, contentWidth, lines[i].Text, lines[i].Color);
        }
        DrawCardBorder(left, top + 1 + lines.Count, contentWidth, '╚', '╝');
    }

    private static void DrawCardBorder(int left, int row, int contentWidth, char leftCorner, char rightCorner)
    {
        Console.SetCursorPosition(left, row);
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.Write(leftCorner);
        Console.Write(new string('═', contentWidth + 2));
        Console.Write(rightCorner);
    }

    private static void DrawCardContentLine(int left, int row, int contentWidth, string text, ConsoleColor color)
    {
        var clipped = text.Length > contentWidth ? text[..contentWidth] : text.PadRight(contentWidth);
        Console.SetCursorPosition(left, row);
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.Write("║ ");
        Console.ForegroundColor = color;
        Console.Write(clipped);
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.Write(" ║");
    }

    public static void DrawScreen(GameState gameState)
    {
        var hueOffset = Environment.TickCount / 10;

        foreach (var poisonCloud in gameState.PoisonClouds)
        {
            DrawCell(poisonCloud.Location, '▒', (110, 220, 50), ConsoleColor.DarkGreen);
        }

        foreach (var enemySnake in gameState.EnemySnakes)
        {
            for (int i = 0; i < enemySnake.Cells.Count; i++)
            {
                DrawItem(enemySnake.Cells[i], GetEnemyColor(enemySnake, i));
            }
        }

        var isGhost = gameState.GhostTicksRemaining > 0 || gameState.GhostSkinActive;
        var playerParts = gameState.PlayerSnake.SnakeBodyParts;
        for (int i = 0; i < playerParts.Count; i++)
        {
            (int R, int G, int B) color;
            if (isGhost)
            {
                color = GetGhostColor(i);
            }
            else if (gameState.RainbowSkinActive)
            {
                // The whole body pulses through the same hue together, instead of the subtle
                // per-segment gradient every snake already has - a faster, showier cycle.
                color = GradientWriter.GetRainbowColor(hueOffset * 4);
            }
            else
            {
                color = GradientWriter.GetRainbowColor(hueOffset + i * 12);
            }
            DrawItem(playerParts[i], color);
        }

        if (gameState.GuestSnake != null && gameState.GuestAlive)
        {
            var guestParts = gameState.GuestSnake.SnakeBodyParts;
            for (int i = 0; i < guestParts.Count; i++)
            {
                DrawItem(guestParts[i], GradientWriter.GetRainbowColor(hueOffset + 180 + i * 12));
            }
        }

        foreach (var food in gameState.Foods)
        {
            DrawItem(food, GetFoodColor(food, hueOffset));
        }

        if (gameState.BirdLocation != null)
        {
            var isBlinkFrame = Environment.TickCount / 200 % 2 == 0;
            DrawCell(gameState.BirdLocation, 'V', isBlinkFrame ? (255, 240, 80) : (255, 255, 255), ConsoleColor.Yellow);
        }
    }

    private static (int R, int G, int B) GetFoodColor(FoodInfo food, int hueOffset)
    {
        if (food.Type == FoodType.Rainbow)
        {
            return GradientWriter.GetRainbowColor(hueOffset * 3);
        }

        var (r, g, b) = food.Type switch
        {
            FoodType.Gold => (255, 200, 40),
            FoodType.Purple => (190, 60, 255),
            FoodType.Blue => (70, 130, 255),
            _ => (255, 60, 40)
        };
        var pulse = 0.8 + 0.2 * Math.Sin(Environment.TickCount / 120.0);
        return ((int)(r * pulse), (int)(g * pulse), (int)(b * pulse));
    }

    // The border wall is built from stone-gray blocks with a per-cell shade variance.
    public static void DrawBorder()
    {
        var width = Settings.Current.MapWidth + 2 * Constants.FieldOffsetX;
        var height = Settings.Current.MapHeight + 2 * Constants.FieldOffsetY;

        for (int x = 0; x < width; x++)
        {
            DrawWallCell(x, 0);
            DrawWallCell(x, height - 1);
        }
        for (int y = 1; y < height - 1; y++)
        {
            DrawWallCell(0, y);
            DrawWallCell(width - 1, y);
        }
    }

    private static void DrawWallCell(int x, int y)
    {
        Console.SetCursorPosition(x, y);
        if (VirtualTerminal.IsEnabled)
        {
            var shade = Random.Shared.Next(100, 150);
            Console.Write(GradientWriter.Colorize('█', (shade, shade, shade)));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write('█');
        }
    }

    public static void DrawStatusLine(string status)
    {
        Console.SetCursorPosition(0, Constants.StatusLineY);
        var width = Settings.Current.MapWidth + Constants.FieldOffsetX;
        var paddedStatus = status.Length > width ? status[..width] : status.PadRight(width);
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

    // Each enemy wears its personality color: brightest at the head, fading towards
    // the tail, with a slow pulse running along the body.
    private static (int R, int G, int B) GetEnemyColor(ObstacleSnake enemySnake, int cellIndex)
    {
        var (r, g, b) = enemySnake.Personality.BodyColor;
        var fade = 1.0 - 0.4 * cellIndex / Math.Max(1, enemySnake.Cells.Count - 1);
        var pulse = 0.85 + 0.15 * Math.Sin(Environment.TickCount / 150.0 + cellIndex * 0.8);
        var factor = fade * pulse;
        return (
            (int)Math.Clamp(r * factor, 0, 255),
            (int)Math.Clamp(g * factor, 0, 255),
            (int)Math.Clamp(b * factor, 0, 255));
    }

    // A phased-out player shimmers in pale blue-white instead of the rainbow.
    private static (int R, int G, int B) GetGhostColor(int cellIndex)
    {
        var shade = (int)Math.Clamp(190 + 50 * Math.Sin(Environment.TickCount / 80.0 + cellIndex * 0.5), 130, 255);
        return (shade, shade, 255);
    }

    private static void DrawCell(Location location, char displayChar, (int R, int G, int B) color, ConsoleColor fallbackColor)
    {
        Console.SetCursorPosition(location.X + Constants.FieldOffsetX, location.Y + Constants.FieldOffsetY);
        if (VirtualTerminal.IsEnabled)
        {
            Console.Write(GradientWriter.Colorize(displayChar, color));
        }
        else
        {
            Console.ForegroundColor = fallbackColor;
            Console.Write(displayChar);
        }
    }

    private static void DrawItem(ElementInfo elementInfo, (int R, int G, int B)? rainbowColor = null)
    {
        if (elementInfo != null)
        {
            Console.SetCursorPosition(elementInfo.Location.X + Constants.FieldOffsetX, elementInfo.Location.Y + Constants.FieldOffsetY);
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
