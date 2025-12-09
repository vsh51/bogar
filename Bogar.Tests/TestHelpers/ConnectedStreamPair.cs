using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Bogar.Tests.TestHelpers;

public sealed class ConnectedStreamPair : IAsyncDisposable
{
    private bool _writerClosed;

    private ConnectedStreamPair(TcpClient readerClient, TcpClient writerClient)
    {
        ReaderClient = readerClient;
        WriterClient = writerClient;
        ReaderStream = readerClient.GetStream();
        WriterStream = writerClient.GetStream();
    }

    public TcpClient ReaderClient { get; }
    public TcpClient WriterClient { get; }
    public NetworkStream ReaderStream { get; }
    public NetworkStream WriterStream { get; }

    public static async Task<ConnectedStreamPair> CreateAsync()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        var writerClient = new TcpClient();
        var connectTask = writerClient.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
        var readerClient = await listener.AcceptTcpClientAsync();
        await connectTask;
        listener.Stop();

        return new ConnectedStreamPair(readerClient, writerClient);
    }

    public void CloseWriter()
    {
        if (_writerClosed)
            return;

        _writerClosed = true;
        try { WriterStream.Dispose(); } catch { }
        try { WriterClient.Close(); } catch { }
        try { WriterClient.Dispose(); } catch { }
    }

    public async ValueTask DisposeAsync()
    {
        CloseWriter();
        try { ReaderStream.Dispose(); } catch { }
        try { ReaderClient.Close(); } catch { }
        try { ReaderClient.Dispose(); } catch { }
        await Task.CompletedTask;
    }
}
