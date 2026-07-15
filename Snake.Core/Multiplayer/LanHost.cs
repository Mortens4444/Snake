using System.Net;
using System.Net.Sockets;

namespace SnakeGameEngine.Multiplayer;

// Hosts a single-guest LAN co-op session: accepts one TCP connection, forwards the guest's
// steering input into GameState, and streams a snapshot of the field back after every tick.
// The host itself keeps running the full authoritative simulation and its own local console view.
public sealed class LanHost : IDisposable
{
    private readonly TcpListener listener;
    private TcpClient? client;
    private NetworkStream? stream;
    private Task? readLoopTask;
    private CancellationTokenSource? cancellation;

    public volatile GameAction LatestGuestAction = GameAction.None;

    public bool IsConnected { get; private set; }

    public bool Disconnected { get; private set; }

    public LanHost(int port)
    {
        listener = new TcpListener(IPAddress.Any, port);
    }

    public static List<IPAddress> GetLocalIPv4Addresses()
    {
        return Dns.GetHostEntry(Dns.GetHostName()).AddressList
            .Where(address => address.AddressFamily == AddressFamily.InterNetwork)
            .ToList();
    }

    public async Task WaitForGuestAsync(CancellationToken cancellationToken)
    {
        listener.Start();
        try
        {
            client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            listener.Stop();
        }
        stream = client.GetStream();
        IsConnected = true;
    }

    public async Task SendHelloAsync(GameState gameState)
    {
        if (stream == null)
        {
            return;
        }
        await NetworkProtocol.WriteMessageAsync(stream, MessageType.Hello, SnapshotBuilder.BuildHello(gameState)).ConfigureAwait(false);

        cancellation = new CancellationTokenSource();
        readLoopTask = RunInputReadLoopAsync(cancellation.Token);
    }

    public async Task SendSnapshotAsync(GameState gameState, string statusText, string? endMessage = null)
    {
        if (stream == null || Disconnected)
        {
            return;
        }
        try
        {
            await NetworkProtocol.WriteMessageAsync(stream, MessageType.Snapshot,
                SnapshotBuilder.BuildSnapshot(gameState, statusText, endMessage)).ConfigureAwait(false);
        }
        catch (IOException)
        {
            Disconnected = true;
        }
        catch (SocketException)
        {
            Disconnected = true;
        }
    }

    private async Task RunInputReadLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && stream != null)
            {
                var (type, payload) = await NetworkProtocol.ReadRawMessageAsync(stream, cancellationToken).ConfigureAwait(false);
                if (type == MessageType.Input)
                {
                    LatestGuestAction = NetworkProtocol.Decode<InputMessage>(payload).Action;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException)
        {
            Disconnected = true;
        }
        catch (SocketException)
        {
            Disconnected = true;
        }
    }

    public void Dispose()
    {
        cancellation?.Cancel();
        stream?.Dispose();
        client?.Dispose();
        try
        {
            listener.Stop();
        }
        catch (SocketException)
        {
        }
    }
}
