using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace BongoCatServer;

public partial class IncomingKeysConnectionManager
{
    private readonly TcpListener _listener;

    public IncomingKeysConnectionManager(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
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

    [GeneratedRegex(@"connect \[(.+)\]")]
    private partial Regex ConnectRegex();

    [GeneratedRegex(@"(?:\[(.+?)\])? key pressed \((right|left)\) at (\d+)")]
    private partial Regex KeyMessageRegex();

    async Task HandleConnectionAsync(Socket socket, CancellationToken ct)
    {
        try {
            await HandleConnectionImplAsync(socket, ct);
        }
        catch (Exception e) {
            Console.WriteLine(e);
            throw;
        }
    }

    async Task HandleConnectionImplAsync(Socket socket, CancellationToken ct)
    {
        await using var stream = new NetworkStream(socket, ownsSocket: true);
        byte[] buffer = new byte[1024];

        var clientId = await Connect(ct, stream, buffer);
        if (clientId is null) {
            return;
        }
        OnConnect(clientId);

        try {
            while (!ct.IsCancellationRequested) {
                var bytesRead = await stream.ReadAsync(buffer, ct);
                if (bytesRead == 0) {
                    OnDisconnect(clientId);
                    break; // disconnect
                }

                var data = buffer.AsSpan(..bytesRead);
                var dataAsStr = Encoding.UTF8.GetString(data).TrimEnd('\0');
                Console.WriteLine(
                    "data received from '{0}': '{1}'",
                    socket.RemoteEndPoint?.Serialize(),
                    dataAsStr
                );

                try {
                    if (!TryParseMessage(socket, clientId, dataAsStr, out var message)) {
                        return;
                    }
                    OnKey(clientId, message.Hand, message.Timestamp);
                }
                catch (Exception ex) {
                    Console.WriteLine("Error thrown in message parse or event. Message:'{0}', Error:{1}", dataAsStr, ex.Message);
                    Console.WriteLine(ex.ToString());
                }

            }
        } catch (OperationCanceledException) { }
    }

    public record KeyMessage(string ClientId, Hand Hand, long? Timestamp);
    private bool TryParseMessage(
        Socket socket,
        string clientId,
        string messageStr,
        [MaybeNullWhen(false)] out KeyMessage parsed)
    {
        var match = KeyMessageRegex().Match(messageStr);
        if (!match.Success) {
            LogParseWarning(socket, $" ('{clientId}'): '{messageStr}'");
            // don't forward unrecognized messages
            parsed = null;
            return false;
        }
        var messageClientId =  match.Groups[1].Value;
        if (messageClientId != clientId) {
            throw new InvalidOperationException($"Connection's ('{socket.RemoteEndPoint?.Serialize()}')" +
                $" ClientId changed from '{clientId}' to '{messageClientId}'. Closing connection.");
        }
        var hand = match.Groups[2].Value;
        if (!Enum.TryParse<Hand>(hand, out var handParsed)) {
            LogParseWarning(socket, $"('{clientId}'): '{messageStr}' -- (`hand` '{hand}' could not be parsed)");
            parsed = null;
            return false;
        }
        var timestamp = match.Groups[3].Value;
        if (!long.TryParse(timestamp, out var timestampParsed)) {
            LogParseWarning(socket, $"Failed to parse timestamp from message ({timestamp})");
            parsed = null;
            return false;
        }

        parsed = new KeyMessage(clientId, handParsed, timestampParsed);
        return true;
    }

    private void LogParseWarning(Socket socket, string message)
    {
        Console.WriteLine(
            $"Invalid key message from '{socket.RemoteEndPoint?.Serialize().ToString() ?? "unknown remote"}' -- "
            + message
        );
    }

    private async Task<string?> Connect(CancellationToken ct, NetworkStream stream, byte[] buffer)
    {
        int bytesRead = await stream.ReadAsync(buffer, ct);
        if (bytesRead == 0) {
            return null;
        }

        // connect [username]
        var connectMsg = Encoding.UTF8.GetString(buffer).TrimEnd('\0');
        var match = ConnectRegex().Match(connectMsg);
        if (!match.Success) {
            throw new InvalidOperationException("invalid connect message")
            {
                Data =
                {
                    ["msg"] = connectMsg,
                }
            };
        }

        string clientId = match.Groups[1].Value;
        if (clientId.Length == 0) {
            throw new InvalidOperationException("invalid connect message")
            {
                Data =
                {
                    ["msg"] = connectMsg,
                }
            };
        }

        return clientId;
    }

    public delegate void RemoteKeyHandler(string clientId, Hand hand, long? timestamp);
    public delegate void ConnectHandler(string clientId);
    public event RemoteKeyHandler OnKey;
    public event ConnectHandler OnConnect;
    public event ConnectHandler OnDisconnect;
}
