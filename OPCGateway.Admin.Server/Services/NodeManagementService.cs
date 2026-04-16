// Copyright (c) 2025 vm.pl

namespace OPCGateway.Admin.Server.Services;

using OPCGateway.Admin.Contracts.Models;
using OPCGateway.Admin.Contracts.Services;
using ProtoBuf.Grpc;

/// <summary>
/// Manages the set of OPC UA nodes tracked by the gateway.
/// The <see cref="MonitorNodeAsync"/> method opens a live subscription and streams
/// value-change events until the client cancels.
/// </summary>
public class NodeManagementService(
    INodeRepository nodeRepository,
    IOpcConnectionManager connectionManager,
    ILogger<NodeManagementService> logger)
    : INodeManagementService
{
    public async Task<NodeListResponse> GetNodesAsync(GetNodesRequest request, CallContext context = default)
    {
        logger.LogDebug("gRPC: GetNodes page {Page}", request.PageNumber);

        var (nodes, total) = await nodeRepository.GetPagedAsync(
            serverId: request.ServerId,
            monitoringOnly: request.MonitoringEnabledOnly,
            page: request.PageNumber,
            pageSize: request.PageSize,
            ct: context.CancellationToken);

        return new NodeListResponse
        {
            Nodes = nodes.Select(MapToModel).ToList(),
            TotalCount = total,
        };
    }

    public async Task<NodeResponse> AddNodeAsync(AddNodeRequest request, CallContext context = default)
    {
        logger.LogInformation("gRPC: AddNode {NodeId} on server {ServerId}", request.NodeId, request.ServerId);
        try
        {
            var entity = new NodeEntity
            {
                Id = Guid.NewGuid().ToString(),
                ServerId = request.ServerId,
                NodeId = request.NodeId,
                DisplayName = request.DisplayName,
                NamespaceIndex = request.NamespaceIndex,
                MonitoringEnabled = request.MonitoringEnabled,
                PublishingIntervalMs = request.PublishingIntervalMs,
                Description = request.Description,
                Tags = request.Tags,
            };

            await nodeRepository.AddAsync(entity, context.CancellationToken);

            if (request.MonitoringEnabled)
            {
                await connectionManager.StartMonitoringAsync(
                    request.ServerId, request.NodeId,
                    request.NamespaceIndex, request.PublishingIntervalMs,
                    context.CancellationToken);
            }

            return new NodeResponse { Success = true, Node = MapToModel(entity) };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add node {NodeId}", request.NodeId);
            return new NodeResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<NodeResponse> UpdateNodeAsync(UpdateNodeRequest request, CallContext context = default)
    {
        logger.LogInformation("gRPC: UpdateNode {Id}", request.Id);
        try
        {
            var entity = await nodeRepository.GetByIdAsync(request.Id, context.CancellationToken)
                ?? throw new InvalidOperationException($"Node {request.Id} not found.");

            var wasMonitoring = entity.MonitoringEnabled;
            entity.DisplayName = request.DisplayName;
            entity.MonitoringEnabled = request.MonitoringEnabled;
            entity.PublishingIntervalMs = request.PublishingIntervalMs;
            entity.Description = request.Description;
            entity.Tags = request.Tags;

            await nodeRepository.UpdateAsync(entity, context.CancellationToken);

            switch (wasMonitoring, request.MonitoringEnabled)
            {
                case (false, true):
                    await connectionManager.StartMonitoringAsync(
                        entity.ServerId, entity.NodeId, entity.NamespaceIndex,
                        request.PublishingIntervalMs, context.CancellationToken);
                    break;
                case (true, false):
                    await connectionManager.StopMonitoringAsync(
                        entity.ServerId, entity.NodeId, context.CancellationToken);
                    break;
                case (true, true) when wasMonitoring:
                    await connectionManager.UpdateMonitoringIntervalAsync(
                        entity.ServerId, entity.NodeId,
                        request.PublishingIntervalMs, context.CancellationToken);
                    break;
            }

            return new NodeResponse { Success = true, Node = MapToModel(entity) };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update node {Id}", request.Id);
            return new NodeResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<DeleteResponse> DeleteNodeAsync(NodeIdRequest request, CallContext context = default)
    {
        logger.LogInformation("gRPC: DeleteNode {Id}", request.Id);
        try
        {
            var entity = await nodeRepository.GetByIdAsync(request.Id, context.CancellationToken);
            if (entity is not null && entity.MonitoringEnabled)
            {
                await connectionManager.StopMonitoringAsync(
                    entity.ServerId, entity.NodeId, context.CancellationToken);
            }

            await nodeRepository.DeleteAsync(request.Id, context.CancellationToken);
            return new DeleteResponse { Success = true };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete node {Id}", request.Id);
            return new DeleteResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async IAsyncEnumerable<NodeValueEvent> MonitorNodeAsync(
        MonitorNodeRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CallContext context = default)
    {
        logger.LogInformation(
            "gRPC: MonitorNode {NodeId} on {ServerId} @{Interval}ms",
            request.NodeId, request.ServerId, request.IntervalMs);

        var channel = System.Threading.Channels.Channel.CreateUnbounded<NodeValueEvent>();

        await connectionManager.SubscribeToNodeChangesAsync(
            request.ServerId,
            request.NodeId,
            request.IntervalMs,
            async (value, dataType, statusCode, timestamp) =>
            {
                await channel.Writer.WriteAsync(new NodeValueEvent
                {
                    NodeId = request.NodeId,
                    ServerId = request.ServerId,
                    Value = value?.ToString(),
                    DataType = dataType,
                    StatusCode = statusCode,
                    Timestamp = timestamp,
                });
            },
            context.CancellationToken);

        try
        {
            await foreach (var ev in channel.Reader.ReadAllAsync(context.CancellationToken))
            {
                yield return ev;
            }
        }
        finally
        {
            await connectionManager.UnsubscribeFromNodeChangesAsync(
                request.ServerId, request.NodeId, context.CancellationToken);

            logger.LogInformation("gRPC: MonitorNode stream ended for {NodeId}", request.NodeId);
        }
    }

    private static ManagedNodeModel MapToModel(NodeEntity e) => new()
    {
        Id = e.Id,
        ServerId = e.ServerId,
        NodeId = e.NodeId,
        DisplayName = e.DisplayName,
        NamespaceIndex = e.NamespaceIndex,
        DataType = e.DataType,
        MonitoringEnabled = e.MonitoringEnabled,
        PublishingIntervalMs = e.PublishingIntervalMs,
        Description = e.Description,
        LastValue = e.LastValue,
        LastValueAt = e.LastValueAt,
        Tags = e.Tags,
    };
}
