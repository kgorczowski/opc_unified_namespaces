// Copyright (c) 2025 vm.pl
// Place this file in the OPCGateway (API) project under Infrastructure/MessageBus/

namespace OPCGateway.Infrastructure.MessageBus;

using System.Text.Json;
using OPCGateway.Worker.Abstractions;
using StackExchange.Redis;

/// <summary>
/// Publishes commands and events to Valkey on behalf of the API.
/// </summary>
public interface IMessageBusPublisher
{
    /// <summary>
    /// Enqueues a write command for the Worker to execute.
    /// Returns a correlation ID that the caller can use to track the operation.
    /// </summary>
    Task<string> PublishWriteCommandAsync(
        string serverId,
        string nodeId,
        int namespaceIndex,
        object value,
        string dataType,
        CancellationToken ct = default);

    /// <summary>
    /// Enqueues a read request and waits for the Worker's reply (up to <paramref name="timeout"/>).
    /// </summary>
    Task<ReadReply> PublishReadRequestAsync(
        string serverId,
        string nodeId,
        int namespaceIndex,
        TimeSpan? timeout = null,
        CancellationToken ct = default);

    /// <summary>
    /// Publishes a data-change event (from the monitoring pipeline) to the pub/sub channel.
    /// </summary>
    Task PublishDataChangeAsync(DataChangeEvent ev, CancellationToken ct = default);

    /// <summary>
    /// Publishes a connection lifecycle event.
    /// </summary>
    Task PublishConnectionEventAsync(ConnectionEvent ev, CancellationToken ct = default);
}

public record ReadReply(
    string CorrelationId,
    string NodeId,
    string ServerId,
    string? Value,
    string? DataType,
    string? StatusCode,
    string? Error,
    DateTime Timestamp);

/// <summary>
/// Valkey (Redis-compatible) implementation of <see cref="IMessageBusPublisher"/>.
/// </summary>
public sealed class ValkeyPublisher(
    IConnectionMultiplexer valkey,
    ILogger<ValkeyPublisher> logger)
    : IMessageBusPublisher
{
    private static readonly TimeSpan DefaultReadTimeout = TimeSpan.FromSeconds(10);

    public async Task<string> PublishWriteCommandAsync(
        string serverId, string nodeId, int namespaceIndex,
        object value, string dataType, CancellationToken ct = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");

        var cmd = new WriteCommand(
            CorrelationId: correlationId,
            ServerId: serverId,
            NodeId: nodeId,
            NamespaceIndex: namespaceIndex,
            ValueJson: JsonSerializer.Serialize(value),
            DataType: dataType,
            IssuedAt: DateTime.UtcNow);

        var db = valkey.GetDatabase();
        await db.StreamAddAsync(
            ValkeyKeys.WriteCommandStream,
            new[]
            {
                new NameValueEntry(ValkeyKeys.PayloadField, JsonSerializer.Serialize(cmd)),
                new NameValueEntry(ValkeyKeys.CorrelationIdField, correlationId),
            });

        logger.LogDebug("Write command enqueued – node {NodeId} server {ServerId} corr {Cid}",
            nodeId, serverId, correlationId);

        return correlationId;
    }

    public async Task<ReadReply> PublishReadRequestAsync(
        string serverId, string nodeId, int namespaceIndex,
        TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var replyChannel = RedisChannel.Literal($"opc:read-reply:{correlationId}");
        var effectiveTimeout = timeout ?? DefaultReadTimeout;

        var tcs = new TaskCompletionSource<ReadReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sub = valkey.GetSubscriber();

        await sub.SubscribeAsync(replyChannel, (_, message) =>
        {
            try
            {
                var reply = JsonSerializer.Deserialize<ReadReply>(message!);
                if (reply is not null)
                    tcs.TrySetResult(reply);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        try
        {
            var req = new ReadRequest(
                CorrelationId: correlationId,
                ServerId: serverId,
                NodeId: nodeId,
                NamespaceIndex: namespaceIndex,
                ReplyChannel: $"opc:read-reply:{correlationId}",
                IssuedAt: DateTime.UtcNow);

            var db = valkey.GetDatabase();
            await db.StreamAddAsync(
                ValkeyKeys.ReadRequestStream,
                new[]
                {
                    new NameValueEntry(ValkeyKeys.PayloadField, JsonSerializer.Serialize(req)),
                    new NameValueEntry(ValkeyKeys.CorrelationIdField, correlationId),
                });

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(effectiveTimeout);

            return await tcs.Task.WaitAsync(timeoutCts.Token);
        }
        finally
        {
            await sub.UnsubscribeAsync(replyChannel);
        }
    }

    public async Task PublishDataChangeAsync(DataChangeEvent ev, CancellationToken ct = default)
    {
        var db = valkey.GetDatabase();
        await db.PublishAsync(
            RedisChannel.Literal(ValkeyKeys.DataChangeChannel),
            JsonSerializer.Serialize(ev));
    }

    public async Task PublishConnectionEventAsync(ConnectionEvent ev, CancellationToken ct = default)
    {
        var db = valkey.GetDatabase();
        await db.PublishAsync(
            RedisChannel.Literal(ValkeyKeys.ConnectionEventChannel),
            JsonSerializer.Serialize(ev));
    }
}
