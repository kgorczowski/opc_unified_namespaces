// Copyright (c) 2025 vm.pl

namespace OPCGateway.Worker.Consumers;

using System.Collections.Concurrent;
using System.Text.Json;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

/// <summary>
/// Maintains one <see cref="Session"/> per OPC UA server endpoint.
/// Sessions are created on first use and automatically reconnected after failure.
/// Thread-safe for concurrent reads/writes from multiple consumer tasks.
/// </summary>
public sealed class OpcSessionPool(
    IServerConfigurationProvider configProvider,
    ILogger<OpcSessionPool> logger)
    : IOpcSessionPool, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly ConcurrentDictionary<string, IWorkerOpcSession> _sessions = new();

    public async Task<IWorkerOpcSession> GetOrCreateAsync(string serverId, CancellationToken ct = default)
    {
        if (_sessions.TryGetValue(serverId, out var existing))
            return existing;

        var gate = _locks.GetOrAdd(serverId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            if (_sessions.TryGetValue(serverId, out existing))
                return existing;

            var config = await configProvider.GetServerConfigAsync(serverId, ct);
            var session = await WorkerOpcSession.CreateAsync(config, logger, ct);
            _sessions[serverId] = session;

            logger.LogInformation("OPC UA session created for server {ServerId} ({Endpoint})",
                serverId, config.EndpointUrl);

            return session;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task DisposeSessionAsync(string serverId)
    {
        if (_sessions.TryRemove(serverId, out var session) && session is IAsyncDisposable d)
            await d.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var key in _sessions.Keys)
            await DisposeSessionAsync(key);
    }
}

/// <summary>OPC UA session wrapper that auto-reconnects on fault.</summary>
internal sealed class WorkerOpcSession(Session session, ILogger logger)
    : IWorkerOpcSession, IAsyncDisposable
{
    private readonly SemaphoreSlim _reconnectLock = new(1, 1);
    private SessionReconnectHandler? _reconnectHandler;

    public static async Task<WorkerOpcSession> CreateAsync(
        ServerConfig config,
        ILogger logger,
        CancellationToken ct)
    {
        var appConfig = BuildApplicationConfig();
        await appConfig.Validate(ApplicationType.Client);

        var endpoint = CoreClientUtils.SelectEndpoint(
            appConfig, config.EndpointUrl, useSecurity: false);

        var session = await Session.Create(
            appConfig,
            new ConfiguredEndpoint(null, endpoint, EndpointConfiguration.Create(appConfig)),
            updateBeforeConnect: true,
            sessionName: $"OPCGateway.Worker-{config.ServerId}",
            sessionTimeout: 60_000,
            identity: BuildIdentity(config),
            preferredLocales: null,
            ct: ct);

        var wrapper = new WorkerOpcSession(session, logger);
        session.KeepAlive += wrapper.OnKeepAlive;
        return wrapper;
    }

    public async Task WriteAsync(
        string nodeId, int namespaceIndex, string valueJson, string dataType, CancellationToken ct = default)
    {
        var value = DeserializeValue(valueJson, dataType);
        var nodesToWrite = new WriteValueCollection
        {
            new WriteValue
            {
                NodeId = new NodeId(nodeId, (ushort)namespaceIndex),
                AttributeId = Attributes.Value,
                Value = new DataValue(new Variant(value)),
            },
        };

        var response = await session.WriteAsync(
            requestHeader: null, nodesToWrite, ct);

        ClientBase.ValidateResponse(response.Results, nodesToWrite);

        if (StatusCode.IsBad(response.Results[0]))
            throw new InvalidOperationException(
                $"OPC UA Write failed: {response.Results[0]}");
    }

    public async Task<OpcReadResult> ReadAsync(
        string nodeId, int namespaceIndex, CancellationToken ct = default)
    {
        var nodesToRead = new ReadValueIdCollection
        {
            new ReadValueId
            {
                NodeId = new NodeId(nodeId, (ushort)namespaceIndex),
                AttributeId = Attributes.Value,
            },
        };

        var response = await session.ReadAsync(
            requestHeader: null,
            maxAge: 0,
            timestampsToReturn: TimestampsToReturn.Both,
            nodesToRead,
            ct: ct);

        ClientBase.ValidateResponse(response.Results, nodesToRead);

        var dv = response.Results[0];
        return new OpcReadResult(
            Value: dv.Value?.ToString(),
            DataType: dv.Value?.GetType().Name,
            StatusCode: StatusCode.IsGood(dv.StatusCode) ? "Good" : dv.StatusCode.ToString());
    }

    private void OnKeepAlive(ISession s, KeepAliveEventArgs e)
    {
        if (!ServiceResult.IsBad(e.Status))
            return;

        logger.LogWarning("OPC UA keep-alive failed – starting reconnect handler");

        if (_reconnectHandler is null)
        {
            _reconnectHandler = new SessionReconnectHandler(reconnectAbort: true);
            _reconnectHandler.BeginReconnect(s, 5_000, null);
        }
    }

    private static ApplicationConfiguration BuildApplicationConfig()
    {
        var config = new ApplicationConfiguration
        {
            ApplicationName = "OPCGateway.Worker",
            ApplicationUri = "urn:opcgateway:worker",
            ApplicationType = ApplicationType.Client,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier(),
                AutoAcceptUntrustedCertificates = true,
            },
            TransportConfigurations = [],
            TransportQuotas = new TransportQuotas { OperationTimeout = 30_000 },
            ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60_000 },
        };
        return config;
    }

    private static IUserIdentity BuildIdentity(ServerConfig config) =>
        config.AuthMode switch
        {
            "UsernamePassword" when config.Username is not null =>
                new UserIdentity(config.Username, config.Password ?? string.Empty),
            _ => new UserIdentity(new AnonymousIdentityToken()),
        };

    private static object? DeserializeValue(string json, string dataType)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return dataType switch
        {
            "Float" or "float" => root.GetSingle(),
            "Double" or "double" => root.GetDouble(),
            "Int32" or "int" => root.GetInt32(),
            "Int64" or "long" => root.GetInt64(),
            "Boolean" or "bool" => root.GetBoolean(),
            "String" or "string" => root.GetString(),
            _ => root.GetRawText(),
        };
    }

    public async ValueTask DisposeAsync()
    {
        _reconnectHandler?.Dispose();
        if (!session.Disposed)
            await session.CloseAsync();

        session.Dispose();
    }
}
