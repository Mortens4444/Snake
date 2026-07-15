using Microsoft.Extensions.Logging;

namespace SnakeGameEngine.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Relative save files (settings, profiles, progress) must land in a writable folder on mobile.
        Directory.SetCurrentDirectory(FileSystem.Current.AppDataDirectory);
        Settings.Load();

        // Console cells are tall and thin, canvas cells are square - pick a map shape that fits the screen.
        if (DeviceInfo.Current.Idiom == DeviceIdiom.Phone)
        {
            Settings.Current.MapWidth = 30;
            Settings.Current.MapHeight = 44;
        }
        else
        {
            Settings.Current.MapWidth = 60;
            Settings.Current.MapHeight = 34;
        }

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
