using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Hosting;
using SnakeGameEngine.Maui.Multiplayer;
using SnakeGameEngine.Multiplayer;
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

    // Bluetooth LAN co-op - see Snake.Maui/Multiplayer. Host and guest are mutually exclusive
    // with each other and with a local single-player gameState.
    private static IBleManager BleManager => IPlatformApplication.Current!.Services.GetRequiredService<IBleManager>();

    private static IBleHostingManager BleHostingManager => IPlatformApplication.Current!.Services.GetRequiredService<IBleHostingManager>();

    private readonly ObservableCollection<ScannedDeviceViewModel> scanResults = new();
    private IDisposable? scanSubscription;
    private CancellationTokenSource? bluetoothWaitCancellation;

    private BluetoothHost? bluetoothHost;
    private bool isHostPerkDialogShowing;

    private BluetoothClient? bluetoothClient;
    private GuestSnapshotDrawable? guestSnapshotDrawable;
    private bool isGuestPerkDialogShowing;

    public MainPage()
    {
        InitializeComponent();
        GameView.Drawable = drawable;
        ScanResultsView.ItemsSource = scanResults;

        AddSwipeGesture(SwipeDirection.Up, GameAction.MoveUp);
        AddSwipeGesture(SwipeDirection.Down, GameAction.MoveDown);
        AddSwipeGesture(SwipeDirection.Left, GameAction.MoveLeft);
        AddSwipeGesture(SwipeDirection.Right, GameAction.MoveRight);

        LoadSettingsIntoControls();
    }

    private void AddSwipeGesture(SwipeDirection direction, GameAction action)
    {
        var swipe = new SwipeGestureRecognizer { Direction = direction, Threshold = 40 };
        swipe.Swiped += (_, _) => HandleLocalAction(action);
        RootGrid.GestureRecognizers.Add(swipe);
    }

    // A Bluetooth guest has no local gameState to apply movement to - its input goes straight
    // over the wire instead. Host and single-player both still just set pendingAction, read by
    // OnGameTick/OnHostGameTick on the next timer tick, exactly as before.
    private void HandleLocalAction(GameAction action)
    {
        if (bluetoothClient != null)
        {
            _ = bluetoothClient.SendInputAsync(action);
        }
        else
        {
            pendingAction = action;
        }
    }

    private void OnNewGameClicked(object? sender, EventArgs e)
    {
        StartOverlay.IsVisible = false;
        GameView.Drawable = drawable;
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

        var progress = PlayerProgress.Load();
        progress.StartingLength = gameState.Status == GameStatus.GameOver && Settings.Current.LoseLengthOnDeath
            ? 2
            : gameState.PlayerSnake.SnakeBodyParts.Count;
        progress.Save();

        await DisplayAlertAsync("Snake Reloaded", message, "Back to menu");
        isDialogOpen = false;
        gameState = null;
        drawable.GameState = null;
        GameView.Invalidate();
        StartOverlay.IsVisible = true;
        PerkBar.IsVisible = false;
        StatusLabel.Text = "SNAKE RELOADED";
    }

    private void OnUpClicked(object? sender, EventArgs e) => HandleLocalAction(GameAction.MoveUp);

    private void OnDownClicked(object? sender, EventArgs e) => HandleLocalAction(GameAction.MoveDown);

    private void OnLeftClicked(object? sender, EventArgs e) => HandleLocalAction(GameAction.MoveLeft);

    private void OnRightClicked(object? sender, EventArgs e) => HandleLocalAction(GameAction.MoveRight);

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
        LoseLengthSwitch.IsToggled = settings.LoseLengthOnDeath;
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

    private void OnLoseLengthToggled(object? sender, ToggledEventArgs e)
    {
        Settings.Current.LoseLengthOnDeath = e.Value;
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

    // ===== Bluetooth: host role =====
    // Not tested on real Bluetooth hardware - see README.md/ROADMAP.md. Compile-verified only.

    private async void OnHostBluetoothClicked(object? sender, EventArgs e)
    {
        StartOverlay.IsVisible = false;
        BluetoothWaitOverlay.IsVisible = true;

        bluetoothHost = new BluetoothHost(BleHostingManager);
        bluetoothWaitCancellation = new CancellationTokenSource();
        try
        {
            await bluetoothHost.WaitForGuestAsync(bluetoothWaitCancellation.Token);
        }
        catch (Exception ex)
        {
            BluetoothWaitOverlay.IsVisible = false;
            StartOverlay.IsVisible = true;
            bluetoothHost.Dispose();
            bluetoothHost = null;
            if (ex is not OperationCanceledException)
            {
                await DisplayAlertAsync("Bluetooth", $"Could not start hosting: {ex.Message}", "OK");
            }
            return;
        }

        gameState = new GameState();
        gameState.EnableGuest();
        GameView.Drawable = drawable;
        drawable.GameState = gameState;
        pendingAction = GameAction.None;
        pendingPerkKey = default;
        isHostPerkDialogShowing = false;
        PerkBar.IsVisible = false;

        await bluetoothHost.SendHelloAsync(gameState);
        BluetoothWaitOverlay.IsVisible = false;

        timer?.Stop();
        timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(Settings.Current.InitialTickMilliseconds);
        timer.Tick += OnHostGameTick;
        timer.Start();
    }

    private void OnCancelBluetoothWaitClicked(object? sender, EventArgs e)
    {
        bluetoothWaitCancellation?.Cancel();
    }

    private async void OnHostGameTick(object? sender, EventArgs e)
    {
        if (gameState == null || bluetoothHost == null || isDialogOpen)
        {
            return;
        }

        gameState.PendingGuestAction = bluetoothHost.LatestGuestAction;
        gameState.PendingGuestKey = bluetoothHost.ConsumePendingGuestKey();

        var guestPerkPick = bluetoothHost.ConsumePendingGuestPerkPick();
        if (gameState.GuestChoosingPerk && guestPerkPick.HasValue)
        {
            ResolveGuestBluetoothPerkChoice(guestPerkPick.Value);
        }

        gameState.Tick(pendingAction, pendingPerkKey);
        pendingAction = GameAction.None;
        pendingPerkKey = default;

        ArmHostBluetoothPerkChoice();
        ArmGuestBluetoothPerkChoice();

        PlayFeedbackFor(gameState.SoundEvents);
        gameState.SoundEvents.Clear();
        GameView.Invalidate();
        UpdateHostBluetoothStatusLabel();
        UpdatePerkBar();

        if (timer != null)
        {
            timer.Interval = TimeSpan.FromMilliseconds(gameState.GetTickMilliseconds());
        }

        _ = bluetoothHost.SendSnapshotAsync(gameState, BuildHostBluetoothStatusText());

        // Non-blocking, unlike single-player's ShowPerkChoiceAsync: the timer must keep firing
        // (and Tick() keeps running) while this ActionSheet is up, so the guest keeps moving -
        // GameState.Tick() already freezes only the host's own slice via HostChoosingPerk.
        if (gameState.HostChoosingPerk && !isHostPerkDialogShowing)
        {
            isHostPerkDialogShowing = true;
            _ = ShowHostBluetoothPerkChoiceAsync();
        }

        if (gameState.Status != GameStatus.Running || bluetoothHost.Disconnected)
        {
            timer?.Stop();
            await ShowBluetoothHostGameEndAsync();
        }
    }

    private void UpdateHostBluetoothStatusLabel()
    {
        if (gameState == null)
        {
            return;
        }
        var guestStatus = gameState.GuestSnake == null
            ? ""
            : gameState.GuestAlive
                ? $"   Guest {gameState.GuestSnake.SnakeBodyParts.Count}"
                : "   Guest out";
        var perkStatus = gameState.GuestChoosingPerk ? "   Guest is choosing a perk..." : "";
        StatusLabel.Text = $"Length {gameState.PlayerSnake.SnakeBodyParts.Count}/{Settings.Current.TargetSnakeLength}{guestStatus}"
            + $"   Level {gameState.Level} ({gameState.LevelPoints}/{Settings.Current.PointsPerLevel}){perkStatus}";
    }

    // Sent to the guest so its screen can show why the host snake stopped moving - the guest
    // already knows about its own card since it's shown directly via DisplayActionSheetAsync.
    private string BuildHostBluetoothStatusText()
    {
        var hostPerkStatus = gameState!.HostChoosingPerk ? "   Host is choosing a perk..." : "";
        return $"Host length {gameState.PlayerSnake.SnakeBodyParts.Count}/{Settings.Current.TargetSnakeLength}{hostPerkStatus}";
    }

    // Mirrors MultiplayerEngine.ArmHostPerkChoice/ArmGuestPerkChoice/ResolveGuestPerkChoice
    // exactly - same GameState fields, same PerkFactory call, no Snake.Core changes needed.
    private void ArmHostBluetoothPerkChoice()
    {
        if (gameState!.Status == GameStatus.Running && gameState.PendingPerkChoice && !gameState.HostChoosingPerk)
        {
            gameState.PendingPerkChoice = false;
            gameState.HostChoosingPerk = true;
            gameState.PerkChoiceOptions = PerkFactory.GetRandomChoices(gameState.PlayerPerks, Settings.Current.PerkChoicesPerLevel);
        }
    }

    private void ArmGuestBluetoothPerkChoice()
    {
        if (gameState!.Status == GameStatus.Running && gameState.GuestPendingPerkChoice && !gameState.GuestChoosingPerk)
        {
            gameState.GuestPendingPerkChoice = false;
            gameState.GuestChoosingPerk = true;
            gameState.GuestPerkChoiceOptions = PerkFactory.GetRandomChoices(gameState.GuestPerks, Settings.Current.PerkChoicesPerLevel, forGuest: true);
        }
    }

    private void ResolveGuestBluetoothPerkChoice(int choiceIndex)
    {
        if (choiceIndex >= 0 && choiceIndex < gameState!.GuestPerkChoiceOptions.Count)
        {
            var perk = gameState.GuestPerkChoiceOptions[choiceIndex];
            perk.IsGuestOwned = true;
            gameState.GuestPerks.Add(perk);
        }
        gameState!.GuestPerkChoiceOptions = new List<Perk>();
        gameState.GuestChoosingPerk = false;
    }

    private async Task ShowHostBluetoothPerkChoiceAsync()
    {
        var choices = gameState!.PerkChoiceOptions;
        if (choices.Count > 0)
        {
            var pick = await DisplayActionSheetAsync($"Level {gameState.Level} - choose a perk!", "Skip", null,
                choices.Select(perk => $"{perk.Name} - {perk.Description}").ToArray());
            var chosenPerk = choices.FirstOrDefault(perk => pick != null && pick.StartsWith(perk.Name));
            if (chosenPerk != null)
            {
                gameState.PlayerPerks.Add(chosenPerk);
                var progress = PlayerProgress.Load();
                progress.PerkNames = gameState.PlayerPerks.Select(perk => perk.Name).ToList();
                progress.Save();
            }
        }
        gameState!.PerkChoiceOptions = new List<Perk>();
        gameState.HostChoosingPerk = false;
        isHostPerkDialogShowing = false;
    }

    private async Task ShowBluetoothHostGameEndAsync()
    {
        if (gameState == null)
        {
            return;
        }

        var progress = PlayerProgress.Load();
        progress.StartingLength = gameState.Status == GameStatus.GameOver && Settings.Current.LoseLengthOnDeath
            ? 2
            : gameState.PlayerSnake.SnakeBodyParts.Count;
        progress.Save();

        var endMessage = gameState.Status == GameStatus.Won
            ? $"You won! Score: {gameState.Score}"
            : gameState.PlayerKilledBy != null
                ? $"Host died ({gameState.PlayerKilledBy})."
                : bluetoothHost != null && bluetoothHost.Disconnected
                    ? "Connection to the guest was lost."
                    : "Session ended.";

        if (bluetoothHost != null && !bluetoothHost.Disconnected)
        {
            await bluetoothHost.SendSnapshotAsync(gameState, BuildHostBluetoothStatusText(), endMessage);
        }
        bluetoothHost?.Dispose();
        bluetoothHost = null;

        await DisplayAlertAsync("Snake Reloaded", endMessage, "Back to menu");

        gameState = null;
        drawable.GameState = null;
        GameView.Invalidate();
        StartOverlay.IsVisible = true;
        PerkBar.IsVisible = false;
        StatusLabel.Text = "SNAKE RELOADED";
    }

    // ===== Bluetooth: guest role =====

    private async void OnJoinBluetoothClicked(object? sender, EventArgs e)
    {
        StartOverlay.IsVisible = false;
        scanResults.Clear();
        BluetoothScanOverlay.IsVisible = true;

        try
        {
            await BleManager.RequestAccessAsync();
        }
        catch (Exception ex)
        {
            BluetoothScanOverlay.IsVisible = false;
            StartOverlay.IsVisible = true;
            await DisplayAlertAsync("Bluetooth", $"Could not access Bluetooth: {ex.Message}", "OK");
            return;
        }

        scanSubscription = BleManager.Scan(new ScanConfig(new[] { BluetoothGatt.ServiceUuid })).Subscribe(result =>
        {
            Dispatcher.Dispatch(() =>
            {
                if (scanResults.Any(device => device.Peripheral.Uuid == result.Peripheral.Uuid))
                {
                    return;
                }
                scanResults.Add(new ScannedDeviceViewModel
                {
                    Name = result.AdvertisementData.LocalName ?? "Snake Reloaded host",
                    Peripheral = result.Peripheral
                });
            });
        });
    }

    private void OnCancelBluetoothScanClicked(object? sender, EventArgs e)
    {
        scanSubscription?.Dispose();
        scanSubscription = null;
        BluetoothScanOverlay.IsVisible = false;
        StartOverlay.IsVisible = true;
    }

    private async void OnScanResultClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: ScannedDeviceViewModel device })
        {
            return;
        }

        scanSubscription?.Dispose();
        scanSubscription = null;
        BluetoothScanOverlay.IsVisible = false;

        bluetoothClient = new BluetoothClient(device.Peripheral);
        HelloMessage hello;
        try
        {
            hello = await bluetoothClient.ConnectAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            bluetoothClient.Dispose();
            bluetoothClient = null;
            StartOverlay.IsVisible = true;
            await DisplayAlertAsync("Bluetooth", $"Could not connect: {ex.Message}", "OK");
            return;
        }

        guestSnapshotDrawable = new GuestSnapshotDrawable(hello);
        GameView.Drawable = guestSnapshotDrawable;
        isGuestPerkDialogShowing = false;
        PerkBar.IsVisible = false;

        _ = RunGuestBluetoothLoopAsync();
    }

    private async Task RunGuestBluetoothLoopAsync()
    {
        var client = bluetoothClient!;
        while (!client.Disconnected)
        {
            var snapshot = await client.WaitForNextSnapshotAsync(CancellationToken.None);
            if (snapshot == null)
            {
                break;
            }

            guestSnapshotDrawable!.Snapshot = snapshot;
            GameView.Invalidate();
            StatusLabel.Text = snapshot.StatusText;

            if (snapshot.GuestPerkChoices != null && !isGuestPerkDialogShowing)
            {
                isGuestPerkDialogShowing = true;
                _ = ShowGuestBluetoothPerkChoiceAsync(snapshot.GuestPerkChoices, snapshot.GuestLevel);
            }

            if (!snapshot.IsRunning)
            {
                await DisplayAlertAsync("Snake Reloaded", snapshot.EndMessage ?? "The game has ended.", "Back to menu");
                break;
            }
        }

        if (client.Disconnected)
        {
            await DisplayAlertAsync("Snake Reloaded", "Connection to the host was lost.", "Back to menu");
        }

        bluetoothClient?.Dispose();
        bluetoothClient = null;
        guestSnapshotDrawable = null;
        GameView.Drawable = drawable;
        GameView.Invalidate();
        StartOverlay.IsVisible = true;
        PerkBar.IsVisible = false;
        StatusLabel.Text = "SNAKE RELOADED";
    }

    // The guest has no local Perk list/activation UI, on par with the console client's guest -
    // BuildSnapshot doesn't carry the guest's owned-perk list either, only the pending choice.
    private async Task ShowGuestBluetoothPerkChoiceAsync(List<PerkOptionDto> choices, int level)
    {
        var pick = await DisplayActionSheetAsync($"Level {level} - choose a perk!", "Skip", null,
            choices.Select(choice => $"{choice.Name} - {choice.Description}").ToArray());
        var index = pick == null ? -1 : choices.FindIndex(choice => pick.StartsWith(choice.Name));
        if (bluetoothClient != null)
        {
            await bluetoothClient.SendPerkPickAsync(index);
        }
        isGuestPerkDialogShowing = false;
    }
}
