using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Collections.Concurrent;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class WebSocketController : ControllerBase
{
    private static readonly ConcurrentBag<WebSocket> _sockets = new ConcurrentBag<WebSocket>();
    [HttpGet("ws")]
    public async Task Get()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            _sockets.Add(webSocket);
            await HandleWebSocket(HttpContext, webSocket);
        }
        else
        {
            HttpContext.Response.StatusCode = 400;
        }
    }


    [HttpGet("test")]
    public async Task Test(string message)
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await HandleWebSocket(HttpContext, webSocket);
        }
        else
        {
            HttpContext.Response.StatusCode = 400;
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
                    BroadcastMessage(message);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    _sockets.TryTake(out _);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            // Log or handle the exception
            Console.WriteLine($"Error in HandleWebSocket: {ex.Message}");
            _sockets.TryTake(out _);
        }
    }

    private async void BroadcastMessage(string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);

        foreach (var socket in _sockets)
        {
            if (socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}
