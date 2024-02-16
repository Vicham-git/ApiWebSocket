using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Collections.Concurrent;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class WebSocketController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, ConcurrentBag<WebSocket>> _groupSockets = new ConcurrentDictionary<string, ConcurrentBag<WebSocket>>();

    [HttpGet("ws/{ITE}")]
    public async Task Get(string ITE)
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            AddToGroup(ITE, webSocket);

            await HandleWebSocket(HttpContext, webSocket);
        }
        else
        {
            HttpContext.Response.StatusCode = 400;
        }
    }

    private void AddToGroup(string ITE, WebSocket webSocket)
    {
        var groupList = _groupSockets.GetOrAdd(ITE, new ConcurrentBag<WebSocket>());
        groupList.Add(webSocket);
    }

    private void RemoveFromGroup(string ITE, WebSocket webSocket)
    {
        if (_groupSockets.TryGetValue(ITE, out var groupList))
        {
            groupList.TryTake(out _);

            if (groupList.IsEmpty)
            {
                _groupSockets.TryRemove(ITE, out _);
            }
        }
    }

    private async Task HandleWebSocket(HttpContext context, WebSocket webSocket)
    {
        try
        {
            var buffer = new byte[1024 * 4];

            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    BroadcastMessage(context.Request.RouteValues["ITE"].ToString(), message);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    RemoveFromGroup(context.Request.RouteValues["ITE"].ToString(), webSocket);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in HandleWebSocket: {ex.Message}");
            RemoveFromGroup(context.Request.RouteValues["ITE"].ToString(), webSocket);
        }
    }

    private async void BroadcastMessage(string ITE, string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);

        if (_groupSockets.TryGetValue(ITE, out var groupList))
        {
            foreach (var socket in groupList)
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
    }
}