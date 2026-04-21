using System.Net.WebSockets;

namespace OPCGateway.Services.Monitoring;

public interface IMonitoringService
{
    Task AddClientAsync(WebSocket webSocket);

    Task MonitorParametersAsync(string connectionId, int opcNamespace, List<string> nodeIds, int publishingInterval);

    Task StopMonitoringParametersAsync(string connectionId, int opcNamespace, List<string> nodeIds);

    List<string> GetMonitoredNodes(string connectionId);
}