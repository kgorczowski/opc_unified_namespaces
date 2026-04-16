// Copyright (c) 2025 vm.pl

namespace OPCGateway.Worker.Abstractions;

/// <summary>
/// Well-known Valkey stream and channel names.
/// Must be kept in sync with any publisher (OPCGateway.API) that writes to these keys.
/// </summary>
public static class ValkeyKeys
{
    // Streams (reliable, consumer-group delivery)
    public const string WriteCommandStream = "opc:write-commands";
    public const string ReadRequestStream = "opc:read-requests";

    // Pub/Sub channels (fire-and-forget, low-latency)
    public const string DataChangeChannel = "opc:data-changes";
    public const string ConnectionEventChannel = "opc:connections";

    // Consumer group name used by all Worker instances
    public const string WorkerConsumerGroup = "opc-workers";

    // Stream field names
    public const string PayloadField = "payload";
    public const string CorrelationIdField = "correlationId";
}

/// <summary>Message published to <see cref="ValkeyKeys.WriteCommandStream"/>.</summary>
public record WriteCommand(
    string CorrelationId,
    string ServerId,
    string NodeId,
    int NamespaceIndex,
    string ValueJson,
    string DataType,
    DateTime IssuedAt);

/// <summary>Message published to <see cref="ValkeyKeys.ReadRequestStream"/>.</summary>
public record ReadRequest(
    string CorrelationId,
    string ServerId,
    string NodeId,
    int NamespaceIndex,
    string ReplyChannel,
    DateTime IssuedAt);

/// <summary>Published to <see cref="ValkeyKeys.DataChangeChannel"/> by the monitoring pipeline.</summary>
public record DataChangeEvent(
    string ServerId,
    string NodeId,
    string? Value,
    string? DataType,
    string StatusCode,
    DateTime Timestamp);

/// <summary>Published to <see cref="ValkeyKeys.ConnectionEventChannel"/>.</summary>
public record ConnectionEvent(
    string ServerId,
    string EndpointUrl,
    string EventType,  // "Connected" | "Disconnected" | "Reconnecting" | "Failed"
    string? Message,
    DateTime Timestamp);
