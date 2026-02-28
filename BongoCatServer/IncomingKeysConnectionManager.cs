using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BongoCatServer;

public class IncomingKeysConnectionManager
{
    private readonly TcpListener _listener;

    public IncomingKeysConnectionManager(int port)
    {
        _listener = new TcpListener(IPAddress.Loopback, port);
    }

    public async Task Run(int maxClients, CancellationToken ct)
    {
        // note: not 100% sure this is accurately described as "max clients"
        _listener.Start(maxClients);

        var connections = new List<Task>();

        try {
            while (!ct.IsCancellationRequested) {
                var acceptedSocket = await _listener.AcceptSocketAsync(ct);
                connections.Add(HandleConnectionAsync(acceptedSocket, ct));
            }
        } catch (OperationCanceledException)
        { }
        finally {
            _listener.Stop();
            await Task.WhenAll(connections);
        }
    }

    async Task HandleConnectionAsync(Socket socket, CancellationToken ct)
    {
        await using var stream = new NetworkStream(socket, ownsSocket: true);
        byte[] buffer = new byte[1024];

        try {
            while (!ct.IsCancellationRequested) {
                int bytesRead = await stream.ReadAsync(buffer, ct);
                if (bytesRead == 0) {
                    break; // disconnect
                }

                var data = buffer.AsSpan(..bytesRead);
                var dataAsStr = Encoding.UTF8.GetString(data).TrimEnd('\0');
                Console.WriteLine(
                    "data received from '{0}': '{1}'",
                    socket.RemoteEndPoint?.Serialize(),
                    dataAsStr
                );
                OnKey(socket.RemoteEndPoint?.Serialize().ToString() ?? "unknown remote", dataAsStr);
            }
        } catch (OperationCanceledException) { }
    }

    public delegate void RemoteKeyHandler(string clientId, string? extraData);
    public event RemoteKeyHandler OnKey;
}
