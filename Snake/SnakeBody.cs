namespace Snake
{
    internal class SnakeBody
    {
        public List<Location> SnakeParts { get; set; } = new List<Location>
        {
            new Location(Constants.HalfOfMaxX, Constants.HalfOfMaxY),
            new Location(Constants.HalfOfMaxX, Constants.HalfOfMaxY + 1)
        };

        public Direction Direction { get; set; } = Direction.Up;
    }
}
 