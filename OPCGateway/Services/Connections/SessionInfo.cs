using Opc.Ua.Client;
using OPCGateway.Data.Entities;

namespace OPCGateway.Services.Connections;

public class SessionInfo
{
    public Opc.Ua.Client.ISession? Session { get; set; }

    public ConnectionStatus ConnectionStatus { get; set; } = ConnectionStatus.NotConnected;

    public required ConnectionParameters Parameters { get; set; }

    public SessionReconnectHandler? ReconnectHandler { get; set; }
}
