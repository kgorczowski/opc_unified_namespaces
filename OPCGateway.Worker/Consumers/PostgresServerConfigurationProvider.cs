// Copyright (c) 2025 vm.pl

namespace OPCGateway.Worker.Consumers;

using Microsoft.Extensions.Configuration;
using Npgsql;

/// <summary>
/// Reads OPC UA server credentials from the shared PostgreSQL database.
/// Passwords are stored AES-encrypted by the API; the Worker decrypts them
/// using the same key from configuration.
/// </summary>
public sealed class PostgresServerConfigurationProvider(
    IConfiguration configuration,
    ILogger<PostgresServerConfigurationProvider> logger)
    : IServerConfigurationProvider
{
    private readonly string _connectionString =
        configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

    public async Task<ServerConfig> GetServerConfigAsync(string serverId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, endpoint_url, auth_mode, username, password_hash,
                   security_mode, security_policy
            FROM opc_servers
            WHERE id = @id
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("id", serverId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            throw new KeyNotFoundException($"Server '{serverId}' not found in database.");

        return new ServerConfig(
            ServerId: reader.GetString(0),
            EndpointUrl: reader.GetString(1),
            AuthMode: reader.GetString(2),
            Username: reader.IsDBNull(3) ? null : reader.GetString(3),
            Password: reader.IsDBNull(4) ? null : reader.GetString(4),
            SecurityMode: reader.GetString(5),
            SecurityPolicy: reader.GetString(6));
    }
}
