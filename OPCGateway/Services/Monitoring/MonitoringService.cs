using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Opc.Ua;
using Opc.Ua.Client;
using OPCGateway.Services.Connections;
using ISession = Opc.Ua.Client.ISession;

namespace OPCGateway.Services.Monitoring;

public class MonitoringService(IOpcConnectionManagement connectionManagement, ISubscriptionManager subscriptionManager, ILogger<IMonitoringService> logger) : IMonitoringService
{
    private readonly ConcurrentDictionary<WebSocket, Task> _clients = new();
    private readonly uint _monitoredItemQueueSize = 1;
    private readonly ConcurrentDictionary<Subscription, SemaphoreSlim> _subscriptionLocks = new();

    public ConcurrentDictionary<WebSocket, Task> Clients => _clients;

    public async Task AddClientAsync(WebSocket webSocket)
    {
        _clients.TryAdd(webSocket, Task.CompletedTask);
        await SendInitialDataAsync(webSocket);
    }

    public async Task MonitorParametersAsync(string connectionId, int opcNamespace, List<string> nodeIds, int publishingInterval)
    {
        await connectionManagement.CheckConnection(connectionId);
        var session = connectionManagement.GetSession(connectionId);

        var subscription = subscriptionManager.GetOrCreateSubscription(connectionId, session, publishingInterval);

        var fullNodeIds = nodeIds.Select(nodeId =>
            OpcUtilities.GetNodeWithNamespace(opcNamespace, nodeId)).ToList();

        // Validate that nodes exist
        foreach (var fullNodeId in fullNodeIds)
        {
            if (!await NodeExistsAsync(session, fullNodeId))
            {
                throw new Exception($"NodeId: {fullNodeId} does not exist on the server.");
            }
        }

        // Get the semaphore for this subscription
        var semaphore = _subscriptionLocks.GetOrAdd(subscription, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync();
        try
        {
            foreach (var fullNodeId in fullNodeIds)
            {
                logger.LogDebug("Monitoring NodeId: {NodeId}", fullNodeId);
                MonitorItem(subscription, fullNodeId, OnTagValueChanged, publishingInterval);
            }
        }
        finally
        {
            semaphore.Release();
        }

        await subscriptionManager.AddSubscriptionAsync(connectionId, session, subscription);
    }

    public async Task StopMonitoringParametersAsync(string connectionId, int opcNamespace, List<string> nodeIds)
    {
        var subscriptionList = subscriptionManager.GetSubscriptions(connectionId);
        foreach (var subscription in subscriptionList.ToList())
        {
            // Get the semaphore for this subscription
            var semaphore = _subscriptionLocks.GetOrAdd(subscription, _ => new SemaphoreSlim(1, 1));

            await semaphore.WaitAsync();
            try
            {
                foreach (var nodeId in nodeIds)
                {
                    var fullNodeId = OpcUtilities.GetNodeWithNamespace(opcNamespace, nodeId);
                    var monitoredItem = subscription.MonitoredItems
                        .FirstOrDefault(item => item.StartNodeId.ToString() == fullNodeId);
                    if (monitoredItem != null)
                    {
                        subscription.RemoveItem(monitoredItem);
                        logger.LogDebug("Stopped monitoring NodeId: {nodeId}", nodeId);
                    }
                }

                await subscription.ApplyChangesAsync();
            }
            finally
            {
                semaphore.Release();
            }

            if (!subscription.MonitoredItems.Any())
            {
                subscriptionManager.RemoveSubscription(connectionId, subscription);

                _subscriptionLocks.TryRemove(subscription, out _);
            }
        }
    }

    public List<string> GetMonitoredNodes(string connectionId)
    {
        return subscriptionManager.GetMonitoredNodes(connectionId);
    }

    private static async Task<bool> NodeExistsAsync(ISession session, string nodeIdString)
    {
        var nodeId = new NodeId(nodeIdString);
        var readValueId = new ReadValueId
        {
            NodeId = nodeId,
            AttributeId = Attributes.NodeClass,
        };

        var readValueIds = new ReadValueIdCollection { readValueId };
        var requestHeader = new RequestHeader();
        var response = await session.ReadAsync(requestHeader, 0, TimestampsToReturn.Neither, readValueIds, CancellationToken.None);

        return (response is not null) && response.Results[0].StatusCode == Opc.Ua.StatusCodes.Good;
    }

    private static async Task SendInitialDataAsync(WebSocket webSocket)
    {
        var message = JsonSerializer.Serialize(new { Message = "Connected to OPC monitoring service." });
        var buffer = Encoding.UTF8.GetBytes(message);
        await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private void MonitorItem(Subscription subscription, string nodeIdString, Action<MonitoredItem> onTagValueChanged, int publishingInterval)
    {
        var nodeId = new NodeId(nodeIdString);
        var existingItem = subscription.MonitoredItems.FirstOrDefault(item => item.StartNodeId == nodeId);
        if (existingItem != null)
        {
            logger.LogWarning("Monitored item for NodeId: {NodeId} already exists", nodeIdString);
            return;
        }

        logger.LogDebug("Adding monitored item for NodeId: {nodeIdString}", nodeIdString);
        var monitoredItem = new MonitoredItem(subscription.DefaultItem)
        {
            StartNodeId = nodeId,
            AttributeId = Attributes.Value,
            DisplayName = nodeIdString,
            SamplingInterval = publishingInterval,
            QueueSize = _monitoredItemQueueSize,
            DiscardOldest = true,
        };

        monitoredItem.Notification += (item, _) => onTagValueChanged(item);
        subscription.AddItem(monitoredItem);
    }

    private async void OnTagValueChanged(MonitoredItem item)
    {
        var values = item.DequeueValues();
        var dataValue = values.LastOrDefault();
        if (dataValue == null)
        {
            return;
        }

        var nodeId = item.StartNodeId.ToString();
        object? value = dataValue.Value;

        var (valueType, valueString) = ValueTypeHelper.GetValueTypeAndString(value);

        var message = JsonSerializer.Serialize(new { NodeId = nodeId, Data = valueString, Type = valueType });

        var clientsSnapshot = _clients.Keys.ToList();

        foreach (var client in clientsSnapshot)
        {
            if (client.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                try
                {
                    await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error sending message to client");
                    _clients.TryRemove(client, out _);
                }
            }
            else
            {
                _clients.TryRemove(client, out _);
            }
        }
    }
}
