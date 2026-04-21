using System.Collections.Concurrent;
using OPCGateway.Data.Entities;
using OPCGateway.Data.Repositories;

namespace OPCGateway.Tests.IntegrationTests;

public class InMemoryConnectionRepository : IConnectionRepository
{
    private readonly ConcurrentDictionary<string, ConnectionParameters> _connections = new();

    public Task SaveConnectionParametersAsync(ConnectionParameters parameters)
    {
        _connections[parameters.ConnectionId] = parameters;
        return Task.CompletedTask;
    }

    public Task<ConnectionParameters?> LoadConnectionParametersAsync(string connectionId)
    {
        return Task.FromResult(_connections.Values.FirstOrDefault(c => c.ConnectionId == connectionId));
    }
}