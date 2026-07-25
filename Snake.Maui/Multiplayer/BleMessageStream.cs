using System.Threading.Channels;

namespace SnakeGameEngine.Maui.Multiplayer;

// Adapts a chunked BLE link (each outgoing write/notify call carries one small, fixed-size raw
// byte chunk with no framing of its own) into a plain Stream, so NetworkProtocol's existing
// length-prefixed message framing (Snake.Core/Multiplayer/NetworkProtocol.cs - already generic
// over Stream, already shared unchanged by LanHost/LanClient) works over Bluetooth with zero
// changes to Snake.Core. NetworkProtocol's own 5-byte header + JSON-length framing already
// reconstructs message boundaries from a raw byte stream regardless of how it was chunked in
// transit, so this class only has to move bytes in order - it never needs to know where one
// logical message ends and the next begins.
public sealed class BleMessageStream : Stream
{
    // Conservative: fits inside the guaranteed-minimum BLE ATT_MTU (23 bytes total, 20 usable
    // after the 3-byte ATT write header) even with no MTU negotiation. Real-device testing may
    // show this can be raised for better throughput, but this is the safe starting point - it
    // can't be tuned blind (see the plan's risk notes).
    private const int ChunkSize = 20;

    private readonly Func<byte[], Task> sendChunk;
    private readonly Channel<byte> incoming = Channel.CreateUnbounded<byte>();

    public BleMessageStream(Func<byte[], Task> sendChunk)
    {
        this.sendChunk = sendChunk;
    }

    // Called by the platform-specific receive callback (the host's SetWrite handler, or the
    // guest's NotifyCharacteristic subscription) with each raw incoming chunk, in order.
    public void OnChunkReceived(byte[] chunk)
    {
        foreach (var b in chunk)
        {
            incoming.Writer.TryWrite(b);
        }
    }

    // Signals a closed connection: pending/future ReadAsync calls return 0, which
    // NetworkProtocol.ReadExactlyAsync already treats as "Connection closed." (IOException),
    // exactly like a dropped NetworkStream does for LanHost/LanClient.
    public void CompleteReceiving()
    {
        incoming.Writer.TryComplete();
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < buffer.Length; i += ChunkSize)
        {
            var length = Math.Min(ChunkSize, buffer.Length - i);
            await sendChunk(buffer.Slice(i, length).ToArray()).ConfigureAwait(false);
        }
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = 0;
        while (read < buffer.Length)
        {
            if (!await incoming.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                break;
            }
            while (read < buffer.Length && incoming.Reader.TryRead(out var b))
            {
                buffer.Span[read] = b;
                read++;
            }
        }
        return read;
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();

    public override void Write(byte[] buffer, int offset, int count) =>
        WriteAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
}
