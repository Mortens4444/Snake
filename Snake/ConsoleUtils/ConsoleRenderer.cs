using SnakeGameEngine.Moving;
using System.Text;

namespace SnakeGameEngine.ConsoleUtils;

public class ConsoleRenderer : IRenderer
{
    private string? lastStatusLine;

    public void BeginGame(GameState gameState)
    {
        Console.Clear();
        ConsoleDrawer.DrawBorder();
        BackgroundRenderer.Draw(gameState.Background);
        lastStatusLine = null;
    }

    public void DrawFrame(GameState gameState)
    {
        foreach (var location in gameState.VacatedLocations)
        {
            BackgroundRenderer.RestoreLocation(gameState.Background, location);
        }

        BackgroundRenderer.Animate(gameState.Background, gameState.TickNumber);
        ConsoleDrawer.DrawScreen(gameState);

        var statusLine = BuildStatusLine(gameState);
        if (VirtualTerminal.IsEnabled || statusLine != lastStatusLine)
        {
            ConsoleDrawer.DrawStatusLine(statusLine);
            lastStatusLine = statusLine;
        }
    }

    public void ShowPaused()
    {
        ConsoleDrawer.DrawStatusLine("PAUSED - Press P to resume");
        lastStatusLine = null;
    }

    // Slow-motion playback of the recorded final moments.
    public void PlayReplay(GameState gameState)
    {
        Console.Clear();
        ConsoleDrawer.DrawBorder();
        BackgroundRenderer.Draw(gameState.Background);
        ConsoleDrawer.DrawStatusLine("REPLAY - your final moments in slow motion (any key skips)");

        List<ReplayCell>? previousFrame = null;
        foreach (var frame in gameState.ReplayFrames)
        {
            if (Console.KeyAvailable)
            {
                Console.ReadKey(true);
                break;
            }

            if (previousFrame != null)
            {
                var currentCells = frame.Select(cell => (cell.X, cell.Y)).ToHashSet();
                foreach (var cell in previousFrame)
                {
                    if (!currentCells.Contains((cell.X, cell.Y)))
                    {
                        BackgroundRenderer.RestoreLocation(gameState.Background, new Location(cell.X, cell.Y));
                    }
                }
            }

            foreach (var cell in frame)
            {
                Console.SetCursorPosition(cell.X + Constants.FieldOffsetX, cell.Y + Constants.FieldOffsetY);
                if (VirtualTerminal.IsEnabled)
                {
                    Console.Write(GradientWriter.Colorize(cell.DisplayChar, cell.Color));
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(cell.DisplayChar);
                }
            }

            previousFrame = frame;
            Thread.Sleep(200);
        }

        ConsoleDrawer.DrawStatusLine("Replay over - press any key to return to the menu...");
        Console.ReadKey(true);
    }

    private static string BuildStatusLine(GameState gameState)
    {
        var statusLine = new StringBuilder();
        statusLine.Append($"Length: {gameState.PlayerSnake.SnakeBodyParts.Count}/{Settings.Current.TargetSnakeLength}");
        statusLine.Append($"  Level: {gameState.Level} ({gameState.LevelPoints}/{Settings.Current.PointsPerLevel})");
        statusLine.Append($"  Enemies: {gameState.EnemySnakes.Count}");
        statusLine.Append($"  Time: {gameState.Elapsed:mm\\:ss}");

        if (gameState.ShieldCharges > 0)
        {
            statusLine.Append($"  Shield x{gameState.ShieldCharges}");
        }

        foreach (var perk in gameState.PlayerPerks.Where(perk => perk.ActivationKey != null))
        {
            var cooldown = perk.IsReady ? "" : $"({perk.CooldownRemaining / 10 + 1}s)";
            statusLine.Append($"  {perk.ActivationKey}-{perk.Name}{cooldown}");
        }

        statusLine.Append("  P-Pause  ESC-Quit");
        return statusLine.ToString();
    }

}
