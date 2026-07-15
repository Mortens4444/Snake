namespace SnakeGameEngine.Maui;

// Renders the Core game state onto a MAUI GraphicsView canvas.
public class GameDrawable : IDrawable
{
    public GameState? GameState { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Color.FromRgb(6, 20, 10);
        canvas.FillRectangle(dirtyRect);

        var gameState = GameState;
        if (gameState == null)
        {
            return;
        }

        var mapWidth = Settings.Current.MapWidth;
        var mapHeight = Settings.Current.MapHeight;
        var cellSize = Math.Min(dirtyRect.Width / mapWidth, dirtyRect.Height / mapHeight);
        var offsetX = dirtyRect.X + (dirtyRect.Width - cellSize * mapWidth) / 2;
        var offsetY = dirtyRect.Y + (dirtyRect.Height - cellSize * mapHeight) / 2;

        RectF CellRect(int x, int y) => new(offsetX + x * cellSize, offsetY + y * cellSize, cellSize, cellSize);

        // Scenery (dimmed so the actors stand out)
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                var cell = gameState.Background.GetCellAt(x, y);
                if (cell == null)
                {
                    continue;
                }
                var (r, g, b) = cell.Value.Color;
                canvas.FillColor = Color.FromRgb((int)(r * 0.55), (int)(g * 0.55), (int)(b * 0.55));
                var rect = CellRect(x, y);
                canvas.FillRectangle(rect);

                // Canopy is a lethal obstacle (unless Woodpecker is owned) - outline it as a warning.
                if (gameState.Background.IsCanopyAt(x, y))
                {
                    canvas.StrokeColor = Color.FromRgba(255, 90, 60, 130);
                    canvas.StrokeSize = 1;
                    canvas.DrawRectangle(rect);
                }
            }
        }

        // Border wall
        canvas.StrokeColor = Colors.Gray;
        canvas.StrokeSize = Math.Max(2, cellSize / 3);
        canvas.DrawRectangle(offsetX, offsetY, cellSize * mapWidth, cellSize * mapHeight);

        foreach (var cloud in gameState.PoisonClouds)
        {
            canvas.FillColor = Color.FromRgba(110, 220, 50, 160);
            canvas.FillRectangle(CellRect(cloud.Location.X, cloud.Location.Y));
        }

        foreach (var enemySnake in gameState.EnemySnakes)
        {
            var (r, g, b) = enemySnake.Personality.BodyColor;
            for (int i = 0; i < enemySnake.Cells.Count; i++)
            {
                var fade = 1.0 - 0.4 * i / Math.Max(1, enemySnake.Cells.Count - 1);
                canvas.FillColor = Color.FromRgb((int)(r * fade), (int)(g * fade), (int)(b * fade));
                var cell = enemySnake.Cells[i];
                canvas.FillRoundedRectangle(CellRect(cell.Location.X, cell.Location.Y), cellSize / 4);
            }
        }

        var isGhost = gameState.GhostTicksRemaining > 0;
        var bodyParts = gameState.PlayerSnake.SnakeBodyParts;
        for (int i = 0; i < bodyParts.Count; i++)
        {
            var (r, g, b) = isGhost ? (200, 200, 255) : RainbowColor.Get(Environment.TickCount / 10 + i * 12);
            canvas.FillColor = Color.FromRgb(r, g, b);
            var rect = CellRect(bodyParts[i].Location.X, bodyParts[i].Location.Y);
            if (i == 0)
            {
                canvas.FillEllipse(rect);
            }
            else
            {
                canvas.FillRoundedRectangle(rect, cellSize / 3);
            }
        }

        foreach (var food in gameState.Foods)
        {
            var (r, g, b) = food.Type switch
            {
                Actors.FoodType.Gold => (255, 200, 40),
                Actors.FoodType.Purple => (190, 60, 255),
                Actors.FoodType.Blue => (70, 130, 255),
                Actors.FoodType.Rainbow => RainbowColor.Get(Environment.TickCount / 3),
                _ => (255, 60, 40)
            };
            canvas.FillColor = Color.FromRgb(r, g, b);
            canvas.FillEllipse(CellRect(food.Location.X, food.Location.Y));
        }

        if (gameState.BirdLocation != null)
        {
            canvas.FillColor = Colors.Yellow;
            var rect = CellRect(gameState.BirdLocation.X, gameState.BirdLocation.Y);
            var path = new PathF();
            path.MoveTo(rect.Left, rect.Top);
            path.LineTo(rect.Right, rect.Top);
            path.LineTo(rect.Center.X, rect.Bottom);
            path.Close();
            canvas.FillPath(path);
        }
    }
}
