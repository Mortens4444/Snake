using System.Runtime.InteropServices;

namespace SnakeGameEngine.ConsoleUtils;

public static class VirtualTerminal
{
    private const int StdOutputHandle = -11;
    private const uint EnableVirtualTerminalProcessing = 0x0004;

    public static bool IsEnabled { get; private set; }

    // ANSI true color codes only work if the console has virtual terminal processing enabled;
    // on Windows this is opt-in, elsewhere it is the default.
    public static void TryEnable()
    {
        if (!OperatingSystem.IsWindows())
        {
            IsEnabled = true;
            return;
        }

        var handle = GetStdHandle(StdOutputHandle);
        if (GetConsoleMode(handle, out var mode) && SetConsoleMode(handle, mode | EnableVirtualTerminalProcessing))
        {
            IsEnabled = true;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
}
