using System.Diagnostics;
using SnakeGameEngine.Moving;

namespace SnakeGameEngine.Actors;

[DebuggerDisplay("Location: {Location.X}, {Location.Y}, Color: {Color}, DisplayChar: {DisplayChar}")]
public class ElementInfo
{
    public Location Location { get; set; }

    public ConsoleColor Color { get; }

    public virtual string DisplayChar { get; }

    public ElementInfo(Location location, ConsoleColor color)
    {
        Location = location;
        Color = color;
    }
}
