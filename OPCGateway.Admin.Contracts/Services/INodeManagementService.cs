// Copyright (c) 2025 vm.pl

namespace OPCGateway.Admin.Contracts.Services;

using System.ServiceModel;
using OPCGateway.Admin.Contracts.Models;

/// <summary>
/// Manages the set of OPC UA nodes tracked by the gateway,
/// and provides a live-value stream for monitoring.
/// </summary>
[ServiceContract(Name = "NodeManagement")]
public interface INodeManagementService
{
    /// <summary>Returns managed nodes, optionally filtered by server or monitoring state.</summary>
    [OperationContract]
    Task<NodeListResponse> GetNodesAsync(GetNodesRequest request, CallContext context = default);

    /// <summary>Registers a new node for the gateway to track.</summary>
    [OperationContract]
    Task<NodeResponse> AddNodeAsync(AddNodeRequest request, CallContext context = default);

    /// <summary>Updates display name, monitoring settings, or tags of an existing node.</summary>
    [OperationContract]
    Task<NodeResponse> UpdateNodeAsync(UpdateNodeRequest request, CallContext context = default);

    /// <summary>Removes a node from gateway management (stops monitoring if active).</summary>
    [OperationContract]
    Task<DeleteResponse> DeleteNodeAsync(NodeIdRequest request, CallContext context = default);

    /// <summary>
    /// Server-streaming: emits a <see cref="NodeValueEvent"/> every time the OPC UA
    /// server reports a data-change for the requested node.
    /// The stream ends when the client cancels via <paramref name="context"/>.
    /// </summary>
    [OperationContract]
    IAsyncEnumerable<NodeValueEvent> MonitorNodeAsync(MonitorNodeRequest request, CallContext context = default);
}
