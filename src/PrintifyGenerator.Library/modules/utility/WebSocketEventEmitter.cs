
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class WebSocketEventEmitter
{
    private readonly Dictionary<string, List<Action<JsonElement>>> _handlers
        = new Dictionary<string, List<Action<JsonElement>>>();

    // Subscribe to an event type
    public void On(string type, Action<JsonElement> handler)
    {
        if (!_handlers.ContainsKey(type))
            _handlers[type] = new List<Action<JsonElement>>();

        _handlers[type].Add(handler);
    }

    // Emit event
    public void Emit(string type, JsonElement data)
    {
        if (_handlers.TryGetValue(type, out var handlers))
        {
            foreach (var handler in handlers)
            {
                handler(data);
            }
        }
    }
}

public class WebSocketListener
{
    private readonly WebSocket _socket;
    private readonly WebSocketEventEmitter _emitter;

    public WebSocketListener(WebSocket socket, WebSocketEventEmitter emitter)
    {
        _socket = socket;
        _emitter = emitter;
    }

    public async Task ListenAsync()
    {
        var buffer = new byte[4096];

        while (_socket.State == WebSocketState.Open)
        {
            var result = await _socket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                CancellationToken.None
            );

            if (result.MessageType == WebSocketMessageType.Close)
                break;

            var jsonString = Encoding.UTF8.GetString(buffer, 0, result.Count);

            try
            {
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                if (root.TryGetProperty("type", out var typeProp))
                {
                    string type = typeProp.GetString();

                    // Emit event with full JSON payload
                    _emitter.Emit(type, root);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Invalid JSON: {ex.Message}");
                Console.WriteLine($"Received: {jsonString}");
            }
        }
    }
}