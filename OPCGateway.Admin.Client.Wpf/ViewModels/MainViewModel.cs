// Copyright (c) 2025 vm.pl

namespace OPCGateway.Admin.Client.Wpf.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class MainViewModel : ObservableObject
{
    private readonly ServerListViewModel _serverList;
    private readonly NodeListViewModel _nodeList;
    private readonly SettingsViewModel _settings;

    [ObservableProperty]
    private ObservableObject? currentView;

    [ObservableProperty]
    private string connectionStatus = "Disconnected";

    [ObservableProperty]
    private bool isConnected;

    [ObservableProperty]
    private string statusMessage = "Ready";

    public MainViewModel(
        ServerListViewModel serverList,
        NodeListViewModel nodeList,
        SettingsViewModel settings)
    {
        _serverList = serverList;
        _nodeList = nodeList;
        _settings = settings;

        CurrentView = _serverList;
    }

    [RelayCommand]
    private void NavigateToServers() => CurrentView = _serverList;

    [RelayCommand]
    private void NavigateToNodes() => CurrentView = _nodeList;

    [RelayCommand]
    private void NavigateToSettings() => CurrentView = _settings;
}
