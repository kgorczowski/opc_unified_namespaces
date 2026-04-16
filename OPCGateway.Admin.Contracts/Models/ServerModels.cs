// Copyright (c) 2025 vm.pl

namespace OPCGateway.Admin.Contracts.Models;

using ProtoBuf;

[ProtoContract]
public class Empty { }

[ProtoContract]
public class ServerIdRequest
{
    [ProtoMember(1)] public string ServerId { get; set; } = string.Empty;
}

[ProtoContract]
public class OpcServerModel
{
    [ProtoMember(1)] public string Id { get; set; } = string.Empty;
    [ProtoMember(2)] public string Name { get; set; } = string.Empty;
    [ProtoMember(3)] public string EndpointUrl { get; set; } = string.Empty;
    [ProtoMember(4)] public string AuthMode { get; set; } = "Anonymous";
    [ProtoMember(5)] public string? Username { get; set; }
    [ProtoMember(6)] public string? CertificateThumbprint { get; set; }
    [ProtoMember(7)] public string SecurityMode { get; set; } = "Auto";
    [ProtoMember(8)] public string SecurityPolicy { get; set; } = "Auto";
    [ProtoMember(9)] public bool IsConnected { get; set; }
    [ProtoMember(10)] public DateTime? LastConnectedAt { get; set; }
}

[ProtoContract]
public class AddServerRequest
{
    [ProtoMember(1)] public string Name { get; set; } = string.Empty;
    [ProtoMember(2)] public string EndpointUrl { get; set; } = string.Empty;
    [ProtoMember(3)] public string AuthMode { get; set; } = "Anonymous";
    [ProtoMember(4)] public string? Username { get; set; }
    [ProtoMember(5)] public string? Password { get; set; }
    [ProtoMember(6)] public string SecurityMode { get; set; } = "Auto";
    [ProtoMember(7)] public string SecurityPolicy { get; set; } = "Auto";
}

[ProtoContract]
public class UpdateServerRequest
{
    [ProtoMember(1)] public string Id { get; set; } = string.Empty;
    [ProtoMember(2)] public string Name { get; set; } = string.Empty;
    [ProtoMember(3)] public string EndpointUrl { get; set; } = string.Empty;
    [ProtoMember(4)] public string SecurityMode { get; set; } = "Auto";
    [ProtoMember(5)] public string SecurityPolicy { get; set; } = "Auto";
}

[ProtoContract]
public class ServerListResponse
{
    [ProtoMember(1)] public List<OpcServerModel> Servers { get; set; } = [];
}

[ProtoContract]
public class ServerResponse
{
    [ProtoMember(1)] public OpcServerModel? Server { get; set; }
    [ProtoMember(2)] public bool Success { get; set; }
    [ProtoMember(3)] public string? ErrorMessage { get; set; }
}

[ProtoContract]
public class ConnectionStatusResponse
{
    [ProtoMember(1)] public string ServerId { get; set; } = string.Empty;
    [ProtoMember(2)] public bool IsConnected { get; set; }
    [ProtoMember(3)] public string StatusMessage { get; set; } = string.Empty;
    [ProtoMember(4)] public DateTime? LastCheckedAt { get; set; }
}
