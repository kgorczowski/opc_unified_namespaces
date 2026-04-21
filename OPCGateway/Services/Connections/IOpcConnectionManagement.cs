using Opc.Ua;
using OPCGateway.Controllers;
using ISession = Opc.Ua.Client.ISession;

namespace OPCGateway.Services.Connections;

public interface IOpcConnectionManagement
{
    Task<string> ConnectAsync(string endpointUrl, string? username, string? password, string? connectionId, SecurityMode? securityMode, SecurityPolicy? securityPolicy, UserTokenType authentication, string? certificatePath, string? certificatePassword);

    Task<string> ReconnectAsync(string connectionId);

    ConnectionStatus GetConnectionStatus(string connectionId);

    void Disconnect(string connectionId);

    Task CheckConnection(string connectionId);

    ISession GetSession(string connectionId);
}