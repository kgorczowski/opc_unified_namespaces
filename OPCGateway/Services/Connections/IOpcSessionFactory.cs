using Opc.Ua;
using Opc.Ua.Client;
using OPCGateway.Controllers;

namespace OPCGateway.Services.Connections;

public interface IOpcSessionFactory
{
    Task<Session> CreateSessionAsync(string endpointUrl, string? username, string? password, SecurityMode? securityMode, SecurityPolicy? securityPolicy, UserTokenType authentication, string? certificatePath, string? certificatePassword);
}
