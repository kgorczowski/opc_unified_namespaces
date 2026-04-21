namespace OPCGateway.Services.Monitoring;

public record MonitoringParameters(string Action, string ConnectionId, int OpcNamespace, List<string> NodeIds, int PublishingInterval);
