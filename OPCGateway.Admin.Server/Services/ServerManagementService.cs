// Copyright (c) 2025 vm.pl

namespace OPCGateway.Admin.Server.Services;

using OPCGateway.Admin.Contracts.Models;
using OPCGateway.Admin.Contracts.Services;
using ProtoBuf.Grpc;

/// <summary>
/// gRPC server-side implementation of <see cref="IServerManagementService"/>.
/// Delegates to the domain services already present in OPCGateway.API.
/// </summary>
public class ServerManagementService(
    IServerRepository serverRepository,
    IOpcConnectionManager connectionManager,
    ILogger<ServerManagementService> logger)
    : IServerManagementService
{
    public async Task<ServerListResponse> GetServersAsync(Empty request, CallContext context = default)
    {
        logger.LogDebug("gRPC: GetServers");
        var servers = await serverRepository.GetAllAsync(context.CancellationToken);
        return new ServerListResponse
        {
            Servers = servers.Select(MapToModel).ToList(),
        };
    }

    public async Task<ServerResponse> AddServerAsync(AddServerRequest request, CallContext context = default)
    {
        logger.LogInformation("gRPC: AddServer {EndpointUrl}", request.EndpointUrl);
        try
        {
            var entity = await serverRepository.AddAsync(new ServerEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                EndpointUrl = request.EndpointUrl,
                AuthMode = request.AuthMode,
                Username = request.Username,
                PasswordHash = request.Password is { } pw ? HashPassword(pw) : null,
                SecurityMode = request.SecurityMode,
                SecurityPolicy = request.SecurityPolicy,
            }, context.CancellationToken);

            await connectionManager.ConnectAsync(entity.Id, context.CancellationToken);

            return new ServerResponse { Success = true, Server = MapToModel(entity) };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add server {EndpointUrl}", request.EndpointUrl);
            return new ServerResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ServerResponse> UpdateServerAsync(UpdateServerRequest request, CallContext context = default)
    {
        logger.LogInformation("gRPC: UpdateServer {Id}", request.Id);
        try
        {
            var entity = await serverRepository.GetByIdAsync(request.Id, context.CancellationToken)
                ?? throw new InvalidOperationException($"Server {request.Id} not found.");

            entity.Name = request.Name;
            entity.EndpointUrl = request.EndpointUrl;
            entity.SecurityMode = request.SecurityMode;
            entity.SecurityPolicy = request.SecurityPolicy;

            await serverRepository.UpdateAsync(entity, context.CancellationToken);
            return new ServerResponse { Success = true, Server = MapToModel(entity) };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update server {Id}", request.Id);
            return new ServerResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<DeleteResponse> DeleteServerAsync(ServerIdRequest request, CallContext context = default)
    {
        logger.LogInformation("gRPC: DeleteServer {Id}", request.ServerId);
        try
        {
            await connectionManager.DisconnectAsync(request.ServerId, context.CancellationToken);
            await serverRepository.DeleteAsync(request.ServerId, context.CancellationToken);
            return new DeleteResponse { Success = true };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete server {Id}", request.ServerId);
            return new DeleteResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ConnectionStatusResponse> GetConnectionStatusAsync(ServerIdRequest request, CallContext context = default)
    {
        var status = await connectionManager.GetStatusAsync(request.ServerId, context.CancellationToken);
        return new ConnectionStatusResponse
        {
            ServerId = request.ServerId,
            IsConnected = status.IsConnected,
            StatusMessage = status.Message,
            LastCheckedAt = DateTime.UtcNow,
        };
    }

    public async Task<ConnectionStatusResponse> ReconnectAsync(ServerIdRequest request, CallContext context = default)
    {
        logger.LogInformation("gRPC: Reconnect {Id}", request.ServerId);
        await connectionManager.ReconnectAsync(request.ServerId, context.CancellationToken);
        return await GetConnectionStatusAsync(request, context);
    }

    private static OpcServerModel MapToModel(ServerEntity e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        EndpointUrl = e.EndpointUrl,
        AuthMode = e.AuthMode,
        Username = e.Username,
        SecurityMode = e.SecurityMode,
        SecurityPolicy = e.SecurityPolicy,
        IsConnected = e.IsConnected,
        LastConnectedAt = e.LastConnectedAt,
    };

    private static string HashPassword(string password)
        => Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(password)));
}
