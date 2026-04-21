using System.Collections.Concurrent;
using Opc.Ua.Client;
using ISession = Opc.Ua.Client.ISession;

namespace OPCGateway.Services.Monitoring;

public class SubscriptionManager : ISubscriptionManager
{
    private readonly ConcurrentDictionary<string, List<Subscription>> _subscriptions = new();

    public List<Subscription> GetSubscriptions(string connectionId)
    {
        if (!_subscriptions.TryGetValue(connectionId, out var subscriptionList))
        {
            subscriptionList = [];
            _subscriptions[connectionId] = subscriptionList;
        }

        return subscriptionList;
    }

    public Subscription GetOrCreateSubscription(string connectionId, ISession session, int publishingInterval)
    {
        var subscriptionList = GetSubscriptions(connectionId);

        // Check if a subscription with the same publishing interval already exists
        var existingSubscription = subscriptionList.FirstOrDefault(sub => sub.PublishingInterval == publishingInterval);

        if (existingSubscription != null)
        {
            return existingSubscription;
        }

        // Create a new subscription if none exists with the same publishing interval
        var newSubscription = new Subscription(session.DefaultSubscription) { PublishingInterval = publishingInterval };
        return newSubscription;
    }

    public async Task AddSubscriptionAsync(string connectionId, ISession session, Subscription subscription)
    {
        if (!session.Subscriptions.Contains(subscription))
        {
            session.AddSubscription(subscription);
            await subscription.CreateAsync();

            var subscriptionList = GetSubscriptions(connectionId);
            subscriptionList.Add(subscription);
        }

        await subscription.ApplyChangesAsync();
    }

    public void RemoveSubscription(string connectionId, Subscription subscription)
    {
        if (_subscriptions.TryGetValue(connectionId, out var subscriptionList))
        {
            subscriptionList.Remove(subscription);
        }
    }

    public List<string> GetMonitoredNodes(string connectionId)
    {
        var monitoredNodes = new List<string>();
        if (_subscriptions.TryGetValue(connectionId, out var subscriptionList))
        {
            foreach (var subscription in subscriptionList)
            {
                monitoredNodes.AddRange(subscription.MonitoredItems.Select(item => item.StartNodeId.ToString()));
            }
        }

        return monitoredNodes;
    }

    public void UpdateSubscriptionsAfterReconnection(string connectionId, ISession newSession)
    {
        var subscriptions = GetSubscriptions(connectionId);
        subscriptions.Clear();

        foreach (var subscription in newSession.Subscriptions)
        {
            subscriptions.Add(subscription);
        }
    }
}
