﻿namespace Snake
{
    public class SnakePartInfo
    {
        public Location Location { get; set; }

        public ConsoleColor Color { get; }

        public char DisplayChar => 'X';

        public SnakePartInfo(Location location, ConsoleColor color)
        {
            Location = location;
            Color = color;
        }
    }
}
