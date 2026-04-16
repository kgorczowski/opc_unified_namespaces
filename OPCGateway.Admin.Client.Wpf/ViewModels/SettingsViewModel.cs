// Copyright (c) 2025 vm.pl

namespace OPCGateway.Admin.Client.Wpf.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OPCGateway.Admin.Client.Wpf.Services;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IAppSettings _settings;

    [ObservableProperty]
    private string grpcEndpoint;

    [ObservableProperty]
    private string? saveMessage;

    public SettingsViewModel(IAppSettings settings)
    {
        _settings = settings;
        grpcEndpoint = settings.GrpcEndpoint;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        _settings.GrpcEndpoint = GrpcEndpoint;
        _settings.Save();
        SaveMessage = "Settings saved. Restart to apply endpoint changes.";
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        GrpcEndpoint = "http://localhost:5002";
        SaveMessage = null;
    }
}
