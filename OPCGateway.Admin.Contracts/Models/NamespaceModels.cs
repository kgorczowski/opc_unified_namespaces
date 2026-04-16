// Copyright (c) 2025 vm.pl

namespace OPCGateway.Admin.Contracts.Models;

using ProtoBuf;

[ProtoContract]
public class BrowseRequest
{
    [ProtoMember(1)] public string ServerId { get; set; } = string.Empty;
    [ProtoMember(2)] public string? ParentNodeId { get; set; }
    [ProtoMember(3)] public int MaxDepth { get; set; } = 2;
}

[ProtoContract]
public class BrowseNode
{
    [ProtoMember(1)] public string NodeId { get; set; } = string.Empty;
    [ProtoMember(2)] public string DisplayName { get; set; } = string.Empty;
    [ProtoMember(3)] public string NodeClass { get; set; } = string.Empty;
    [ProtoMember(4)] public string? DataType { get; set; }
    [ProtoMember(5)] public int NamespaceIndex { get; set; }
    [ProtoMember(6)] public bool HasChildren { get; set; }
    [ProtoMember(7)] public int Depth { get; set; }
    [ProtoMember(8)] public string? ParentNodeId { get; set; }
    [ProtoMember(9)] public string? Description { get; set; }
}

[ProtoContract]
public class NamespaceModel
{
    [ProtoMember(1)] public int Index { get; set; }
    [ProtoMember(2)] public string Uri { get; set; } = string.Empty;
}

[ProtoContract]
public class NamespaceListResponse
{
    [ProtoMember(1)] public List<NamespaceModel> Namespaces { get; set; } = [];
}
