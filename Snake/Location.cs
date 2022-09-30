namespace Snake
{
    internal class Location : IEquatable<Location>
    {
        public Location(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public bool Equals(Location? other)
        {
            if (other == null)
            {
                return false;
            }

            return X == other.X && Y == other.Y;
        }
    }
}
