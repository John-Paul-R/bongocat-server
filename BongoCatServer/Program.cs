using BongoCatServer;

Console.WriteLine("Hello, World!");

int incomingKeysPort = 2017;
int outgoingWebSocketPort = 2018;

var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) => {
    e.Cancel = true;
    cts.Cancel();
};

var incomingKeysSvc = new IncomingKeysConnectionManager(port: incomingKeysPort);
var outgoingWebSocketSvc = new WebSocketKeysSender(outgoingWebSocketPort);

incomingKeysSvc.OnKey += outgoingWebSocketSvc.BroadcastKey;

await Task.WhenAll(
    incomingKeysSvc.Run(maxClients: 10, cts.Token),
    outgoingWebSocketSvc.Start(cts.Token)
);
