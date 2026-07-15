using SnakeGameEngine.Perks;

namespace SnakeGameEngine.Maui;

public partial class MainPage : ContentPage
{
    private readonly GameDrawable drawable = new();
    private GameState? gameState;
    private IDispatcherTimer? timer;
    private GameAction pendingAction = GameAction.None;
    private ConsoleKey pendingPerkKey;
    private bool isDialogOpen;

    public MainPage()
    {
        InitializeComponent();
        GameView.Drawable = drawable;

        AddSwipeGesture(SwipeDirection.Up, GameAction.MoveUp);
        AddSwipeGesture(SwipeDirection.Down, GameAction.MoveDown);
        AddSwipeGesture(SwipeDirection.Left, GameAction.MoveLeft);
        AddSwipeGesture(SwipeDirection.Right, GameAction.MoveRight);

        LoadSettingsIntoControls();
    }

    private void AddSwipeGesture(SwipeDirection direction, GameAction action)
    {
        var swipe = new SwipeGestureRecognizer { Direction = direction, Threshold = 40 };
        swipe.Swiped += (_, _) => pendingAction = action;
        RootGrid.GestureRecognizers.Add(swipe);
    }

    private void OnNewGameClicked(object? sender, EventArgs e)
    {
        StartOverlay.IsVisible = false;
        gameState = new GameState();
        drawable.GameState = gameState;
        pendingAction = GameAction.None;
        pendingPerkKey = default;
        PerkBar.IsVisible = false;

        timer?.Stop();
        timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(Settings.Current.InitialTickMilliseconds);
        timer.Tick += OnGameTick;
        timer.Start();
    }

    private async void OnGameTick(object? sender, EventArgs e)
    {
        if (gameState == null || isDialogOpen)
        {
            return;
        }

        gameState.Tick(pendingAction, pendingPerkKey);
        pendingAction = GameAction.None;
        pendingPerkKey = default;
        PlayFeedbackFor(gameState.SoundEvents);
        gameState.SoundEvents.Clear();
        GameView.Invalidate();
        UpdateStatusLabel();
        UpdatePerkBar();

        if (timer != null)
        {
            timer.Interval = TimeSpan.FromMilliseconds(gameState.GetTickMilliseconds());
        }

        if (gameState.Status == GameStatus.Running && gameState.PendingPerkChoice)
        {
            gameState.PendingPerkChoice = false;
            await ShowPerkChoiceAsync();
            return;
        }

        if (gameState.Status != GameStatus.Running)
        {
            await ShowGameEndAsync();
        }
    }

    private void UpdateStatusLabel()
    {
        if (gameState == null)
        {
            return;
        }
        StatusLabel.Text = $"Length {gameState.PlayerSnake.SnakeBodyParts.Count}/{Settings.Current.TargetSnakeLength}"
            + $"   Level {gameState.Level} ({gameState.LevelPoints}/{Settings.Current.PointsPerLevel})"
            + $"   Enemies {gameState.EnemySnakes.Count}"
            + (gameState.ShieldCharges > 0 ? $"   Shield x{gameState.ShieldCharges}" : "");
    }

    // Tapping an active perk's button raises it the same way a keypress would in the console client.
    private void UpdatePerkBar()
    {
        if (gameState == null)
        {
            return;
        }

        var activePerks = gameState.PlayerPerks.Where(perk => perk.ActivationKey != null).ToList();
        PerkBar.IsVisible = activePerks.Count > 0;
        if (activePerks.Count == 0)
        {
            return;
        }

        PerkBar.ItemsSource = activePerks.Select(perk => new PerkButtonViewModel
        {
            Name = perk.Name,
            IsReady = perk.IsReady,
            Label = perk.IsReady ? perk.Name : $"{perk.Name} ({perk.CooldownRemaining / 10 + 1}s)"
        }).ToList();
    }

    private void OnPerkButtonClicked(object? sender, EventArgs e)
    {
        if (gameState == null || sender is not Button { CommandParameter: string perkName })
        {
            return;
        }

        var perk = gameState.PlayerPerks.FirstOrDefault(perk => perk.Name == perkName);
        if (perk?.ActivationKey != null)
        {
            // Tick() only recognizes the perk's own ActivationKey, so replay it for one tick.
            pendingPerkKey = perk.ActivationKey.Value;
        }
    }

    // Console.Beep has no mobile equivalent, so haptic pulses stand in for sound feedback;
    // reuses the shared SoundEnabled setting since both are "give me feedback" toggles.
    private void PlayFeedbackFor(List<SoundEvent> soundEvents)
    {
        if (!Settings.Current.SoundEnabled || soundEvents.Count == 0)
        {
            return;
        }

        var strongEvents = new[] { SoundEvent.PlayerDied, SoundEvent.Win, SoundEvent.PerkGained, SoundEvent.BirdCaught };
        var type = soundEvents.Any(strongEvents.Contains) ? HapticFeedbackType.LongPress : HapticFeedbackType.Click;
        try
        {
            HapticFeedback.Default.Perform(type);
        }
        catch (FeatureNotSupportedException)
        {
        }
    }

    private async Task ShowPerkChoiceAsync()
    {
        if (gameState == null)
        {
            return;
        }

        isDialogOpen = true;
        gameState.Pause();
        var choices = PerkFactory.GetRandomChoices(gameState.PlayerPerks, Settings.Current.PerkChoicesPerLevel);
        if (choices.Count > 0)
        {
            var pick = await DisplayActionSheetAsync($"Level {gameState.Level} - choose a perk!", "Skip", null,
                choices.Select(perk => $"{perk.Name} - {perk.Description}").ToArray());
            var chosenPerk = choices.FirstOrDefault(perk => pick != null && pick.StartsWith(perk.Name));
            if (chosenPerk != null)
            {
                gameState.PlayerPerks.Add(chosenPerk);
            }
        }
        gameState.Resume();
        isDialogOpen = false;
    }

    private async Task ShowGameEndAsync()
    {
        if (gameState == null)
        {
            return;
        }

        timer?.Stop();
        isDialogOpen = true;
        var message = gameState.Status == GameStatus.Won
            ? $"You won! Score: {gameState.Score}"
            : $"Game over! {gameState.PlayerKilledBy ?? "The wall"} got you at length {gameState.PlayerSnake.SnakeBodyParts.Count}.";
        await DisplayAlertAsync("Snake Reloaded", message, "Back to menu");
        isDialogOpen = false;
        gameState = null;
        drawable.GameState = null;
        GameView.Invalidate();
        StartOverlay.IsVisible = true;
        PerkBar.IsVisible = false;
        StatusLabel.Text = "SNAKE RELOADED";
    }

    private void OnUpClicked(object? sender, EventArgs e) => pendingAction = GameAction.MoveUp;

    private void OnDownClicked(object? sender, EventArgs e) => pendingAction = GameAction.MoveDown;

    private void OnLeftClicked(object? sender, EventArgs e) => pendingAction = GameAction.MoveLeft;

    private void OnRightClicked(object? sender, EventArgs e) => pendingAction = GameAction.MoveRight;

    // Settings overlay

    private void OnSettingsClicked(object? sender, EventArgs e)
    {
        LoadSettingsIntoControls();
        SettingsOverlay.IsVisible = true;
    }

    private void OnCloseSettingsClicked(object? sender, EventArgs e)
    {
        SettingsOverlay.IsVisible = false;
    }

    private void LoadSettingsIntoControls()
    {
        var settings = Settings.Current;
        SoundSwitch.IsToggled = settings.SoundEnabled;
        LosePerksSwitch.IsToggled = settings.LosePerksOnDeath;
        DifficultySlider.Value = settings.EnemyDifficulty;
        TargetLengthSlider.Value = settings.TargetSnakeLength;
        BirdSlider.Value = settings.BirdIntervalMinutes;
        UpdateDifficultyLabel();
        UpdateTargetLengthLabel();
        UpdateBirdLabel();
    }

    private void OnSoundToggled(object? sender, ToggledEventArgs e)
    {
        Settings.Current.SoundEnabled = e.Value;
        Settings.Current.Save();
    }

    private void OnLosePerksToggled(object? sender, ToggledEventArgs e)
    {
        Settings.Current.LosePerksOnDeath = e.Value;
        Settings.Current.Save();
    }

    private void OnDifficultyChanged(object? sender, ValueChangedEventArgs e)
    {
        Settings.Current.EnemyDifficulty = (int)Math.Round(e.NewValue);
        Settings.Current.Save();
        UpdateDifficultyLabel();
    }

    private void OnTargetLengthChanged(object? sender, ValueChangedEventArgs e)
    {
        Settings.Current.TargetSnakeLength = (int)Math.Round(e.NewValue);
        Settings.Current.Save();
        UpdateTargetLengthLabel();
    }

    private void OnBirdIntervalChanged(object? sender, ValueChangedEventArgs e)
    {
        Settings.Current.BirdIntervalMinutes = (int)Math.Round(e.NewValue);
        Settings.Current.Save();
        UpdateBirdLabel();
    }

    private void UpdateDifficultyLabel()
    {
        var index = Math.Clamp(Settings.Current.EnemyDifficulty, 0, AI.BrainFactory.DifficultyNames.Length - 1);
        DifficultyLabel.Text = $"Enemy difficulty: {AI.BrainFactory.DifficultyNames[index]}";
    }

    private void UpdateTargetLengthLabel()
    {
        TargetLengthLabel.Text = $"Target snake length: {Settings.Current.TargetSnakeLength}";
    }

    private void UpdateBirdLabel()
    {
        BirdLabel.Text = Settings.Current.BirdIntervalMinutes == 0
            ? "Bird: never"
            : $"Bird every {Settings.Current.BirdIntervalMinutes} minutes";
    }

    private async void OnResetProgressClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync("Reset progress", "Delete all earned perks and enemy profiles?", "Reset", "Cancel");
        if (confirmed)
        {
            PlayerProgress.Reset();
            EnemyProfileStore.Reset();
        }
    }
}
