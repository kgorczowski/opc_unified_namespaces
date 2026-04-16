// Copyright (c) 2025 vm.pl

namespace OPCGateway.Admin.Client.Wpf.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OPCGateway.Admin.Contracts.Models;
using OPCGateway.Admin.Contracts.Services;

public partial class ServerListViewModel : ObservableObject
{
    private readonly IServerManagementService _service;

    [ObservableProperty]
    private ObservableCollection<OpcServerModel> servers = [];

    [ObservableProperty]
    private OpcServerModel? selectedServer;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    public ServerListViewModel(IServerManagementService service)
    {
        _service = service;
    }

    [RelayCommand]
    private async Task LoadServersAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var response = await _service.GetServersAsync(new Empty());
            Servers = new ObservableCollection<OpcServerModel>(response.Servers);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load servers: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ReconnectAsync(OpcServerModel server)
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var status = await _service.ReconnectAsync(new ServerIdRequest { ServerId = server.Id });
            server.IsConnected = status.IsConnected;

            // Refresh the list item binding
            var idx = Servers.IndexOf(server);
            if (idx >= 0)
            {
                Servers[idx] = server;
                SelectedServer = server;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Reconnect failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteServerAsync(OpcServerModel server)
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var result = await _service.DeleteServerAsync(
                new ServerIdRequest { ServerId = server.Id });

            if (result.Success)
                Servers.Remove(server);
            else
                ErrorMessage = result.ErrorMessage;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Delete failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void AddServer()
    {
        // Signal to the view to open the add dialog
        OnAddServerRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void EditServer(OpcServerModel server)
    {
        SelectedServer = server;
        OnEditServerRequested?.Invoke(this, server);
    }

    public event EventHandler? OnAddServerRequested;
    public event EventHandler<OpcServerModel>? OnEditServerRequested;

    public async Task RefreshAfterSaveAsync(OpcServerModel saved)
    {
        var existing = Servers.FirstOrDefault(s => s.Id == saved.Id);
        if (existing is not null)
        {
            var idx = Servers.IndexOf(existing);
            Servers[idx] = saved;
        }
        else
        {
            Servers.Add(saved);
        }

        await Task.CompletedTask;
    }
}
