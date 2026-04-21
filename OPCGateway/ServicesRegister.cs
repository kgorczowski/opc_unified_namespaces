using System.Text.Json;
using OPCGateway.Data;
using OPCGateway.Data.Repositories;
using OPCGateway.Services.Connections;
using OPCGateway.Services.Monitoring;
using OPCGateway.Services.ReadWrite;

namespace OPCGateway;

public static class ServicesRegister
{
    public static IServiceCollection AddServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<PasswordEncryptor>();
        serviceCollection.AddScoped<IOpcConnectionManagement, OpcConnectionManagement>();
        serviceCollection.AddSingleton<IOpcSessionManager, OpcSessionManager>();
        serviceCollection.AddSingleton<IOpcSessionFactory, OpcSessionFactory>();
        serviceCollection.AddSingleton<ISubscriptionManager, SubscriptionManager>();
        serviceCollection.AddScoped<IConnectionRepository, ConnectionRepository>();
        serviceCollection.AddScoped<IOpcReader, OpcReader>();
        serviceCollection.AddScoped<IOpcWriter, OpcWriter>();

        serviceCollection.AddScoped<IMonitoringService, MonitoringService>();
        serviceCollection.AddScoped<IWebSocketHandler, WebSocketHandler>();

        serviceCollection.AddSingleton(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        return serviceCollection;
    }
}
