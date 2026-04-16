// Copyright (c) 2025 vm.pl

namespace OPCGateway.Admin.Server.Abstractions;

using OPCGateway.Admin.Server.Entities;

// ── Repositories ─────────────────────────────────────────────────────────────

public interface IServerRepository
{
    Task<IReadOnlyList<ServerEntity>> GetAllAsync(CancellationToken ct = default);
    Task<ServerEntity?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<ServerEntity> AddAsync(ServerEntity entity, CancellationToken ct = default);
    Task UpdateAsync(ServerEntity entity, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}

public interface INodeRepository
{
    Task<(IReadOnlyList<NodeEntity> Items, int Total)> GetPagedAsync(
        string? serverId,
        bool? monitoringOnly,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<NodeEntity?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<NodeEntity> AddAsync(NodeEntity entity, CancellationToken ct = default);
    Task UpdateAsync(NodeEntity entity, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}

// ── OPC Session ──────────────────────────────────────────────────────────────

public record OpcNodeReference(
    string NodeId,
    string DisplayName,
    string NodeClass,
    string? DataType,
    int NamespaceIndex,
    bool HasChildren,
    string? Description);

public interface IOpcSession
{
    string[] NamespaceUris { get; }
    Task<IReadOnlyList<OpcNodeReference>> BrowseAsync(string nodeId, CancellationToken ct = default);
}

// ── Connection Manager ───────────────────────────────────────────────────────

public record ConnectionStatus(bool IsConnected, string Message);

public interface IOpcConnectionManager
{
    Task ConnectAsync(string serverId, CancellationToken ct = default);
    Task DisconnectAsync(string serverId, CancellationToken ct = default);
    Task ReconnectAsync(string serverId, CancellationToken ct = default);
    Task<ConnectionStatus> GetStatusAsync(string serverId, CancellationToken ct = default);
    Task<IOpcSession> GetSessionAsync(string serverId, CancellationToken ct = default);

    Task StartMonitoringAsync(string serverId, string nodeId, int namespaceIndex, int intervalMs, CancellationToken ct = default);
    Task StopMonitoringAsync(string serverId, string nodeId, CancellationToken ct = default);
    Task UpdateMonitoringIntervalAsync(string serverId, string nodeId, int intervalMs, CancellationToken ct = default);

    Task SubscribeToNodeChangesAsync(
        string serverId,
        string nodeId,
        int intervalMs,
        Func<object?, string?, string, DateTime, Task> onValueChanged,
        CancellationToken ct = default);

    Task UnsubscribeFromNodeChangesAsync(string serverId, string nodeId, CancellationToken ct = default);
}
