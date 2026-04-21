using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Opc.Ua;
using OPCGateway.Controllers.ConnectionRequests;
using OPCGateway.Services.Connections;

namespace OPCGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConnectionController(IOpcConnectionManagement connectionManagement) : ControllerBase
{
    [HttpPost("connect/anonymous")]
    public async Task<IActionResult> ConnectAnonymous([FromBody] AnonymousConnectionRequest request)
    {
        var connectionId = await connectionManagement.ConnectAsync(
            request.EndpointUrl,
            null,
            null,
            null,
            request.SecurityMode,
            request.SecurityPolicy,
            UserTokenType.Anonymous,
            null,
            null);

        return Ok(new { ConnectionId = connectionId });
    }

    [HttpPost("connect/username")]
    public async Task<IActionResult> ConnectUserName([FromBody] UserNameConnectionRequest request)
    {
        var connectionId = await connectionManagement.ConnectAsync(
            request.EndpointUrl,
            request.Username,
            request.Password,
            null,
            request.SecurityMode,
            request.SecurityPolicy,
            UserTokenType.UserName,
            null,
            null);

        return Ok(new { ConnectionId = connectionId });
    }

    [HttpPost("connect/certificate")]
    public async Task<IActionResult> ConnectCertificate([FromBody] CertificateConnectionRequest request)
    {
        var connectionId = await connectionManagement.ConnectAsync(
            request.EndpointUrl,
            null,
            null,
            null,
            request.SecurityMode,
            request.SecurityPolicy,
            UserTokenType.Certificate,
            request.CertificatePath,
            request.CertificatePassword);

        return Ok(new { ConnectionId = connectionId });
    }

    [HttpPost("reconnect/{connectionId}")]
    public async Task<IActionResult> Reconnect(string connectionId)
    {
        var resultConnectionId = await connectionManagement.ReconnectAsync(connectionId);
        return Ok(new { ConnectionId = resultConnectionId });
    }

    [HttpGet("status/{connectionId}")]
    public IActionResult GetStatus(string connectionId)
    {
        var status = connectionManagement.GetConnectionStatus(connectionId);
        return Ok(new { Status = status.ToString() });
    }

    [HttpPost("disconnect/{connectionId}")]
    public IActionResult Disconnect(string connectionId)
    {
        connectionManagement.Disconnect(connectionId);
        return Ok();
    }
}
