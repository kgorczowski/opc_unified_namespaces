// Copyright (c) 2025 vm.pl

namespace OPCGateway.Admin.Client.Wpf.ViewModels;

using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OPCGateway.Admin.Contracts.Models;
using OPCGateway.Admin.Contracts.Services;

/// <summary>
/// Populates a flat list of <see cref="BrowseNode"/> items streamed from
/// the gRPC <see cref="INamespaceManagementService.BrowseAsync"/> call.
/// The WPF view binds this list to a virtualized TreeView or ListView.
/// </summary>
public partial class NamespaceBrowserViewModel : ObservableObject
{
    private readonly INamespaceManagementService _service;
    private CancellationTokenSource? _browseCts;

    [ObservableProperty]
    private ObservableCollection<BrowseNode> nodes = [];

    [ObservableProperty]
    private ObservableCollection<NamespaceModel> namespaces = [];

    [ObservableProperty]
    private bool isBrowsing;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private int nodeCount;

    [ObservableProperty]
    private string? currentServerId;

    [ObservableProperty]
    private BrowseNode? selectedNode;

    public NamespaceBrowserViewModel(INamespaceManagementService service)
    {
        _service = service;
    }

    [RelayCommand]
    private async Task LoadNamespacesAsync(string serverId)
    {
        ErrorMessage = null;
        try
        {
            var response = await _service.GetNamespacesAsync(
                new ServerIdRequest { ServerId = serverId });

            Namespaces = new ObservableCollection<NamespaceModel>(response.Namespaces);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load namespaces: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task BrowseAsync(string serverId)
    {
        // Cancel any previous browse
        _browseCts?.Cancel();
        _browseCts = new CancellationTokenSource();

        Nodes.Clear();
        NodeCount = 0;
        IsBrowsing = true;
        ErrorMessage = null;
        CurrentServerId = serverId;

        try
        {
            var request = new BrowseRequest
            {
                ServerId = serverId,
                ParentNodeId = null,
                MaxDepth = 3,
            };

            await foreach (var node in _service.BrowseAsync(request)
                               .WithCancellation(_browseCts.Token))
            {
                // Marshal back to UI thread for ObservableCollection updates
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Nodes.Add(node);
                    NodeCount = Nodes.Count;
                });
            }
        }
        catch (OperationCanceledException)
        {
            // User cancelled – normal
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Browse failed: {ex.Message}";
        }
        finally
        {
            IsBrowsing = false;
        }
    }

    [RelayCommand]
    private void CancelBrowse()
    {
        _browseCts?.Cancel();
    }

    [RelayCommand]
    private async Task ExpandNodeAsync(BrowseNode parent)
    {
        if (string.IsNullOrEmpty(CurrentServerId) || !parent.HasChildren)
            return;

        // Remove placeholder children, then stream real children
        var existing = Nodes.Where(n => n.ParentNodeId == parent.NodeId).ToList();
        foreach (var old in existing)
            Nodes.Remove(old);

        var childCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            var request = new BrowseRequest
            {
                ServerId = CurrentServerId,
                ParentNodeId = parent.NodeId,
                MaxDepth = 1,
            };

            await foreach (var node in _service.BrowseAsync(request)
                               .WithCancellation(childCts.Token))
            {
                Application.Current.Dispatcher.Invoke(() => Nodes.Add(node));
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Expand failed: {ex.Message}";
        }
    }
}
