using System.Collections.Concurrent;
using Opc.Ua;
using Opc.Ua.Client;
using OPCGateway.Data.Entities;
using OPCGateway.Services.Monitoring;
using ISession = Opc.Ua.Client.ISession;

namespace OPCGateway.Services.Connections;

public class OpcSessionManager(ISubscriptionManager subscriptionManager, ILogger<IOpcSessionManager> logger) : IOpcSessionManager
{
    private readonly ConcurrentDictionary<string, SessionInfo> _sessionInfos = new();

    public ConnectionStatus GetConnectionStatus(string connectionId)
    {
        if (_sessionInfos.TryGetValue(connectionId, out var sessionInfo))
        {
            return sessionInfo.ConnectionStatus;
        }

        return ConnectionStatus.NotConnected;
    }

    public void AddSession(string connectionId, Session session, ConnectionParameters parameters)
    {
        session.KeepAliveInterval = 2000;
        var sessionInfo = new SessionInfo
        {
            Session = session,
            ConnectionStatus = session.Connected ? ConnectionStatus.Connected : ConnectionStatus.NotConnected,
            Parameters = parameters,
        };
        _sessionInfos[connectionId] = sessionInfo;

        session.KeepAlive += (sender, e) =>
        {
            if (sender is Session s)
            {
                if (ServiceResult.IsBad(e.Status))
                {
                    logger.LogWarning("Session disconnected. Attempting to reconnect...");
                    sessionInfo.ConnectionStatus = ConnectionStatus.Reconnecting;

                    if (sessionInfo.ReconnectHandler == null)
                    {
                        sessionInfo.ReconnectHandler = new SessionReconnectHandler();
                        sessionInfo.ReconnectHandler.BeginReconnect(sessionInfo.Session, 5000, (sender, eventArgs) => ReconnectComplete(sender, eventArgs, connectionId));
                    }
                }
                else
                {
                    sessionInfo.ConnectionStatus = s.Connected ? ConnectionStatus.Connected : ConnectionStatus.NotConnected;
                }
            }
        };
    }

    public void RemoveSession(string connectionId)
    {
        _sessionInfos.TryRemove(connectionId, out _);
    }

    public ISession GetSession(string connectionId)
    {
        if (!_sessionInfos.TryGetValue(connectionId, out var sessionInfo) || sessionInfo.Session == null)
        {
            throw new KeyNotFoundException("Connection not found.");
        }

        return sessionInfo.Session;
    }

    private void ReconnectComplete(object sender, EventArgs e, string connectionId)
    {
        if (!_sessionInfos.TryGetValue(connectionId, out var sessionInfo))
        {
            logger.LogWarning("SessionInfo not found for connectionId: {ConnectionId}", connectionId);
            return;
        }

        var reconnectHandler = sender as SessionReconnectHandler;
        if (reconnectHandler == null)
        {
            logger.LogWarning("Reconnect handler is null for connectionId: {ConnectionId}", connectionId);
            return;
        }

        // Ensure we're dealing with the correct handler
        if (!ReferenceEquals(sessionInfo.ReconnectHandler, reconnectHandler))
        {
            // This event is from an old reconnect attempt that is no longer relevant
            logger.LogWarning("Received reconnect event from an outdated handler for connectionId: {ConnectionId}", connectionId);
            return;
        }

        // Dispose of the reconnect handler
        sessionInfo.ReconnectHandler.Dispose();
        sessionInfo.ReconnectHandler = null;

        // Check if the session is connected
        if (reconnectHandler.Session != null && reconnectHandler.Session.Connected)
        {
            // Reconnection succeeded
            sessionInfo.Session = reconnectHandler.Session; // Update the session reference
            sessionInfo.ConnectionStatus = ConnectionStatus.Connected;

            subscriptionManager.UpdateSubscriptionsAfterReconnection(connectionId, sessionInfo.Session);

            logger.LogInformation("Reconnection succeeded for connectionId: {ConnectionId}", connectionId);
        }
        else
        {
            // Reconnection failed
            sessionInfo.ConnectionStatus = ConnectionStatus.NotConnected;
            logger.LogError("Reconnection failed for connectionId: {ConnectionId}", connectionId);
        }
    }
}
