namespace SnakeGameEngine;

// Pure color math shared by every client.
public static class RainbowColor
{
    public static (int R, int G, int B) Get(int hue)
    {
        hue = (hue % 360 + 360) % 360;
        var risingChannel = (int)Math.Round(255 * (hue % 60) / 60.0);
        var fallingChannel = 255 - risingChannel;

        return (hue / 60) switch
        {
            0 => (255, risingChannel, 0),
            1 => (fallingChannel, 255, 0),
            2 => (0, 255, risingChannel),
            3 => (0, fallingChannel, 255),
            4 => (risingChannel, 0, 255),
            _ => (255, 0, fallingChannel)
        };
    }
}
