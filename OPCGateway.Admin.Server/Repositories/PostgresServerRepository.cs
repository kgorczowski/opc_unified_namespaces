// Copyright (c) 2025 vm.pl

namespace OPCGateway.Admin.Server.Repositories;

using Npgsql;
using OPCGateway.Admin.Server.Abstractions;
using OPCGateway.Admin.Server.Entities;

/// <summary>
/// Npgsql-based repository for OPC UA server records.
/// Uses raw SQL for minimal latency in hot gRPC paths.
/// </summary>
public sealed class PostgresServerRepository(NpgsqlDataSource dataSource)
    : IServerRepository
{
    public async Task<IReadOnlyList<ServerEntity>> GetAllAsync(CancellationToken ct = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, name, endpoint_url, auth_mode, username, password_hash,
                   security_mode, security_policy, is_connected, last_connected_at
            FROM opc_servers
            ORDER BY name
            """;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var results = new List<ServerEntity>();
        while (await reader.ReadAsync(ct))
            results.Add(MapRow(reader));

        return results;
    }

    public async Task<ServerEntity?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, name, endpoint_url, auth_mode, username, password_hash,
                   security_mode, security_policy, is_connected, last_connected_at
            FROM opc_servers WHERE id = $1
            """;
        cmd.Parameters.AddWithValue(id);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? MapRow(reader) : null;
    }

    public async Task<ServerEntity> AddAsync(ServerEntity entity, CancellationToken ct = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO opc_servers
                (id, name, endpoint_url, auth_mode, username, password_hash,
                 security_mode, security_policy, is_connected)
            VALUES ($1, $2, $3, $4, $5, $6, $7, $8, false)
            """;
        cmd.Parameters.AddWithValue(entity.Id);
        cmd.Parameters.AddWithValue(entity.Name);
        cmd.Parameters.AddWithValue(entity.EndpointUrl);
        cmd.Parameters.AddWithValue(entity.AuthMode);
        cmd.Parameters.AddWithValue((object?)entity.Username ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)entity.PasswordHash ?? DBNull.Value);
        cmd.Parameters.AddWithValue(entity.SecurityMode);
        cmd.Parameters.AddWithValue(entity.SecurityPolicy);

        await cmd.ExecuteNonQueryAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(ServerEntity entity, CancellationToken ct = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE opc_servers
            SET name = $2,
                endpoint_url = $3,
                security_mode = $4,
                security_policy = $5,
                is_connected = $6,
                last_connected_at = $7
            WHERE id = $1
            """;
        cmd.Parameters.AddWithValue(entity.Id);
        cmd.Parameters.AddWithValue(entity.Name);
        cmd.Parameters.AddWithValue(entity.EndpointUrl);
        cmd.Parameters.AddWithValue(entity.SecurityMode);
        cmd.Parameters.AddWithValue(entity.SecurityPolicy);
        cmd.Parameters.AddWithValue(entity.IsConnected);
        cmd.Parameters.AddWithValue((object?)entity.LastConnectedAt ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM opc_servers WHERE id = $1";
        cmd.Parameters.AddWithValue(id);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static ServerEntity MapRow(NpgsqlDataReader r) => new()
    {
        Id = r.GetString(0),
        Name = r.GetString(1),
        EndpointUrl = r.GetString(2),
        AuthMode = r.GetString(3),
        Username = r.IsDBNull(4) ? null : r.GetString(4),
        PasswordHash = r.IsDBNull(5) ? null : r.GetString(5),
        SecurityMode = r.GetString(6),
        SecurityPolicy = r.GetString(7),
        IsConnected = r.GetBoolean(8),
        LastConnectedAt = r.IsDBNull(9) ? null : r.GetDateTime(9),
    };
}
