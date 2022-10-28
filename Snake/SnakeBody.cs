namespace Snake
{
    internal class SnakeBody
    {
        public List<SnakePartInfo> SnakeParts { get; set; } = new()
        {
            new SnakePartInfo(new Location(Constants.HalfOfMaxX, Constants.HalfOfMaxY), ConsoleColor.DarkGreen),
            new SnakePartInfo(new Location(Constants.HalfOfMaxX, Constants.HalfOfMaxY + 1), ConsoleColor.Blue)
        };

        public Direction Direction { get; set; } = Direction.Up;

        public Location Head => SnakeParts.First().Location;

        public void Move() 
        {
            for (int i = SnakeParts.Count - 1; i > 0; i--)
            {
                SnakeParts[i].Location.X = SnakeParts[i - 1].Location.X;
                SnakeParts[i].Location.Y = SnakeParts[i - 1].Location.Y;
            }
            switch (Direction)
            {
                case Direction.Up:
                    Head.Y -= 1;
                    break;
                case Direction.Down:
                    Head.Y += 1;
                    break;
                case Direction.Left:
                    Head.X -= 1;
                    break;
                case Direction.Right:
                    Head.X += 1;
                    break;
            }
        }
    }
}