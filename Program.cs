
using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
// builder.Services.AddSignalR();

var app = builder.Build();

// app.MapGet("/", () => "Hello World!");
app.UseWebSockets();
// app.MapHub<ChatHub>("/hub");

app.MapGet("/ws", async (HttpContext context) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await HandleWebSocket(webSocket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});
app.MapControllers();
app.Run();

async Task HandleWebSocket(WebSocket webSocket)
{
    // WebSocket communication logic
    var buffer = new byte[1024 * 4];
    WebSocketReceiveResult result;

    do
    {
        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        // Handle received data

    } while (!result.CloseStatus.HasValue);

    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
}