using System.Net.Sockets;

namespace SnakeGameEngine.Multiplayer;

// Connects to a LanHost and exposes the stream of snapshots to render, plus a way to send
// the guest's own steering input. The client runs no simulation of its own - the host is
// authoritative, so there is nothing to keep in sync beyond "draw what the host just sent".
public sealed class LanClient : IDisposable
{
    private readonly TcpClient client = new();
    private NetworkStream? stream;
    private Task? readLoopTask;
    private CancellationTokenSource? cancellation;

    public bool Disconnected { get; private set; }

    // Guarded by lock since the read loop (background thread) writes it and the render loop reads it.
    private readonly object snapshotLock = new();
    private SnapshotMessage? latestSnapshot;
    private readonly SemaphoreSlim snapshotAvailable = new(0);

    public async Task<HelloMessage> ConnectAsync(string hostAddress, int port, CancellationToken cancellationToken)
    {
        await client.ConnectAsync(hostAddress, port, cancellationToken).ConfigureAwait(false);
        stream = client.GetStream();

        var (type, payload) = await NetworkProtocol.ReadRawMessageAsync(stream, cancellationToken).ConfigureAwait(false);
        if (type != MessageType.Hello)
        {
            throw new InvalidDataException("Expected a Hello message from the host.");
        }
        var hello = NetworkProtocol.Decode<HelloMessage>(payload);

        cancellation = new CancellationTokenSource();
        readLoopTask = RunSnapshotReadLoopAsync(cancellation.Token);
        return hello;
    }

    public async Task SendInputAsync(GameAction action)
    {
        if (stream == null || Disconnected)
        {
            return;
        }
        try
        {
            await NetworkProtocol.WriteMessageAsync(stream, MessageType.Input, new InputMessage(action)).ConfigureAwait(false);
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

    // Blocks until the host sends the next snapshot (or the connection drops).
    public async Task<SnapshotMessage?> WaitForNextSnapshotAsync(CancellationToken cancellationToken)
    {
        try
        {
            await snapshotAvailable.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        lock (snapshotLock)
        {
            return latestSnapshot;
        }
    }

    private async Task RunSnapshotReadLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && stream != null)
            {
                var (type, payload) = await NetworkProtocol.ReadRawMessageAsync(stream, cancellationToken).ConfigureAwait(false);
                if (type == MessageType.Snapshot)
                {
                    lock (snapshotLock)
                    {
                        latestSnapshot = NetworkProtocol.Decode<SnapshotMessage>(payload);
                    }
                    // Only the most recent snapshot ever matters, so collapse the count back to 1
                    // if the render loop fell behind instead of letting a backlog build up.
                    if (snapshotAvailable.CurrentCount == 0)
                    {
                        snapshotAvailable.Release();
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException)
        {
            Disconnected = true;
            snapshotAvailable.Release();
        }
        catch (SocketException)
        {
            Disconnected = true;
            snapshotAvailable.Release();
        }
    }

    public void Dispose()
    {
        cancellation?.Cancel();
        stream?.Dispose();
        client.Dispose();
        snapshotAvailable.Dispose();
    }
}
