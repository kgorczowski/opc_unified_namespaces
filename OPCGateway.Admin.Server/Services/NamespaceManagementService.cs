// Copyright (c) 2025 vm.pl

namespace OPCGateway.Admin.Server.Services;

using OPCGateway.Admin.Contracts.Models;
using OPCGateway.Admin.Contracts.Services;
using ProtoBuf.Grpc;

/// <summary>
/// Serves OPC UA address-space browsing via gRPC server-streaming.
/// Each <see cref="BrowseNode"/> is yielded as soon as it is read from the OPC server,
/// so the WPF tree-view can start populating immediately.
/// </summary>
public class NamespaceManagementService(
    IOpcConnectionManager connectionManager,
    ILogger<NamespaceManagementService> logger)
    : INamespaceManagementService
{
    public async Task<NamespaceListResponse> GetNamespacesAsync(
        ServerIdRequest request,
        CallContext context = default)
    {
        logger.LogDebug("gRPC: GetNamespaces for server {Id}", request.ServerId);

        var session = await connectionManager.GetSessionAsync(request.ServerId, context.CancellationToken);
        var namespaceUris = session.NamespaceUris.ToArray();

        return new NamespaceListResponse
        {
            Namespaces = namespaceUris
                .Select((uri, index) => new NamespaceModel { Index = index, Uri = uri })
                .ToList(),
        };
    }

    public async IAsyncEnumerable<BrowseNode> BrowseAsync(
        BrowseRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CallContext context = default)
    {
        logger.LogDebug(
            "gRPC: Browse server {ServerId} from node {Parent} depth {Depth}",
            request.ServerId,
            request.ParentNodeId ?? "root",
            request.MaxDepth);

        var session = await connectionManager.GetSessionAsync(request.ServerId, context.CancellationToken);
        var rootNodeId = request.ParentNodeId ?? "i=84"; // Objects folder

        await foreach (var node in BrowseRecursiveAsync(session, rootNodeId, null, 0, request.MaxDepth, context.CancellationToken))
        {
            yield return node;
        }
    }

    private static async IAsyncEnumerable<BrowseNode> BrowseRecursiveAsync(
        IOpcSession session,
        string nodeId,
        string? parentNodeId,
        int currentDepth,
        int maxDepth,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        if (currentDepth > maxDepth)
            yield break;

        var references = await session.BrowseAsync(nodeId, ct);

        foreach (var reference in references)
        {
            ct.ThrowIfCancellationRequested();

            var node = new BrowseNode
            {
                NodeId = reference.NodeId,
                DisplayName = reference.DisplayName,
                NodeClass = reference.NodeClass,
                DataType = reference.DataType,
                NamespaceIndex = reference.NamespaceIndex,
                HasChildren = reference.HasChildren,
                Depth = currentDepth,
                ParentNodeId = parentNodeId,
                Description = reference.Description,
            };

            yield return node;

            if (reference.HasChildren && currentDepth < maxDepth)
            {
                await foreach (var child in BrowseRecursiveAsync(
                    session, reference.NodeId, reference.NodeId,
                    currentDepth + 1, maxDepth, ct))
                {
                    yield return child;
                }
            }
        }
    }
}
