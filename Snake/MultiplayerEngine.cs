using SnakeGameEngine.ConsoleUtils;
using SnakeGameEngine.Multiplayer;
using SnakeGameEngine.Perks;

namespace SnakeGameEngine;

// LAN co-op: the host runs the full authoritative simulation (including its own enemies/AI/perks)
// and streams a snapshot to the guest after every tick; the guest only steers and renders.
// This is the stepping stone the roadmap called for before tackling mobile Bluetooth multiplayer -
// the wire protocol (Snake.Core/Multiplayer) does not care whether the transport is LAN or Bluetooth.
public static class MultiplayerEngine
{
    private const int Port = 57121;

    public static void HostGame()
    {
        if (!GameEngine.FitsInConsole())
        {
            return;
        }

        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Hosting a LAN game");
        Console.WriteLine("------------------");
        Console.ForegroundColor = ConsoleColor.White;

        var addresses = LanHost.GetLocalIPv4Addresses();
        if (addresses.Count == 0)
        {
            Console.WriteLine("No network connection found.");
        }
        else
        {
            Console.WriteLine("Tell the other player to Join using one of these addresses:");
            foreach (var address in addresses)
            {
                Console.WriteLine($"  {address}:{Port}");
            }
        }
        Console.WriteLine();
        Console.WriteLine("Waiting for a player to join... (ESC to cancel)");

        using var lanHost = new LanHost(Port);
        using var cancellation = new CancellationTokenSource();
        var waitTask = lanHost.WaitForGuestAsync(cancellation.Token);

        while (!waitTask.IsCompleted)
        {
            if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
            {
                cancellation.Cancel();
            }
            Thread.Sleep(100);
        }

        try
        {
            waitTask.GetAwaiter().GetResult();
        }
        catch (Exception)
        {
            return;
        }

        Console.WriteLine("Player connected! Starting the game...");
        Thread.Sleep(500);

        var gameState = new GameState();
        gameState.EnableGuest();
        lanHost.SendHelloAsync(gameState).GetAwaiter().GetResult();

        IRenderer renderer = new ConsoleRenderer();
        renderer.BeginGame(gameState);

        while (gameState.Status == GameStatus.Running && !lanHost.Disconnected)
        {
            var action = InputReader.ReadAction(out var pressedKey);

            // While a card is up, digits/ESC pick a perk instead of their usual meaning (ESC
            // would otherwise unconditionally quit) - this check must run before the Quit check.
            if (gameState.HostChoosingPerk)
            {
                if (pressedKey == ConsoleKey.Escape)
                {
                    ResolveHostPerkChoice(gameState, renderer, -1);
                }
                else if (pressedKey is >= ConsoleKey.D1 and <= ConsoleKey.D9)
                {
                    ResolveHostPerkChoice(gameState, renderer, pressedKey - ConsoleKey.D1);
                }
            }
            else if (action == GameAction.Quit)
            {
                break;
            }
            else if (action == GameAction.Pause)
            {
                // Pausing a shared session would also freeze the guest's stream; kept out of scope for now.
                continue;
            }

            var guestPerkPick = lanHost.ConsumePendingGuestPerkPick();
            if (gameState.GuestChoosingPerk && guestPerkPick.HasValue)
            {
                ResolveGuestPerkChoice(gameState, guestPerkPick.Value);
            }

            gameState.PendingGuestAction = lanHost.LatestGuestAction;
            gameState.PendingGuestKey = lanHost.ConsumePendingGuestKey();
            gameState.Tick(action, pressedKey);

            ArmHostPerkChoice(gameState);
            ArmGuestPerkChoice(gameState);

            foreach (var soundEvent in gameState.SoundEvents)
            {
                Sounds.Play(soundEvent);
            }
            gameState.SoundEvents.Clear();

            renderer.DrawFrame(gameState);
            if (gameState.HostChoosingPerk)
            {
                // Constants.FieldOffsetX/Y leave no dead margin, so DrawFrame repaints over the
                // card within a tick or two - it must be redrawn every iteration, not just once.
                ConsoleDrawer.DrawPerkCard(gameState.PerkChoiceOptions, gameState.Level);
            }

            lanHost.SendSnapshotAsync(gameState, BuildHostStatusText(gameState)).GetAwaiter().GetResult();

            Thread.Sleep(gameState.GetTickMilliseconds());
        }

        if (gameState.Status == GameStatus.GameOver && Settings.Current.LosePerksOnDeath)
        {
            PlayerProgress.Reset();
        }
        if (gameState.Status is GameStatus.GameOver or GameStatus.Won)
        {
            var progress = PlayerProgress.Load();
            progress.StartingLength = gameState.Status == GameStatus.GameOver && Settings.Current.LoseLengthOnDeath
                ? 2
                : gameState.PlayerSnake.SnakeBodyParts.Count;
            progress.Save();
        }

        var endMessage = BuildEndMessage(gameState);
        lanHost.SendSnapshotAsync(gameState, BuildHostStatusText(gameState), endMessage).GetAwaiter().GetResult();

        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Multiplayer game over.");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(endMessage);
        Console.WriteLine("Press any key to return to the main menu...");
        Console.ReadKey(true);
    }

    public static void JoinGame()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Join a LAN game");
        Console.WriteLine("----------------");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("Host address: ");
        Console.CursorVisible = true;
        var address = Console.ReadLine();
        Console.CursorVisible = false;
        if (string.IsNullOrWhiteSpace(address))
        {
            return;
        }

        // The host screen prints "IP:PORT" (ready to paste), but only the IP should be used to
        // connect - the port is always this app's fixed Port constant. Without stripping it, a
        // pasted "IP:PORT" gets treated as one bad hostname and fails DNS resolution.
        var hostAddress = address.Trim();
        var colonIndex = hostAddress.LastIndexOf(':');
        if (colonIndex > 0)
        {
            hostAddress = hostAddress[..colonIndex];
        }

        Console.WriteLine("Connecting...");
        using var lanClient = new LanClient();
        HelloMessage hello;
        try
        {
            hello = lanClient.ConnectAsync(hostAddress, Port, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Could not connect: {exception.Message}");
            Console.WriteLine("Press any key to return to the main menu...");
            Console.ReadKey(true);
            return;
        }

        var renderer = new MultiplayerGuestRenderer(hello);
        renderer.DrawInitial();

        var lastAction = GameAction.None;
        SnapshotMessage? lastSnapshot = null;
        while (!lanClient.Disconnected)
        {
            var action = InputReader.ReadAction(out var pressedKey);

            // Judged from the previous tick's snapshot, since this tick's hasn't arrived yet -
            // the card-up check must run before the Quit check, since ESC otherwise always quits.
            var cardIsUp = lastSnapshot?.GuestPerkChoices != null;
            if (cardIsUp)
            {
                if (pressedKey == ConsoleKey.Escape)
                {
                    lanClient.SendPerkPickAsync(-1).GetAwaiter().GetResult();
                }
                else if (pressedKey is >= ConsoleKey.D1 and <= ConsoleKey.D9)
                {
                    lanClient.SendPerkPickAsync(pressedKey - ConsoleKey.D1).GetAwaiter().GetResult();
                }
            }
            else
            {
                if (action == GameAction.Quit)
                {
                    break;
                }
                // Perk activation letters and digits all map to GameAction.None, so the send must
                // also fire on any raw keypress, not just a changed steering action - otherwise
                // those keystrokes would be silently swallowed and never reach the host.
                if ((action != GameAction.None && action != lastAction) || pressedKey != default)
                {
                    if (action != GameAction.None)
                    {
                        lastAction = action;
                    }
                    lanClient.SendInputAsync(action, pressedKey).GetAwaiter().GetResult();
                }
            }

            var snapshot = lanClient.WaitForNextSnapshotAsync(CancellationToken.None).GetAwaiter().GetResult();
            if (snapshot == null)
            {
                break;
            }

            if (cardIsUp && snapshot.GuestPerkChoices == null)
            {
                // The card was drawn straight over live board cells; Frame is a full redraw of
                // every live entity each tick, so a full repaint here is safe - DrawSnapshot right
                // after restores every entity, it just also needs the static background back first.
                renderer.DrawInitial();
            }
            renderer.DrawSnapshot(snapshot);
            if (snapshot.GuestPerkChoices != null)
            {
                MultiplayerGuestRenderer.DrawPerkCard(snapshot.GuestPerkChoices, snapshot.GuestLevel);
            }
            lastSnapshot = snapshot;

            if (!snapshot.IsRunning)
            {
                Console.WriteLine();
                Console.WriteLine(snapshot.EndMessage ?? "The game has ended.");
                Console.WriteLine("Press any key to return to the main menu...");
                Console.ReadKey(true);
                break;
            }
        }

        if (lanClient.Disconnected)
        {
            Console.WriteLine();
            Console.WriteLine("Connection to the host was lost.");
            Console.WriteLine("Press any key to return to the main menu...");
            Console.ReadKey(true);
        }
    }

    // Arms the freeze right where the old code discarded PendingPerkChoice: computes the offered
    // cards once (not re-rolled every frame while the non-blocking card is up).
    private static void ArmHostPerkChoice(GameState gameState)
    {
        if (gameState.Status == GameStatus.Running && gameState.PendingPerkChoice && !gameState.HostChoosingPerk)
        {
            gameState.PendingPerkChoice = false;
            gameState.HostChoosingPerk = true;
            gameState.PerkChoiceOptions = PerkFactory.GetRandomChoices(gameState.PlayerPerks, Settings.Current.PerkChoicesPerLevel);
        }
    }

    private static void ArmGuestPerkChoice(GameState gameState)
    {
        if (gameState.Status == GameStatus.Running && gameState.GuestPendingPerkChoice && !gameState.GuestChoosingPerk)
        {
            gameState.GuestPendingPerkChoice = false;
            gameState.GuestChoosingPerk = true;
            gameState.GuestPerkChoiceOptions = PerkFactory.GetRandomChoices(gameState.GuestPerks, Settings.Current.PerkChoicesPerLevel, forGuest: true);
        }
    }

    private static void ResolveHostPerkChoice(GameState gameState, IRenderer renderer, int choiceIndex)
    {
        if (choiceIndex >= 0 && choiceIndex < gameState.PerkChoiceOptions.Count)
        {
            gameState.PlayerPerks.Add(gameState.PerkChoiceOptions[choiceIndex]);
            // Load-modify-save, not a fresh PlayerProgress: replacing it outright would silently
            // wipe the persisted StartingLength (see SaveStartingLength below).
            var progress = PlayerProgress.Load();
            progress.PerkNames = gameState.PlayerPerks.Select(perk => perk.Name).ToList();
            progress.Save();
            Sounds.Play(SoundEvent.PerkGained);
        }
        gameState.PerkChoiceOptions = new List<Perk>();
        gameState.HostChoosingPerk = false;
        // Wipes the card overlay - the field/border/background all need a full repaint since the
        // card was drawn straight over live board cells (same pattern ShowPerkSelection uses).
        renderer.BeginGame(gameState);
    }

    private static void ResolveGuestPerkChoice(GameState gameState, int choiceIndex)
    {
        if (choiceIndex >= 0 && choiceIndex < gameState.GuestPerkChoiceOptions.Count)
        {
            var perk = gameState.GuestPerkChoiceOptions[choiceIndex];
            perk.IsGuestOwned = true;
            gameState.GuestPerks.Add(perk);
            Sounds.Play(SoundEvent.PerkGained);
        }
        gameState.GuestPerkChoiceOptions = new List<Perk>();
        gameState.GuestChoosingPerk = false;
    }

    private static string BuildHostStatusText(GameState gameState)
    {
        var guestStatus = gameState.GuestSnake == null
            ? ""
            : gameState.GuestAlive
                ? $"  Guest: {gameState.GuestSnake.SnakeBodyParts.Count}"
                : "  Guest: out";
        // Lets the guest's screen know why the host snake stopped moving - the guest already
        // knows about its own card since it's drawn directly on the guest's own screen.
        var hostPerkStatus = gameState.HostChoosingPerk ? "  Host is choosing a perk..." : "";
        return $"Length: {gameState.PlayerSnake.SnakeBodyParts.Count}/{Settings.Current.TargetSnakeLength}{guestStatus}"
            + $"  Enemies: {gameState.EnemySnakes.Count}  Time: {gameState.Elapsed:mm\\:ss}  ESC-Quit{hostPerkStatus}";
    }

    private static string BuildEndMessage(GameState gameState)
    {
        if (gameState.Status == GameStatus.Won)
        {
            return $"{gameState.WinnerName} won by reaching {Settings.Current.TargetSnakeLength} parts!";
        }
        if (gameState.PlayerKilledBy != null)
        {
            return $"Host died ({gameState.PlayerKilledBy}). Session over.";
        }
        if (!gameState.GuestAlive && gameState.GuestKilledBy != null)
        {
            return $"Guest was eliminated ({gameState.GuestKilledBy}). Host is still playing.";
        }
        return "The host ended the session.";
    }
}
