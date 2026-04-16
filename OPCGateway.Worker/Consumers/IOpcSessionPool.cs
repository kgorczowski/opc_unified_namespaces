// Copyright (c) 2025 vm.pl

namespace OPCGateway.Worker.Consumers;

/// <summary>
/// Provides pooled, lazily-created OPC UA sessions for Worker consumers.
/// Each server ID maps to one long-lived session that is reconnected on failure.
/// </summary>
public interface IOpcSessionPool
{
    Task<IWorkerOpcSession> GetOrCreateAsync(string serverId, CancellationToken ct = default);
    Task DisposeSessionAsync(string serverId);
}

/// <summary>
/// Minimal OPC UA session API needed by the Worker consumers.
/// The implementation lives in the Worker project and wraps OPCFoundation SDK.
/// </summary>
public interface IWorkerOpcSession
{
    Task WriteAsync(string nodeId, int namespaceIndex, string valueJson, string dataType, CancellationToken ct = default);
    Task<OpcReadResult> ReadAsync(string nodeId, int namespaceIndex, CancellationToken ct = default);
}

public record OpcReadResult(string? Value, string? DataType, string StatusCode);
