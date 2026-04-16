// Copyright (c) 2025 vm.pl

namespace OPCGateway.Admin.Client.Wpf.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OPCGateway.Admin.Contracts.Models;
using OPCGateway.Admin.Contracts.Services;

public partial class ServerEditViewModel : ObservableObject
{
    private readonly IServerManagementService _service;

    [ObservableProperty] private string serverId = string.Empty;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string endpointUrl = string.Empty;
    [ObservableProperty] private string authMode = "Anonymous";
    [ObservableProperty] private string? username;
    [ObservableProperty] private string? password;
    [ObservableProperty] private string securityMode = "Auto";
    [ObservableProperty] private string securityPolicy = "Auto";

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private bool isEditMode;

    public List<string> AuthModes { get; } = ["Anonymous", "UsernamePassword", "Certificate"];
    public List<string> SecurityModes { get; } = ["Auto", "None", "Sign", "SignAndEncrypt"];
    public List<string> SecurityPolicies { get; } =
    [
        "Auto", "None",
        "Basic256Sha256", "Aes128_Sha256_RsaOaep", "Aes256_Sha256_RsaPss",
    ];

    public event EventHandler<OpcServerModel>? SaveCompleted;

    public ServerEditViewModel(IServerManagementService service)
    {
        _service = service;
    }

    public void LoadForEdit(OpcServerModel model)
    {
        ServerId = model.Id;
        Name = model.Name;
        EndpointUrl = model.EndpointUrl;
        AuthMode = model.AuthMode;
        Username = model.Username;
        SecurityMode = model.SecurityMode;
        SecurityPolicy = model.SecurityPolicy;
        IsEditMode = true;
        ErrorMessage = null;
    }

    public void LoadForAdd()
    {
        ServerId = string.Empty;
        Name = string.Empty;
        EndpointUrl = string.Empty;
        AuthMode = "Anonymous";
        Username = null;
        Password = null;
        SecurityMode = "Auto";
        SecurityPolicy = "Auto";
        IsEditMode = false;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(EndpointUrl))
        {
            ErrorMessage = "Name and Endpoint URL are required.";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            ServerResponse response;

            if (IsEditMode)
            {
                response = await _service.UpdateServerAsync(new UpdateServerRequest
                {
                    Id = ServerId,
                    Name = Name,
                    EndpointUrl = EndpointUrl,
                    SecurityMode = SecurityMode,
                    SecurityPolicy = SecurityPolicy,
                });
            }
            else
            {
                response = await _service.AddServerAsync(new AddServerRequest
                {
                    Name = Name,
                    EndpointUrl = EndpointUrl,
                    AuthMode = AuthMode,
                    Username = string.IsNullOrWhiteSpace(Username) ? null : Username,
                    Password = string.IsNullOrWhiteSpace(Password) ? null : Password,
                    SecurityMode = SecurityMode,
                    SecurityPolicy = SecurityPolicy,
                });
            }

            if (response.Success && response.Server is not null)
                SaveCompleted?.Invoke(this, response.Server);
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
