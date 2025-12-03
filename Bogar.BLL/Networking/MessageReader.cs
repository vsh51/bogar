using System.Net.Sockets;

namespace Bogar.BLL.Networking;

/// <summary>
/// Reads framed messages from a NetworkStream.
/// </summary>
public sealed class MessageReader
{
    private readonly NetworkStream _stream;
    private readonly byte[] _header = new byte[5];

    public MessageReader(NetworkStream stream)
    {
        this._stream = stream;
    }

    public async Task<NetworkMessage> ReadMessageAsync(
        CancellationToken cancellationToken)
    {
        await this.ReadFullyAsync(this._header, 0, this._header.Length, cancellationToken);

        var type = (MessageType)this._header[0];
        var payloadLength = BitConverter.ToInt32(this._header, 1);

        if (payloadLength < 0 || payloadLength > 50_000_000)
            throw new InvalidOperationException(
                $"Invalid payload length: {payloadLength}");

        byte[] payload = Array.Empty<byte>();
        if (payloadLength > 0)
        {
            payload = new byte[payloadLength];
            await this.ReadFullyAsync(payload, 0, payloadLength, cancellationToken);
        }

        return new NetworkMessage(type, payload);
    }

    private async Task ReadFullyAsync(
        byte[] buffer,
        int offset, int count,
        CancellationToken cancellationToken)
    {
        int read = 0;
        while (read < count)
        {
            int bytesRead = await this._stream.ReadAsync(
                buffer, offset + read,
                count - read, cancellationToken);

            if (bytesRead == 0)
                throw new InvalidOperationException(
                    "Stream closed unexpectedly");

            read += bytesRead;
        }
    }
}
