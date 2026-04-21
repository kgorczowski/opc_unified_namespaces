// Copyright (c) 2025 vm.pl

namespace OPCGateway.Worker.Consumers;

using System.Text.Json;
using OPCGateway.Worker.Abstractions;
using StackExchange.Redis;

/// <summary>
/// Consumes <see cref="WriteCommand"/> messages from the Valkey stream
/// <see cref="ValkeyKeys.WriteCommandStream"/> and writes the requested values
/// to the appropriate OPC UA server.
///
/// Uses Redis Streams consumer groups so multiple Worker instances can be
/// deployed without processing the same command twice.
/// </summary>
public sealed class OpcWriteConsumer(
    IConnectionMultiplexer redis,
    IOpcSessionPool sessionPool,
    ILogger<OpcWriteConsumer> logger)
    : BackgroundService
{
    private const string ConsumerName = "write-consumer";
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan ClaimIdleThreshold = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var db = redis.GetDatabase();
        await EnsureConsumerGroupAsync(db, stoppingToken);

        logger.LogInformation("OpcWriteConsumer started – listening on stream '{Stream}'",
            ValkeyKeys.WriteCommandStream);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Claim any messages stuck for > ClaimIdleThreshold (dead worker recovery)
                await ClaimAbandonedAsync(db, stoppingToken);

                var entries = await db.StreamReadGroupAsync(
                    ValkeyKeys.WriteCommandStream,
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

                foreach (var entry in entries)
                {
                    await ProcessEntryAsync(db, entry, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in OpcWriteConsumer loop – retrying in 2 s");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }

        logger.LogInformation("OpcWriteConsumer stopped.");
    }

    private async Task ProcessEntryAsync(IDatabase db, StreamEntry entry, CancellationToken ct)
    {
        WriteCommand? cmd = null;
        try
        {
            var payload = entry[ValkeyKeys.PayloadField];
            cmd = JsonSerializer.Deserialize<WriteCommand>((string)payload!);
            if (cmd is null) throw new InvalidOperationException("Null deserialized command.");

            logger.LogDebug(
                "Writing node {NodeId} on server {ServerId} (correlation {Cid})",
                cmd.NodeId, cmd.ServerId, cmd.CorrelationId);

            var session = await sessionPool.GetOrCreateAsync(cmd.ServerId, ct);
            await session.WriteAsync(cmd.NodeId, cmd.NamespaceIndex, cmd.ValueJson, cmd.DataType, ct);

            await db.StreamAcknowledgeAsync(
                ValkeyKeys.WriteCommandStream,
                ValkeyKeys.WorkerConsumerGroup,
                entry.Id);

            logger.LogInformation(
                "Write OK – node {NodeId} server {ServerId} (correlation {Cid})",
                cmd.NodeId, cmd.ServerId, cmd.CorrelationId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Write FAILED – node {NodeId} server {ServerId} (correlation {Cid})",
                cmd?.NodeId ?? "?", cmd?.ServerId ?? "?", cmd?.CorrelationId ?? "?");

            // Do NOT acknowledge – message stays in PEL for retry / dead-letter inspection
        }
    }

    private async Task ClaimAbandonedAsync(IDatabase db, CancellationToken ct)
    {
        try
        {
            var claimed = await db.StreamAutoClaimAsync(
                ValkeyKeys.WriteCommandStream,
                ValkeyKeys.WorkerConsumerGroup,
                ConsumerName,
                (long)ClaimIdleThreshold.TotalMilliseconds,
                "0-0",
                count: 10);

            if (claimed.ClaimedEntries.Length > 0)
            {
                logger.LogWarning(
                    "Reclaimed {Count} abandoned write command(s) from idle consumers",
                    claimed.ClaimedEntries.Length);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AutoClaim failed – skipping abandoned message recovery this cycle");
        }
    }

    private async Task EnsureConsumerGroupAsync(IDatabase db, CancellationToken ct)
    {
        try
        {
            await db.StreamCreateConsumerGroupAsync(
                ValkeyKeys.WriteCommandStream,
                ValkeyKeys.WorkerConsumerGroup,
                position: StreamPosition.NewMessages,
                createStream: true);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            // Group already exists – that's fine
        }
    }
}
