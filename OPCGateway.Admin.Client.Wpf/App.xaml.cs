// Copyright (c) 2025 vm.pl

namespace OPCGateway.Admin.Client.Wpf;

using System.Windows;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OPCGateway.Admin.Client.Wpf.Services;
using OPCGateway.Admin.Client.Wpf.ViewModels;
using OPCGateway.Admin.Client.Wpf.Views;
using OPCGateway.Admin.Contracts.Services;
using ProtoBuf.Grpc.ClientFactory;
using Serilog;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Log.Logger = new LoggerConfiguration()
            .WriteTo.File("logs/opc-admin-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        var window = Services.GetRequiredService<MainWindow>();
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Logging
        services.AddLogging(b => b.AddSerilog(dispose: true));

        // Settings (loaded from app config or user settings)
        services.AddSingleton<IAppSettings, AppSettings>();

        // gRPC channel – endpoint configured via IAppSettings
        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IAppSettings>();
            return GrpcChannel.ForAddress(settings.GrpcEndpoint, new GrpcChannelOptions
            {
                HttpHandler = new SocketsHttpHandler
                {
                    EnableMultipleHttp2Connections = true,
                    KeepAlivePingDelay = TimeSpan.FromSeconds(30),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(5),
                },
            });
        });

        // gRPC service clients (code-first, generated from the Contracts interfaces)
        services.AddCodeFirstGrpcClient<IServerManagementService>((sp, o) =>
            o.Address = new Uri(sp.GetRequiredService<IAppSettings>().GrpcEndpoint));

        services.AddCodeFirstGrpcClient<INamespaceManagementService>((sp, o) =>
            o.Address = new Uri(sp.GetRequiredService<IAppSettings>().GrpcEndpoint));

        services.AddCodeFirstGrpcClient<INodeManagementService>((sp, o) =>
            o.Address = new Uri(sp.GetRequiredService<IAppSettings>().GrpcEndpoint));

        // HttpClient for accessing the channel builder
        services.AddHttpClient();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<ServerListViewModel>();
        services.AddTransient<ServerEditViewModel>();
        services.AddTransient<NodeListViewModel>();
        services.AddTransient<NodeEditViewModel>();
        services.AddTransient<NamespaceBrowserViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Views
        services.AddTransient<MainWindow>();
    }
}
