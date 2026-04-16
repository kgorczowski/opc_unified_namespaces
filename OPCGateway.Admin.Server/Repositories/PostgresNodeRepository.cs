// Copyright (c) 2025 vm.pl

namespace OPCGateway.Admin.Server.Repositories;

using Npgsql;
using OPCGateway.Admin.Server.Abstractions;
using OPCGateway.Admin.Server.Entities;

public sealed class PostgresNodeRepository(NpgsqlDataSource dataSource)
    : INodeRepository
{
    public async Task<(IReadOnlyList<NodeEntity> Items, int Total)> GetPagedAsync(
        string? serverId, bool? monitoringOnly, int page, int pageSize, CancellationToken ct = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);

        // Build dynamic WHERE clause
        var conditions = new List<string>();
        var paramIdx = 1;
        var countParams = new List<object?>();
        var dataParams = new List<object?>();

        if (serverId is not null)
        {
            conditions.Add($"server_id = ${paramIdx++}");
            countParams.Add(serverId);
            dataParams.Add(serverId);
        }

        if (monitoringOnly is not null)
        {
            conditions.Add($"monitoring_enabled = ${paramIdx++}");
            countParams.Add(monitoringOnly.Value);
            dataParams.Add(monitoringOnly.Value);
        }

        var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;

        // Count query
        await using var countCmd = conn.CreateCommand();
        countCmd.CommandText = $"SELECT COUNT(*) FROM managed_nodes {where}";
        foreach (var p in countParams) countCmd.Parameters.AddWithValue(p ?? DBNull.Value);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct));

        // Data query with paging
        var offsetParamIdx = paramIdx;
        var limitParamIdx = paramIdx + 1;
        dataParams.Add((page - 1) * pageSize);
        dataParams.Add(pageSize);

        await using var dataCmd = conn.CreateCommand();
        dataCmd.CommandText = $"""
            SELECT id, server_id, node_id, display_name, namespace_index,
                   data_type, monitoring_enabled, publishing_interval_ms,
                   description, last_value, last_value_at, tags
            FROM managed_nodes
            {where}
            ORDER BY display_name
            OFFSET ${offsetParamIdx} LIMIT ${limitParamIdx}
            """;
        foreach (var p in dataParams) dataCmd.Parameters.AddWithValue(p ?? DBNull.Value);

        await using var reader = await dataCmd.ExecuteReaderAsync(ct);
        var items = new List<NodeEntity>();
        while (await reader.ReadAsync(ct))
            items.Add(MapRow(reader));

        return (items, total);
    }

    public async Task<NodeEntity?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, server_id, node_id, display_name, namespace_index,
                   data_type, monitoring_enabled, publishing_interval_ms,
                   description, last_value, last_value_at, tags
            FROM managed_nodes WHERE id = $1
            """;
        cmd.Parameters.AddWithValue(id);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? MapRow(reader) : null;
    }

    public async Task<NodeEntity> AddAsync(NodeEntity entity, CancellationToken ct = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO managed_nodes
                (id, server_id, node_id, display_name, namespace_index,
                 monitoring_enabled, publishing_interval_ms, description, tags)
            VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)
            """;
        cmd.Parameters.AddWithValue(entity.Id);
        cmd.Parameters.AddWithValue(entity.ServerId);
        cmd.Parameters.AddWithValue(entity.NodeId);
        cmd.Parameters.AddWithValue(entity.DisplayName);
        cmd.Parameters.AddWithValue(entity.NamespaceIndex);
        cmd.Parameters.AddWithValue(entity.MonitoringEnabled);
        cmd.Parameters.AddWithValue(entity.PublishingIntervalMs);
        cmd.Parameters.AddWithValue((object?)entity.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)entity.Tags ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(NodeEntity entity, CancellationToken ct = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE managed_nodes
            SET display_name = $2,
                monitoring_enabled = $3,
                publishing_interval_ms = $4,
                description = $5,
                tags = $6,
                last_value = $7,
                last_value_at = $8
            WHERE id = $1
            """;
        cmd.Parameters.AddWithValue(entity.Id);
        cmd.Parameters.AddWithValue(entity.DisplayName);
        cmd.Parameters.AddWithValue(entity.MonitoringEnabled);
        cmd.Parameters.AddWithValue(entity.PublishingIntervalMs);
        cmd.Parameters.AddWithValue((object?)entity.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)entity.Tags ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)entity.LastValue ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)entity.LastValueAt ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM managed_nodes WHERE id = $1";
        cmd.Parameters.AddWithValue(id);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static NodeEntity MapRow(NpgsqlDataReader r) => new()
    {
        Id = r.GetString(0),
        ServerId = r.GetString(1),
        NodeId = r.GetString(2),
        DisplayName = r.GetString(3),
        NamespaceIndex = r.GetInt32(4),
        DataType = r.IsDBNull(5) ? null : r.GetString(5),
        MonitoringEnabled = r.GetBoolean(6),
        PublishingIntervalMs = r.GetInt32(7),
        Description = r.IsDBNull(8) ? null : r.GetString(8),
        LastValue = r.IsDBNull(9) ? null : r.GetString(9),
        LastValueAt = r.IsDBNull(10) ? null : r.GetDateTime(10),
        Tags = r.IsDBNull(11) ? null : r.GetString(11),
    };
}
