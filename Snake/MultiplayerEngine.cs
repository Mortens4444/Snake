using SnakeGameEngine.ConsoleUtils;
using SnakeGameEngine.Multiplayer;

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
            if (action == GameAction.Quit)
            {
                break;
            }
            if (action == GameAction.Pause)
            {
                // Pausing a shared session would also freeze the guest's stream; kept out of scope for now.
                continue;
            }

            gameState.PendingGuestAction = lanHost.LatestGuestAction;
            gameState.Tick(action, pressedKey);

            foreach (var soundEvent in gameState.SoundEvents)
            {
                Sounds.Play(soundEvent);
            }
            gameState.SoundEvents.Clear();

            renderer.DrawFrame(gameState);
            lanHost.SendSnapshotAsync(gameState, BuildHostStatusText(gameState)).GetAwaiter().GetResult();

            // Roguelike perk cards are a host-only concept in this first multiplayer cut;
            // the level-up still happens, it just never pauses for a choice mid-session.
            gameState.PendingPerkChoice = false;

            Thread.Sleep(gameState.GetTickMilliseconds());
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

        Console.WriteLine("Connecting...");
        using var lanClient = new LanClient();
        HelloMessage hello;
        try
        {
            hello = lanClient.ConnectAsync(address.Trim(), Port, CancellationToken.None).GetAwaiter().GetResult();
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
        while (!lanClient.Disconnected)
        {
            var action = InputReader.ReadAction(out _);
            if (action == GameAction.Quit)
            {
                break;
            }
            if (action != GameAction.None && action != lastAction)
            {
                lastAction = action;
                lanClient.SendInputAsync(action).GetAwaiter().GetResult();
            }

            var snapshot = lanClient.WaitForNextSnapshotAsync(CancellationToken.None).GetAwaiter().GetResult();
            if (snapshot == null)
            {
                break;
            }
            renderer.DrawSnapshot(snapshot);

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

    private static string BuildHostStatusText(GameState gameState)
    {
        var guestStatus = gameState.GuestSnake == null
            ? ""
            : gameState.GuestAlive
                ? $"  Guest: {gameState.GuestSnake.SnakeBodyParts.Count}"
                : "  Guest: out";
        return $"Length: {gameState.PlayerSnake.SnakeBodyParts.Count}/{Settings.Current.TargetSnakeLength}{guestStatus}"
            + $"  Enemies: {gameState.EnemySnakes.Count}  Time: {gameState.Elapsed:mm\\:ss}  ESC-Quit";
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
