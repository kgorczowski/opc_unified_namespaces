// Copyright (c) 2025 vm.pl

namespace OPCGateway.Admin.Contracts.Services;

using System.ServiceModel;
using OPCGateway.Admin.Contracts.Models;

/// <summary>
/// Manages OPC UA server connections and configuration.
/// </summary>
[ServiceContract(Name = "ServerManagement")]
public interface IServerManagementService
{
    /// <summary>Returns all configured OPC UA servers.</summary>
    [OperationContract]
    Task<ServerListResponse> GetServersAsync(Empty request, CallContext context = default);

    /// <summary>Adds a new OPC UA server and attempts an initial connection.</summary>
    [OperationContract]
    Task<ServerResponse> AddServerAsync(AddServerRequest request, CallContext context = default);

    /// <summary>Updates server configuration (name, endpoint, security settings).</summary>
    [OperationContract]
    Task<ServerResponse> UpdateServerAsync(UpdateServerRequest request, CallContext context = default);

    /// <summary>Removes a server and terminates its connection.</summary>
    [OperationContract]
    Task<DeleteResponse> DeleteServerAsync(ServerIdRequest request, CallContext context = default);

    /// <summary>Returns the current connection status of a server.</summary>
    [OperationContract]
    Task<ConnectionStatusResponse> GetConnectionStatusAsync(ServerIdRequest request, CallContext context = default);

    /// <summary>Triggers a reconnect attempt for a server.</summary>
    [OperationContract]
    Task<ConnectionStatusResponse> ReconnectAsync(ServerIdRequest request, CallContext context = default);
}
