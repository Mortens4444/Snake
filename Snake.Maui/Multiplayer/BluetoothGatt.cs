namespace SnakeGameEngine.Maui.Multiplayer;

// Reuses the well-known Nordic UART Service UUID triplet - a widely recognized "BLE byte pipe"
// convention (one service, one write characteristic, one notify characteristic) - rather than
// inventing new UUIDs, since that's exactly the shape this needs: a bidirectional byte stream.
public static class BluetoothGatt
{
    public const string ServiceUuid = "6e400001-b5a3-f393-e0a9-e50e24dcca9e";

    // Guest writes to this one; the host's SetWrite handler reads it.
    public const string GuestWriteCharacteristicUuid = "6e400002-b5a3-f393-e0a9-e50e24dcca9e";

    // Host notifies this one; the guest subscribes to it.
    public const string HostNotifyCharacteristicUuid = "6e400003-b5a3-f393-e0a9-e50e24dcca9e";

    public const string LocalName = "Snake Reloaded";
}
