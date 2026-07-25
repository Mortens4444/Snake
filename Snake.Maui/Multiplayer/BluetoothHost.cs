using Shiny.BluetoothLE.Hosting;
using SnakeGameEngine.Multiplayer;

namespace SnakeGameEngine.Maui.Multiplayer;

// Bluetooth counterpart to Snake.Core's LanHost - same public surface (LatestGuestAction,
// Disconnected, SendHelloAsync, SendSnapshotAsync, ConsumePendingGuestKey,
// ConsumePendingGuestPerkPick), but advertises a BLE GATT service instead of listening on a TCP
// port. Delegates all message framing to NetworkProtocol via a BleMessageStream, unchanged from
// how LanHost uses NetworkProtocol over a NetworkStream.
public sealed class BluetoothHost : IDisposable
{
    private readonly IBleHostingManager hostingManager;
    private readonly BleMessageStream bleStream;
    private IGattCharacteristic? notifyCharacteristic;
    private IPeripheral? connectedCentral;
    private CancellationTokenSource? cancellation;

    public volatile GameAction LatestGuestAction = GameAction.None;

    // One-shot events, consumed exactly once - mirrors LanHost's inputEventLock pattern exactly.
    private readonly object inputEventLock = new();
    private ConsoleKey pendingGuestKey;
    private int? pendingGuestPerkPick;

    public bool IsConnected { get; private set; }

    public bool Disconnected { get; private set; }

    public BluetoothHost(IBleHostingManager hostingManager)
    {
        this.hostingManager = hostingManager;
        bleStream = new BleMessageStream(SendChunkAsync);
    }

    private async Task SendChunkAsync(byte[] chunk)
    {
        if (notifyCharacteristic != null && connectedCentral != null)
        {
            await notifyCharacteristic.Notify(chunk, new[] { connectedCentral }).ConfigureAwait(false);
        }
    }

    // Advertises the GATT service and waits for a guest to subscribe to the notify
    // characteristic - that subscription is this role's equivalent of "a client connected".
    public async Task WaitForGuestAsync(CancellationToken cancellationToken)
    {
        await hostingManager.RequestAccess(advertise: true, connect: true).ConfigureAwait(false);

        var guestConnected = new TaskCompletionSource();
        using var registration = cancellationToken.Register(() => guestConnected.TrySetCanceled());

        await hostingManager.AddService(BluetoothGatt.ServiceUuid, true, serviceBuilder =>
        {
            serviceBuilder.AddCharacteristic(BluetoothGatt.GuestWriteCharacteristicUuid, characteristicBuilder =>
                characteristicBuilder.SetWrite(OnGuestWriteAsync, WriteOptions.Write));

            notifyCharacteristic = serviceBuilder.AddCharacteristic(BluetoothGatt.HostNotifyCharacteristicUuid, characteristicBuilder =>
                characteristicBuilder.SetNotification(sub => OnSubscriptionChangedAsync(sub, guestConnected), NotificationOptions.Notify));
        }).ConfigureAwait(false);

        await hostingManager.StartAdvertising(new AdvertisementOptions(BluetoothGatt.LocalName, new[] { BluetoothGatt.ServiceUuid })).ConfigureAwait(false);

        await guestConnected.Task.ConfigureAwait(false);
        IsConnected = true;
    }

    private Task OnGuestWriteAsync(WriteRequest request)
    {
        bleStream.OnChunkReceived(request.Data);
        if (request.IsReplyNeeded)
        {
            request.Respond(GattState.Success);
        }
        return Task.CompletedTask;
    }

    private Task OnSubscriptionChangedAsync(CharacteristicSubscription subscription, TaskCompletionSource guestConnected)
    {
        if (subscription.IsSubscribing)
        {
            connectedCentral = subscription.Peripheral;
            guestConnected.TrySetResult();
        }
        else
        {
            Disconnected = true;
            bleStream.CompleteReceiving();
        }
        return Task.CompletedTask;
    }

    public async Task SendHelloAsync(GameState gameState)
    {
        await NetworkProtocol.WriteMessageAsync(bleStream, MessageType.Hello, SnapshotBuilder.BuildHello(gameState)).ConfigureAwait(false);

        cancellation = new CancellationTokenSource();
        _ = RunInputReadLoopAsync(cancellation.Token);
    }

    public async Task SendSnapshotAsync(GameState gameState, string statusText, string? endMessage = null)
    {
        if (Disconnected)
        {
            return;
        }
        try
        {
            await NetworkProtocol.WriteMessageAsync(bleStream, MessageType.Snapshot,
                SnapshotBuilder.BuildSnapshot(gameState, statusText, endMessage)).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Shiny's BLE send failures surface as BleException/GattException depending on
            // platform and failure mode - broad by necessity, since the exact exception surface
            // for a dropped BLE link can't be enumerated without real hardware to observe it on.
            Disconnected = true;
        }
    }

    private async Task RunInputReadLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var (type, payload) = await NetworkProtocol.ReadRawMessageAsync(bleStream, cancellationToken).ConfigureAwait(false);
                if (type == MessageType.Input)
                {
                    var input = NetworkProtocol.Decode<InputMessage>(payload);
                    LatestGuestAction = input.Action;
                    if (input.Key != default)
                    {
                        lock (inputEventLock)
                        {
                            pendingGuestKey = input.Key;
                        }
                    }
                }
                else if (type == MessageType.PerkPick)
                {
                    var pick = NetworkProtocol.Decode<PerkPickMessage>(payload);
                    lock (inputEventLock)
                    {
                        pendingGuestPerkPick = pick.ChoiceIndex;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            Disconnected = true;
        }
    }

    public ConsoleKey ConsumePendingGuestKey()
    {
        lock (inputEventLock)
        {
            var key = pendingGuestKey;
            pendingGuestKey = default;
            return key;
        }
    }

    public int? ConsumePendingGuestPerkPick()
    {
        lock (inputEventLock)
        {
            var pick = pendingGuestPerkPick;
            pendingGuestPerkPick = null;
            return pick;
        }
    }

    public void Dispose()
    {
        cancellation?.Cancel();
        bleStream.CompleteReceiving();
        try
        {
            hostingManager.StopAdvertising();
        }
        catch (Exception)
        {
        }
    }
}
