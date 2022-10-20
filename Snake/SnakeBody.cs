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
            for (int i = 0; i < SnakeParts.Count; i++)
            {
                switch (Direction)
                {
                    case Direction.Up:
                        SnakeParts[i].Location = new Location(SnakeParts[i].Location.X, SnakeParts[i].Location.Y - 1);
                        break;
                    case Direction.Down:
                        SnakeParts[i].Location = new Location(SnakeParts[i].Location.X, SnakeParts[i].Location.Y + 1);
                        break;
                    case Direction.Left:
                        SnakeParts[i].Location = new Location(SnakeParts[i].Location.X - 1, SnakeParts[i].Location.Y);
                        break;
                    case Direction.Right:
                        SnakeParts[i].Location = new Location(SnakeParts[i].Location.X + 1, SnakeParts[i].Location.Y);
                        break;
                }
            }
        }
    }
}