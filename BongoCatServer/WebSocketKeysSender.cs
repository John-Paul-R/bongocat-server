using System.Net;
using System.Net.WebSockets;
using System.Text;

public class WebSocketKeysSender
{
    private readonly HttpListener _httpListener;
    private readonly List<Task> _connectionHandlerTasks = [];
    private readonly List<WebSocket> _connections = [];
    private CancellationToken _ct;

    public WebSocketKeysSender(int port)
    {
        _httpListener = new();
        _httpListener.Prefixes.Add($"http://+:{port}/");
    }

    public void BroadcastKey(string senderClientId, string? extraData)
    {
        ArraySegment<byte> message = new ArraySegment<byte>(Encoding.UTF8.GetBytes(
            $"{senderClientId}: {extraData}"
        ));
        foreach (var ws in _connections) {
            ws.SendAsync(message, WebSocketMessageType.Text, endOfMessage: true, _ct);
        }
    }

    public async Task Start(CancellationToken ct)
    {
        _httpListener.Start();

        _ct = ct;

        try {
            while (!ct.IsCancellationRequested) {
                var ctx = await _httpListener.GetContextAsync().WaitAsync(ct);
                if (!ctx.Request.IsWebSocketRequest) {
                    ctx.Response.Close();
                    continue;
                }

                var wsCtx = await ctx.AcceptWebSocketAsync(null);
                _connectionHandlerTasks.Add(HandleConnectionAsync(wsCtx.WebSocket, ct));
                _connections.Add(wsCtx.WebSocket);
            }
        }
        catch (OperationCanceledException) { }
        finally {
            _httpListener.Stop();
            await Task.WhenAll(_connectionHandlerTasks);
        }
    }

    public async Task HandleConnectionAsync(WebSocket ws, CancellationToken ct)
    {
        // no special message handling yet -- events are sent as they come
        // from other sources (see BroadcastKey)
    }
}