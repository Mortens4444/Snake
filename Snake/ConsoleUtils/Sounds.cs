namespace SnakeGameEngine.ConsoleUtils;

public static class Sounds
{
    // Console.Beep blocks for its duration, so tunes play on a background thread;
    // the lock keeps overlapping events from interleaving their notes.
    private static readonly object BeepLock = new();

    public static void Play(SoundEvent soundEvent)
    {
        if (!Settings.Current.SoundEnabled)
        {
            return;
        }

        var notes = GetNotes(soundEvent);
        Task.Run(() =>
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }
            lock (BeepLock)
            {
                foreach (var (frequency, duration) in notes)
                {
                    Console.Beep(frequency, duration);
                }
            }
        });
    }

    private static (int Frequency, int Duration)[] GetNotes(SoundEvent soundEvent)
    {
        return soundEvent switch
        {
            SoundEvent.FoodEaten => new[] { (880, 40) },
            SoundEvent.LuckyFood => new[] { (988, 50), (1319, 70) },
            SoundEvent.PerkGained => new[] { (659, 70), (880, 70), (1109, 110) },
            SoundEvent.ShieldAbsorbed => new[] { (494, 60), (494, 60) },
            SoundEvent.EnemyDied => new[] { (220, 90) },
            SoundEvent.PlayerDied => new[] { (392, 130), (294, 130), (196, 240) },
            SoundEvent.Win => new[] { (659, 110), (880, 110), (1109, 110), (1319, 240) },
            SoundEvent.BirdChirp => new[] { (1568, 45), (1976, 40) },
            SoundEvent.BirdCaught => new[] { (1319, 60), (1568, 60), (1976, 100) },
            _ => Array.Empty<(int, int)>()
        };
    }
}
