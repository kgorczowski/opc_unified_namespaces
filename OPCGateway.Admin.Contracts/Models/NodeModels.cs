// Copyright (c) 2025 vm.pl

namespace OPCGateway.Admin.Contracts.Models;

using ProtoBuf;

[ProtoContract]
public class ManagedNodeModel
{
    [ProtoMember(1)] public string Id { get; set; } = string.Empty;
    [ProtoMember(2)] public string ServerId { get; set; } = string.Empty;
    [ProtoMember(3)] public string NodeId { get; set; } = string.Empty;
    [ProtoMember(4)] public string DisplayName { get; set; } = string.Empty;
    [ProtoMember(5)] public int NamespaceIndex { get; set; }
    [ProtoMember(6)] public string? DataType { get; set; }
    [ProtoMember(7)] public bool MonitoringEnabled { get; set; }
    [ProtoMember(8)] public int PublishingIntervalMs { get; set; } = 500;
    [ProtoMember(9)] public string? Description { get; set; }
    [ProtoMember(10)] public string? LastValue { get; set; }
    [ProtoMember(11)] public DateTime? LastValueAt { get; set; }
    [ProtoMember(12)] public string? Tags { get; set; }
}

[ProtoContract]
public class GetNodesRequest
{
    [ProtoMember(1)] public string? ServerId { get; set; }
    [ProtoMember(2)] public bool? MonitoringEnabledOnly { get; set; }
    [ProtoMember(3)] public int PageSize { get; set; } = 50;
    [ProtoMember(4)] public int PageNumber { get; set; } = 1;
}

[ProtoContract]
public class NodeListResponse
{
    [ProtoMember(1)] public List<ManagedNodeModel> Nodes { get; set; } = [];
    [ProtoMember(2)] public int TotalCount { get; set; }
}

[ProtoContract]
public class AddNodeRequest
{
    [ProtoMember(1)] public string ServerId { get; set; } = string.Empty;
    [ProtoMember(2)] public string NodeId { get; set; } = string.Empty;
    [ProtoMember(3)] public string DisplayName { get; set; } = string.Empty;
    [ProtoMember(4)] public int NamespaceIndex { get; set; }
    [ProtoMember(5)] public bool MonitoringEnabled { get; set; }
    [ProtoMember(6)] public int PublishingIntervalMs { get; set; } = 500;
    [ProtoMember(7)] public string? Description { get; set; }
    [ProtoMember(8)] public string? Tags { get; set; }
}

[ProtoContract]
public class UpdateNodeRequest
{
    [ProtoMember(1)] public string Id { get; set; } = string.Empty;
    [ProtoMember(2)] public string DisplayName { get; set; } = string.Empty;
    [ProtoMember(3)] public bool MonitoringEnabled { get; set; }
    [ProtoMember(4)] public int PublishingIntervalMs { get; set; } = 500;
    [ProtoMember(5)] public string? Description { get; set; }
    [ProtoMember(6)] public string? Tags { get; set; }
}

[ProtoContract]
public class NodeIdRequest
{
    [ProtoMember(1)] public string Id { get; set; } = string.Empty;
}

[ProtoContract]
public class NodeResponse
{
    [ProtoMember(1)] public ManagedNodeModel? Node { get; set; }
    [ProtoMember(2)] public bool Success { get; set; }
    [ProtoMember(3)] public string? ErrorMessage { get; set; }
}

[ProtoContract]
public class DeleteResponse
{
    [ProtoMember(1)] public bool Success { get; set; }
    [ProtoMember(2)] public string? ErrorMessage { get; set; }
}

[ProtoContract]
public class MonitorNodeRequest
{
    [ProtoMember(1)] public string NodeId { get; set; } = string.Empty;
    [ProtoMember(2)] public string ServerId { get; set; } = string.Empty;
    [ProtoMember(3)] public int IntervalMs { get; set; } = 500;
}

[ProtoContract]
public class NodeValueEvent
{
    [ProtoMember(1)] public string NodeId { get; set; } = string.Empty;
    [ProtoMember(2)] public string ServerId { get; set; } = string.Empty;
    [ProtoMember(3)] public string? Value { get; set; }
    [ProtoMember(4)] public string? DataType { get; set; }
    [ProtoMember(5)] public DateTime Timestamp { get; set; }
    [ProtoMember(6)] public string StatusCode { get; set; } = "Good";
}
