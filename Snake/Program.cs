using SnakeGameEngine.ConsoleUtils;
using System.Diagnostics;
using System.Text;

namespace SnakeGameEngine
{
    class Program
    {
        public static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.CursorVisible = false;
            VirtualTerminal.TryEnable();
            Settings.Load();
            EnsureConsoleIsLargeEnoughForTheMenu();

            try
            {
                ConsoleKeyInfo consoleKeyInfo;
                do
                {
                    GameMenu.Show();
                    consoleKeyInfo = GameMenu.Choose();
                }
                while (consoleKeyInfo.Key != ConsoleKey.Escape);
            }
            catch (Exception exception)
            {
                OfferBugReport(exception);
            }
        }

        // The animated menu is taller than a default console window; if it doesn't fit, writing
        // it scrolls the buffer every single frame, making the whole menu drift upward endlessly.
        // Grow the window up front so that never happens.
        private static void EnsureConsoleIsLargeEnoughForTheMenu()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            try
            {
                var width = Math.Min(Console.LargestWindowWidth, Math.Max(GameMenu.RequiredWidth, Console.WindowWidth));
                var height = Math.Min(Console.LargestWindowHeight, Math.Max(GameMenu.RequiredHeight, Console.WindowHeight));

                if (Console.BufferWidth < width || Console.BufferHeight < height)
                {
                    Console.SetBufferSize(Math.Max(width, Console.BufferWidth), Math.Max(height, Console.BufferHeight));
                }
                if (Console.WindowWidth < width || Console.WindowHeight < height)
                {
                    Console.SetWindowSize(width, height);
                }
            }
            catch (Exception exception) when (exception is IOException or ArgumentOutOfRangeException or PlatformNotSupportedException)
            {
                // Some terminals (redirected output, Windows Terminal in certain modes) refuse resize
                // requests; GameMenu falls back to a static, non-scrolling layout when that happens.
            }
        }

        private static void OfferBugReport(Exception exception)
        {
            Console.CursorVisible = true;
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Something went wrong:");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(exception.ToString());
            Console.WriteLine();
            Console.WriteLine("Press B to send a bug report e-mail, any other key to exit...");
            if (Console.ReadKey(true).Key != ConsoleKey.B)
            {
                return;
            }

            // mailto URLs have tight length limits, so the stack trace is truncated.
            var body = exception.ToString();
            if (body.Length > 1500)
            {
                body = body[..1500];
            }
            var mailto = "mailto:mortens.4444@gmail.com"
                + "?subject=" + Uri.EscapeDataString("Snake Reloaded bug report")
                + "&body=" + Uri.EscapeDataString(body);
            Process.Start(new ProcessStartInfo(mailto) { UseShellExecute = true });
        }
    }
}
