using Shiny.BluetoothLE;

namespace SnakeGameEngine.Maui.Multiplayer;

// One row in the Bluetooth device picker (BluetoothScanOverlay's CollectionView).
public sealed class ScannedDeviceViewModel
{
    public required string Name { get; init; }

    public required IPeripheral Peripheral { get; init; }
}
