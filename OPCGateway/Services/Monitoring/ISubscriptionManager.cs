using Opc.Ua.Client;
using ISession = Opc.Ua.Client.ISession;

namespace OPCGateway.Services.Monitoring;

public interface ISubscriptionManager
{
    List<Subscription> GetSubscriptions(string connectionId);

    Subscription GetOrCreateSubscription(string connectionId, ISession session, int publishingInterval);

    Task AddSubscriptionAsync(string connectionId, ISession session, Subscription subscription);

    void RemoveSubscription(string connectionId, Subscription subscription);

    List<string> GetMonitoredNodes(string connectionId);

    void UpdateSubscriptionsAfterReconnection(string connectionId, ISession newSession);
}