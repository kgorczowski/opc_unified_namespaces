// Copyright (c) 2025 vm.pl

namespace OPCGateway.Admin.Client.Wpf.ViewModels;

using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OPCGateway.Admin.Contracts.Models;
using OPCGateway.Admin.Contracts.Services;

public partial class NodeListViewModel : ObservableObject
{
    private readonly INodeManagementService _service;
    private readonly Dictionary<string, CancellationTokenSource> _monitoringCts = new();

    [ObservableProperty]
    private ObservableCollection<ManagedNodeModel> nodes = [];

    [ObservableProperty]
    private ManagedNodeModel? selectedNode;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private string? filterServerId;

    public NodeListViewModel(INodeManagementService service)
    {
        _service = service;
    }

    [RelayCommand]
    private async Task LoadNodesAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var response = await _service.GetNodesAsync(new GetNodesRequest
            {
                ServerId = FilterServerId,
                PageSize = 100,
                PageNumber = 1,
            });
            Nodes = new ObservableCollection<ManagedNodeModel>(response.Nodes);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load nodes: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteNodeAsync(ManagedNodeModel node)
    {
        IsBusy = true;
        try
        {
            StopMonitoring(node);
            var result = await _service.DeleteNodeAsync(new NodeIdRequest { Id = node.Id });
            if (result.Success)
                Nodes.Remove(node);
            else
                ErrorMessage = result.ErrorMessage;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task StartMonitoringAsync(ManagedNodeModel node)
    {
        if (_monitoringCts.ContainsKey(node.Id))
            return;

        var cts = new CancellationTokenSource();
        _monitoringCts[node.Id] = cts;

        try
        {
            var request = new MonitorNodeRequest
            {
                NodeId = node.NodeId,
                ServerId = node.ServerId,
                IntervalMs = node.PublishingIntervalMs,
            };

            await foreach (var ev in _service.MonitorNodeAsync(request)
                               .WithCancellation(cts.Token))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    node.LastValue = ev.Value;
                    node.LastValueAt = ev.Timestamp;

                    // Force binding refresh
                    var idx = Nodes.IndexOf(node);
                    if (idx >= 0) Nodes[idx] = node;
                });
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Monitoring error for {node.DisplayName}: {ex.Message}";
        }
        finally
        {
            _monitoringCts.Remove(node.Id);
        }
    }

    [RelayCommand]
    private void StopMonitoringCommand(ManagedNodeModel node) => StopMonitoring(node);

    private void StopMonitoring(ManagedNodeModel node)
    {
        if (_monitoringCts.TryGetValue(node.Id, out var cts))
        {
            cts.Cancel();
            _monitoringCts.Remove(node.Id);
        }
    }

    public bool IsMonitoring(string nodeId) => _monitoringCts.ContainsKey(nodeId);

    public void StopAllMonitoring()
    {
        foreach (var cts in _monitoringCts.Values)
            cts.Cancel();
        _monitoringCts.Clear();
    }
}
