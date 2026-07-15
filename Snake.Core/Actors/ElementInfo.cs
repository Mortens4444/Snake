using System.Diagnostics;
using SnakeGameEngine.Moving;

namespace SnakeGameEngine.Actors;

[DebuggerDisplay("Location: {Location.X}, {Location.Y}, Color: {Color}, DisplayChar: {DisplayChar}")]
public abstract class ElementInfo
{
    public Location Location { get; set; }

    public abstract ConsoleColor Color { get; }

    public abstract char DisplayChar { get; }

    public ElementInfo(Location location)
    {
        Location = location;
    }
}
