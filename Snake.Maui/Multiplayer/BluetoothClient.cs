using Shiny.BluetoothLE;
using SnakeGameEngine.Multiplayer;

namespace SnakeGameEngine.Maui.Multiplayer;

// Bluetooth counterpart to Snake.Core's LanClient - same public surface (Disconnected,
// SendInputAsync, SendPerkPickAsync, WaitForNextSnapshotAsync), but talks to an already-selected
// BLE peripheral (picked from a scan list in the UI) instead of a known host address/port.
// Delegates all message framing to NetworkProtocol via a BleMessageStream, unchanged from how
// LanClient uses NetworkProtocol over a NetworkStream.
public sealed class BluetoothClient : IDisposable
{
    private readonly IPeripheral peripheral;
    private readonly BleMessageStream bleStream;
    private IDisposable? notifySubscription;
    private CancellationTokenSource? cancellation;

    public bool Disconnected { get; private set; }

    // Guarded by lock since the read loop (background) writes it and the render loop reads it -
    // mirrors LanClient's snapshotLock/latestSnapshot pattern exactly.
    private readonly object snapshotLock = new();
    private SnapshotMessage? latestSnapshot;
    private readonly SemaphoreSlim snapshotAvailable = new(0);

    public BluetoothClient(IPeripheral peripheral)
    {
        this.peripheral = peripheral;
        bleStream = new BleMessageStream(SendChunkAsync);
    }

    private async Task SendChunkAsync(byte[] chunk)
    {
        await peripheral.WriteCharacteristicAsync(BluetoothGatt.ServiceUuid, BluetoothGatt.GuestWriteCharacteristicUuid,
            chunk, true, default, 5000).ConfigureAwait(false);
    }

    public async Task<HelloMessage> ConnectAsync(CancellationToken cancellationToken)
    {
        await peripheral.ConnectAsync(new ConnectionConfig(false), cancellationToken, null).ConfigureAwait(false);

        notifySubscription = peripheral
            .NotifyCharacteristic(BluetoothGatt.ServiceUuid, BluetoothGatt.HostNotifyCharacteristicUuid, false)
            .Subscribe(result => bleStream.OnChunkReceived(result.Data ?? Array.Empty<byte>()));

        var (type, payload) = await NetworkProtocol.ReadRawMessageAsync(bleStream, cancellationToken).ConfigureAwait(false);
        if (type != MessageType.Hello)
        {
            throw new InvalidDataException("Expected a Hello message from the host.");
        }
        var hello = NetworkProtocol.Decode<HelloMessage>(payload);

        cancellation = new CancellationTokenSource();
        _ = RunSnapshotReadLoopAsync(cancellation.Token);
        return hello;
    }

    public async Task SendInputAsync(GameAction action, ConsoleKey key = default)
    {
        if (Disconnected)
        {
            return;
        }
        try
        {
            await NetworkProtocol.WriteMessageAsync(bleStream, MessageType.Input, new InputMessage(action, key)).ConfigureAwait(false);
        }
        catch (Exception)
        {
            Disconnected = true;
        }
    }

    public async Task SendPerkPickAsync(int choiceIndex)
    {
        if (Disconnected)
        {
            return;
        }
        try
        {
            await NetworkProtocol.WriteMessageAsync(bleStream, MessageType.PerkPick, new PerkPickMessage(choiceIndex)).ConfigureAwait(false);
        }
        catch (Exception)
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
            while (!cancellationToken.IsCancellationRequested)
            {
                var (type, payload) = await NetworkProtocol.ReadRawMessageAsync(bleStream, cancellationToken).ConfigureAwait(false);
                if (type == MessageType.Snapshot)
                {
                    lock (snapshotLock)
                    {
                        latestSnapshot = NetworkProtocol.Decode<SnapshotMessage>(payload);
                    }
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
        catch (Exception)
        {
            // As on BluetoothHost: Shiny's BLE failure exception surface can't be enumerated
            // without real hardware, so this is deliberately broad.
            Disconnected = true;
            snapshotAvailable.Release();
        }
    }

    public void Dispose()
    {
        cancellation?.Cancel();
        notifySubscription?.Dispose();
        bleStream.CompleteReceiving();
        peripheral.CancelConnection();
        snapshotAvailable.Dispose();
    }
}
