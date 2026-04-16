// Copyright (c) 2025 vm.pl

namespace OPCGateway.Admin.Server.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OPCGateway.Admin.Server.Services;
using ProtoBuf.Grpc.Server;

/// <summary>
/// Extension methods that wire up the admin gRPC stack.
/// Call these from the host application (OPCGateway.API's Program.cs).
/// </summary>
public static class AdminGrpcExtensions
{
    /// <summary>
    /// Registers protobuf-net gRPC and the three admin service implementations.
    /// Must be called inside <c>builder.Services</c> before <c>Build()</c>.
    /// </summary>
    public static IServiceCollection AddAdminGrpcServices(this IServiceCollection services)
    {
        services.AddCodeFirstGrpc(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaxReceiveMessageSize = 4 * 1024 * 1024; // 4 MB
        });

        services.AddScoped<ServerManagementService>();
        services.AddScoped<NamespaceManagementService>();
        services.AddScoped<NodeManagementService>();

        return services;
    }

    /// <summary>
    /// Maps all three admin gRPC endpoints.
    /// Must be called inside <c>app.MapGrpcService</c> after <c>Build()</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapAdminGrpcServices(this IEndpointRouteBuilder app)
    {
        app.MapGrpcService<ServerManagementService>();
        app.MapGrpcService<NamespaceManagementService>();
        app.MapGrpcService<NodeManagementService>();
        return app;
    }
}
