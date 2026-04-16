// Copyright (c) 2025 vm.pl

namespace OPCGateway.Admin.Server.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OPCGateway.Admin.Server.Abstractions;
using OPCGateway.Admin.Server.Repositories;

public static class RepositoryExtensions
{
    /// <summary>
    /// Registers Npgsql data source and the two repository implementations.
    /// Call from Program.cs or AdminGrpcExtensions.
    /// </summary>
    public static IServiceCollection AddAdminRepositories(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

        services.AddNpgsqlDataSource(cs);

        services.AddScoped<IServerRepository, PostgresServerRepository>();
        services.AddScoped<INodeRepository, PostgresNodeRepository>();

        return services;
    }
}
