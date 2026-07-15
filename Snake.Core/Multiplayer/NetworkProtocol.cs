using System.Buffers.Binary;
using System.Text.Json;

namespace SnakeGameEngine.Multiplayer;

public enum MessageType : byte
{
    Hello = 1,
    Input = 2,
    Snapshot = 3
}

// Length-prefixed framing over a NetworkStream: [1 byte type][4 byte big-endian length][UTF8 JSON payload].
// A NetworkStream supports one reader thread and one writer thread concurrently, so as long as each
// side keeps a single dedicated read loop and a single dedicated write path, no locking is needed.
public static class NetworkProtocol
{
    public static async Task WriteMessageAsync<T>(Stream stream, MessageType type, T payload, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(payload);
        var header = new byte[5];
        header[0] = (byte)type;
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(1), json.Length);
        await stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(json, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async Task<(MessageType Type, byte[] PayloadBytes)> ReadRawMessageAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var header = await ReadExactlyAsync(stream, 5, cancellationToken).ConfigureAwait(false);
        var type = (MessageType)header[0];
        var length = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(1));
        var payloadBytes = await ReadExactlyAsync(stream, length, cancellationToken).ConfigureAwait(false);
        return (type, payloadBytes);
    }

    public static T Decode<T>(byte[] payloadBytes)
    {
        return JsonSerializer.Deserialize<T>(payloadBytes)
            ?? throw new InvalidDataException($"Could not decode a {typeof(T).Name} message.");
    }

    private static async Task<byte[]> ReadExactlyAsync(Stream stream, int count, CancellationToken cancellationToken)
    {
        var buffer = new byte[count];
        var offset = 0;
        while (offset < count)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, count - offset), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                throw new IOException("Connection closed.");
            }
            offset += read;
        }
        return buffer;
    }
}
