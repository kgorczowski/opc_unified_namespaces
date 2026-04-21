// Copyright (c) 2025 vm.pl

namespace OPCGateway.Admin.Contracts.Services;

using System.ServiceModel;
using System.Runtime.CompilerServices;
using OPCGateway.Admin.Contracts.Models;
using ProtoBuf.Grpc;

/// <summary>
/// Provides OPC UA address space browsing and namespace inspection.
/// </summary>
[ServiceContract(Name = "NamespaceManagement")]
public interface INamespaceManagementService
{
    /// <summary>Returns all namespaces registered on the OPC UA server.</summary>
    [OperationContract]
    Task<NamespaceListResponse> GetNamespacesAsync(ServerIdRequest request, CallContext context = default);

    /// <summary>
    /// Streams nodes from the OPC UA address space starting at the given parent node.
    /// Uses server-streaming so the WPF tree-view can populate incrementally.
    /// </summary>
    [OperationContract]
    IAsyncEnumerable<BrowseNode> BrowseAsync(BrowseRequest request, CallContext context = default);
}
