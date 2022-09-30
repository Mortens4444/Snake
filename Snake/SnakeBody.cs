namespace Snake
{
    internal class SnakeBody
    {
        public List<(Location, ConsoleColor)> SnakeParts { get; set; } = new()
        {
            (new Location(Constants.HalfOfMaxX, Constants.HalfOfMaxY), ConsoleColor.DarkGreen),
            (new Location(Constants.HalfOfMaxX, Constants.HalfOfMaxY + 1), ConsoleColor.Blue)
        };

        public Direction Direction { get; set; } = Direction.Up;

        public Location Head => SnakeParts.First().Item1;
    }
}
 