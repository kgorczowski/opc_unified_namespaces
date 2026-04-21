namespace OPCGateway.Services.Monitoring;

public interface IWebSocketHandler
{
    Task HandleWebSocketAsync(HttpContext context);
}