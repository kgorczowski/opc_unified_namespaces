// Copyright (c) 2025 vm.pl

using OPCGateway.Worker.Consumers;
using Serilog;
using StackExchange.Redis;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog((ctx, services, cfg) => cfg
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console())
        .ConfigureServices((ctx, services) =>
        {
            var valkeyCs = ctx.Configuration.GetConnectionString("Valkey")
                ?? throw new InvalidOperationException("ConnectionStrings:Valkey is required.");

            // Valkey / Redis
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(valkeyCs));

            // OPC UA session infrastructure
            services.AddSingleton<IOpcSessionPool, OpcSessionPool>();
            services.AddSingleton<IServerConfigurationProvider, PostgresServerConfigurationProvider>();

            // Consumers (background services)
            services.AddHostedService<OpcWriteConsumer>();
            services.AddHostedService<OpcReadConsumer>();
        })
        .Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "OPCGateway.Worker terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
