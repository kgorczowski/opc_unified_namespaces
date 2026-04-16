// Copyright (c) 2025 vm.pl
// Add this file to: OPCGateway/Infrastructure/MessageBus/MessageBusExtensions.cs

namespace OPCGateway.Infrastructure.MessageBus;

using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

/// <summary>
/// Registers the Valkey connection multiplexer and the <see cref="IMessageBusPublisher"/>
/// into the DI container.
///
/// Call from Program.cs:
///   builder.Services.AddMessageBus(builder.Configuration);
/// </summary>
public static class MessageBusExtensions
{
    public static IServiceCollection AddMessageBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("Valkey")
            ?? throw new InvalidOperationException("ConnectionStrings:Valkey is required.");

        // IConnectionMultiplexer is thread-safe and intended to be reused as a singleton
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var opts = ConfigurationOptions.Parse(cs);
            opts.AbortOnConnectFail = false;      // allow graceful startup before Valkey is ready
            opts.ConnectRetry = 5;
            opts.ReconnectRetryPolicy = new ExponentialRetry(1_000);
            return ConnectionMultiplexer.Connect(opts);
        });

        services.AddSingleton<IMessageBusPublisher, ValkeyPublisher>();

        return services;
    }
}
