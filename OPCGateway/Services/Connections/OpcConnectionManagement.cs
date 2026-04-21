using Opc.Ua;
using OPCGateway.Controllers;
using OPCGateway.Data.Entities;
using OPCGateway.Data.Repositories;
using ISession = Opc.Ua.Client.ISession;
using StatusCodes = Opc.Ua.StatusCodes;

namespace OPCGateway.Services.Connections;

public class OpcConnectionManagement(IConnectionRepository repository, IOpcSessionManager sessionManager, IOpcSessionFactory sessionFactory, ILogger<IOpcConnectionManagement> logger) : IOpcConnectionManagement
{
    public async Task<string> ConnectAsync(string endpointUrl, string? username, string? password, string? connectionId, SecurityMode? securityMode, SecurityPolicy? securityPolicy, UserTokenType authentication, string? certificatePath, string? certificatePassword)
    {
        connectionId ??= Guid.NewGuid().ToString();

        sessionManager.RemoveSession(connectionId);

        try
        {
            var connectionParameters = new ConnectionParameters
            {
                ConnectionId = connectionId,
                EndpointUrl = endpointUrl,
                Username = username,
                Password = password,
                SecurityMode = securityMode,
                SecurityPolicy = securityPolicy,
                Authentication = authentication,
                CertificatePath = certificatePath,
                CertificatePassword = certificatePassword,
            };

            var session = await sessionFactory.CreateSessionAsync(endpointUrl, username, password, securityMode, securityPolicy, authentication, certificatePath, certificatePassword);
            sessionManager.AddSession(connectionId, session, connectionParameters);

            // Save connection parameters only if the connection is new
            if (await repository.LoadConnectionParametersAsync(connectionId) is null)
            {
                await repository.SaveConnectionParametersAsync(connectionParameters);
            }
        }
        catch (ServiceResultException ex) when (ex.StatusCode == StatusCodes.BadUserAccessDenied)
        {
            sessionManager.RemoveSession(connectionId);
            throw new UnauthorizedAccessException("Authentication failed. Please check your credentials.", ex);
        }
        catch (ServiceResultException ex)
        {
            sessionManager.RemoveSession(connectionId);
            logger.LogWarning("OPC UA ServiceResultException with StatusCode: {StatusCode}. Endpoint: {EndpointUrl}, ConnectionId: {ConnectionId}", ex.StatusCode, endpointUrl, connectionId);
            throw new InvalidOperationException($"OPC UA connection failed with status code: {StatusCodes.GetBrowseName(ex.StatusCode)}", ex);
        }
        catch (Exception)
        {
            sessionManager.RemoveSession(connectionId);

            logger.LogWarning("Failed to connect to the OPC UA server. Endpoint: {EndpointUrl}, ConnectionId: {ConnectionId}", endpointUrl, connectionId);
            throw;
        }

        return connectionId;
    }

    public async Task<string> ReconnectAsync(string connectionId)
    {
        var connection = await repository.LoadConnectionParametersAsync(connectionId);

        if (connection == null)
        {
            throw new KeyNotFoundException("Connection ID not found.");
        }

        if (sessionManager.GetConnectionStatus(connectionId) == ConnectionStatus.Connected)
        {
            return connectionId;
        }

        if (sessionManager.GetConnectionStatus(connectionId) == ConnectionStatus.NotConnected)
        {
            await ConnectAsync(connection.EndpointUrl, connection.Username, connection.Password, connectionId, connection.SecurityMode, connection.SecurityPolicy, connection.Authentication, connection.CertificatePath, connection.CertificatePassword);
            return connectionId;
        }

        throw new InvalidOperationException("Connection ID exists but is in an unknown state.");
    }

    public void Disconnect(string connectionId)
    {
        if (sessionManager.GetConnectionStatus(connectionId) == ConnectionStatus.Connected)
        {
            var session = sessionManager.GetSession(connectionId);
            session.Close();
            sessionManager.RemoveSession(connectionId);
        }
    }

    public async Task CheckConnection(string connectionId)
    {
        var connectionStatus = sessionManager.GetConnectionStatus(connectionId);

        if (connectionStatus == ConnectionStatus.Connected)
        {
            return;
        }

        if (connectionStatus == ConnectionStatus.Reconnecting)
        {
            logger.LogInformation("Connection {ConnectionId} is reconnecting.", connectionId);
            throw new InvalidOperationException($"Connection {connectionId} is reconnecting.");
        }

        var connection = await repository.LoadConnectionParametersAsync(connectionId);

        if (connection == null)
        {
            throw new KeyNotFoundException("Connection not found.");
        }

        await ConnectAsync(connection.EndpointUrl, connection.Username, connection.Password, connectionId, connection.SecurityMode, connection.SecurityPolicy, connection.Authentication, connection.CertificatePath, connection.CertificatePassword);
    }

    public ISession GetSession(string connectionId)
    {
        return sessionManager.GetSession(connectionId);
    }

    public ConnectionStatus GetConnectionStatus(string connectionId)
    {
        return sessionManager.GetConnectionStatus(connectionId);
    }
}
