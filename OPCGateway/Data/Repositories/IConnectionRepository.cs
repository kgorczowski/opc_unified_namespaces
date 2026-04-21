using OPCGateway.Data.Entities;

namespace OPCGateway.Data.Repositories;

public interface IConnectionRepository
{
    Task SaveConnectionParametersAsync(ConnectionParameters parameters);

    Task<ConnectionParameters?> LoadConnectionParametersAsync(string connectionId);
}