// Copyright (c) 2025 vm.pl

namespace OPCGateway.Worker.Consumers;

/// <summary>
/// Provides OPC UA server connection parameters to the Worker.
/// Implementation reads from the same PostgreSQL database as the API.
/// </summary>
public interface IServerConfigurationProvider
{
    Task<ServerConfig> GetServerConfigAsync(string serverId, CancellationToken ct = default);
}

public record ServerConfig(
    string ServerId,
    string EndpointUrl,
    string AuthMode,
    string? Username,
    string? Password,
    string SecurityMode,
    string SecurityPolicy);
