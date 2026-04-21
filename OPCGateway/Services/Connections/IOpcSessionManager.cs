using Opc.Ua.Client;
using OPCGateway.Data.Entities;
using ISession = Opc.Ua.Client.ISession;

namespace OPCGateway.Services.Connections;

public interface IOpcSessionManager
{
    void AddSession(string connectionId, Session session, ConnectionParameters parameters);

    ConnectionStatus GetConnectionStatus(string connectionId);

    ISession GetSession(string connectionId);

    void RemoveSession(string connectionId);
}