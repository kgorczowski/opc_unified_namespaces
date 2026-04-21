// Copyright (c) 2025 vm.pl

namespace OPCGateway.Worker.Consumers;

using System.Text.Json;
using OPCGateway.Worker.Abstractions;
using StackExchange.Redis;

/// <summary>
/// Consumes <see cref="ReadRequest"/> messages, reads the node value from
/// the OPC UA server, and publishes the result back on the per-request reply channel.
///
/// The reply channel is <c>opc:read-reply:{CorrelationId}</c>, so the API can
/// subscribe and await the value with a reasonable timeout.
/// </summary>
public sealed class OpcReadConsumer(
    IConnectionMultiplexer redis,
    IOpcSessionPool sessionPool,
    ILogger<OpcReadConsumer> logger)
    : BackgroundService
{
    private const string ConsumerName = "read-consumer";
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan ClaimIdleThreshold = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ReplyTtl = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var db = redis.GetDatabase();
        await EnsureConsumerGroupAsync(db);

        logger.LogInformation("OpcReadConsumer started – listening on stream '{Stream}'",
            ValkeyKeys.ReadRequestStream);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ClaimAbandonedAsync(db);

                var entries = await db.StreamReadGroupAsync(
                    ValkeyKeys.ReadRequestStream,
                    ValkeyKeys.WorkerConsumerGroup,
                    ConsumerName,
                    position: ">",
                    count: 20,
                    noAck: false);

                if (entries is null || entries.Length == 0)
                {
                    await Task.Delay(PollInterval, stoppingToken);
                    continue;
                }

                // Process reads in parallel – they are independent
                await Parallel.ForEachAsync(entries, stoppingToken,
                    async (entry, ct) => await ProcessEntryAsync(db, entry, ct));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in OpcReadConsumer loop – retrying in 1 s");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        logger.LogInformation("OpcReadConsumer stopped.");
    }

    private async Task ProcessEntryAsync(IDatabase db, StreamEntry entry, CancellationToken ct)
    {
        ReadRequest? req = null;
        try
        {
            var payload = entry[ValkeyKeys.PayloadField];
            req = JsonSerializer.Deserialize<ReadRequest>((string)payload!);
            if (req is null) throw new InvalidOperationException("Null deserialized request.");

            logger.LogDebug(
                "Reading node {NodeId} on server {ServerId} (correlation {Cid})",
                req.NodeId, req.ServerId, req.CorrelationId);

            var session = await sessionPool.GetOrCreateAsync(req.ServerId, ct);
            var result = await session.ReadAsync(req.NodeId, req.NamespaceIndex, ct);

            var replyPayload = JsonSerializer.Serialize(new
            {
                req.CorrelationId,
                req.NodeId,
                req.ServerId,
                result.Value,
                result.DataType,
                result.StatusCode,
                Timestamp = DateTime.UtcNow,
            });

            // Publish reply and set a short TTL key so the API can poll as fallback
            await db.PublishAsync(
                RedisChannel.Literal($"opc:read-reply:{req.CorrelationId}"),
                replyPayload);

            await db.StringSetAsync(
                $"opc:read-result:{req.CorrelationId}",
                replyPayload,
                ReplyTtl);

            await db.StreamAcknowledgeAsync(
                ValkeyKeys.ReadRequestStream,
                ValkeyKeys.WorkerConsumerGroup,
                entry.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Read FAILED – node {NodeId} server {ServerId} (correlation {Cid})",
                req?.NodeId ?? "?", req?.ServerId ?? "?", req?.CorrelationId ?? "?");

            if (req is not null)
            {
                var errorPayload = JsonSerializer.Serialize(new
                {
                    req.CorrelationId,
                    req.NodeId,
                    req.ServerId,
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow,
                });

                await db.PublishAsync(
                    RedisChannel.Literal($"opc:read-reply:{req.CorrelationId}"),
                    errorPayload);
            }
        }
    }

    private async Task EnsureConsumerGroupAsync(IDatabase db)
    {
        try
        {
            await db.StreamCreateConsumerGroupAsync(
                ValkeyKeys.ReadRequestStream,
                ValkeyKeys.WorkerConsumerGroup,
                position: StreamPosition.NewMessages,
                createStream: true);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            // Already exists
        }
    }

    private async Task ClaimAbandonedAsync(IDatabase db)
    {
        try
        {
            await db.StreamAutoClaimAsync(
                ValkeyKeys.ReadRequestStream,
                ValkeyKeys.WorkerConsumerGroup,
                ConsumerName,
                (long)ClaimIdleThreshold.TotalMilliseconds,
                "0-0",
                count: 10);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AutoClaim failed for read stream");
        }
    }
}
