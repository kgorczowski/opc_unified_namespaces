// Copyright (c) 2025 vm.pl

namespace OPCGateway.Admin.Client.Wpf.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OPCGateway.Admin.Contracts.Models;
using OPCGateway.Admin.Contracts.Services;

public partial class NodeEditViewModel : ObservableObject
{
    private readonly INodeManagementService _nodeService;

    [ObservableProperty] private string nodeEntityId = string.Empty;
    [ObservableProperty] private string serverId = string.Empty;
    [ObservableProperty] private string nodeId = string.Empty;
    [ObservableProperty] private string displayName = string.Empty;
    [ObservableProperty] private int namespaceIndex;
    [ObservableProperty] private bool monitoringEnabled;
    [ObservableProperty] private int publishingIntervalMs = 500;
    [ObservableProperty] private string? description;
    [ObservableProperty] private string? tags;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private bool isEditMode;

    public event EventHandler<ManagedNodeModel>? SaveCompleted;

    public NodeEditViewModel(INodeManagementService nodeService)
    {
        _nodeService = nodeService;
    }

    public void LoadForAdd(string targetServerId, BrowseNode? fromBrowse = null)
    {
        NodeEntityId = string.Empty;
        ServerId = targetServerId;
        NodeId = fromBrowse?.NodeId ?? string.Empty;
        DisplayName = fromBrowse?.DisplayName ?? string.Empty;
        NamespaceIndex = fromBrowse?.NamespaceIndex ?? 2;
        MonitoringEnabled = false;
        PublishingIntervalMs = 500;
        Description = fromBrowse?.Description;
        Tags = null;
        IsEditMode = false;
        ErrorMessage = null;
    }

    public void LoadForEdit(ManagedNodeModel model)
    {
        NodeEntityId = model.Id;
        ServerId = model.ServerId;
        NodeId = model.NodeId;
        DisplayName = model.DisplayName;
        NamespaceIndex = model.NamespaceIndex;
        MonitoringEnabled = model.MonitoringEnabled;
        PublishingIntervalMs = model.PublishingIntervalMs;
        Description = model.Description;
        Tags = model.Tags;
        IsEditMode = true;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(NodeId) || string.IsNullOrWhiteSpace(DisplayName))
        {
            ErrorMessage = "Node ID and Display Name are required.";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            NodeResponse response;

            if (IsEditMode)
            {
                response = await _nodeService.UpdateNodeAsync(new UpdateNodeRequest
                {
                    Id = NodeEntityId,
                    DisplayName = DisplayName,
                    MonitoringEnabled = MonitoringEnabled,
                    PublishingIntervalMs = PublishingIntervalMs,
                    Description = Description,
                    Tags = Tags,
                });
            }
            else
            {
                response = await _nodeService.AddNodeAsync(new AddNodeRequest
                {
                    ServerId = ServerId,
                    NodeId = NodeId,
                    DisplayName = DisplayName,
                    NamespaceIndex = NamespaceIndex,
                    MonitoringEnabled = MonitoringEnabled,
                    PublishingIntervalMs = PublishingIntervalMs,
                    Description = Description,
                    Tags = Tags,
                });
            }

            if (response.Success && response.Node is not null)
                SaveCompleted?.Invoke(this, response.Node);
            else
                ErrorMessage = response.ErrorMessage ?? "Unknown error.";
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
}
