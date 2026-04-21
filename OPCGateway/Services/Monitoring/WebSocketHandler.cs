using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace OPCGateway.Services.Monitoring;

public class WebSocketHandler(IMonitoringService monitoringService, JsonSerializerOptions jsonSerializerOptions) : IWebSocketHandler
{
    public async Task HandleWebSocketAsync(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            await monitoringService.AddClientAsync(webSocket);

            await ReceiveMessagesAsync(webSocket);
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }

    private async Task ReceiveMessagesAsync(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var parameters = JsonSerializer.Deserialize<MonitoringParameters>(message, jsonSerializerOptions);
                if (parameters != null)
                {
                    try
                    {
                        switch (parameters.Action)
                        {
                            case "StartMonitoring":
                                await monitoringService.MonitorParametersAsync(parameters.ConnectionId, parameters.OpcNamespace, parameters.NodeIds, parameters.PublishingInterval);
                                break;
                            case "StopMonitoring":
                                await monitoringService.StopMonitoringParametersAsync(parameters.ConnectionId, parameters.OpcNamespace, parameters.NodeIds);
                                break;
                            case "GetMonitoredNodes":
                                var monitoredNodes = monitoringService.GetMonitoredNodes(parameters.ConnectionId);
                                var responseMessage = JsonSerializer.Serialize(new { Action = "MonitoredNodes", Nodes = monitoredNodes });
                                var responseBuffer = Encoding.UTF8.GetBytes(responseMessage);
                                await webSocket.SendAsync(new ArraySegment<byte>(responseBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                                break;
                            default:
                                throw new InvalidOperationException("Invalid action specified.");
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorMessage = JsonSerializer.Serialize(new { Action = "Error", ex.Message });
                        var errorBuffer = Encoding.UTF8.GetBytes(errorMessage);
                        await webSocket.SendAsync(new ArraySegment<byte>(errorBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
                else
                {
                    var errorMessage = JsonSerializer.Serialize(new { Action = "Error", Message = "Invalid parameters." });
                    var errorBuffer = Encoding.UTF8.GetBytes(errorMessage);
                    await webSocket.SendAsync(new ArraySegment<byte>(errorBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
        }
    }
}
