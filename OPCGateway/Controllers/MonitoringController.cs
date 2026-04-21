using Microsoft.AspNetCore.Mvc;
using OPCGateway.Services.Monitoring;

namespace OPCGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MonitoringController(IWebSocketHandler webSocketHandler) : ControllerBase
{
    [HttpGet("monitor")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task Monitor()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            await webSocketHandler.HandleWebSocketAsync(HttpContext);
        }
        else
        {
            HttpContext.Response.StatusCode = 400;
        }
    }
}
