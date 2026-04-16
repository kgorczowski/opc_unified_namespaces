// Copyright (c) 2025 vm.pl

namespace OPCGateway.Admin.Server.Entities;

public class ServerEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string EndpointUrl { get; set; } = string.Empty;
    public string AuthMode { get; set; } = "Anonymous";
    public string? Username { get; set; }
    public string? PasswordHash { get; set; }
    public string SecurityMode { get; set; } = "Auto";
    public string SecurityPolicy { get; set; } = "Auto";
    public bool IsConnected { get; set; }
    public DateTime? LastConnectedAt { get; set; }
}

public class NodeEntity
{
    public string Id { get; set; } = string.Empty;
    public string ServerId { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int NamespaceIndex { get; set; }
    public string? DataType { get; set; }
    public bool MonitoringEnabled { get; set; }
    public int PublishingIntervalMs { get; set; } = 500;
    public string? Description { get; set; }
    public string? LastValue { get; set; }
    public DateTime? LastValueAt { get; set; }
    public string? Tags { get; set; }
}
