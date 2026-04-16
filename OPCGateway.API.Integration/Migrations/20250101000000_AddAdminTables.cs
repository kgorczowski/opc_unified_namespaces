// Copyright (c) 2025 vm.pl
// Migration: 20250101000000_AddAdminTables
// Add to OPCGateway/Migrations/ or run via:
//   dotnet ef migrations add AddAdminTables --project OPCGateway

namespace OPCGateway.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class AddAdminTables : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // opc_servers – managed server registry (used by Admin gRPC + Worker)
        migrationBuilder.CreateTable(
            name: "opc_servers",
            columns: table => new
            {
                id = table.Column<string>(maxLength: 36, nullable: false),
                name = table.Column<string>(maxLength: 200, nullable: false),
                endpoint_url = table.Column<string>(maxLength: 500, nullable: false),
                auth_mode = table.Column<string>(maxLength: 50, nullable: false, defaultValue: "Anonymous"),
                username = table.Column<string>(maxLength: 200, nullable: true),
                password_hash = table.Column<string>(maxLength: 500, nullable: true),
                security_mode = table.Column<string>(maxLength: 50, nullable: false, defaultValue: "Auto"),
                security_policy = table.Column<string>(maxLength: 200, nullable: false, defaultValue: "Auto"),
                is_connected = table.Column<bool>(nullable: false, defaultValue: false),
                last_connected_at = table.Column<DateTime>(nullable: true),
                created_at = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                updated_at = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
            },
            constraints: table => table.PrimaryKey("pk_opc_servers", x => x.id));

        migrationBuilder.CreateIndex(
            name: "ix_opc_servers_endpoint_url",
            table: "opc_servers",
            column: "endpoint_url");

        // managed_nodes – nodes tracked by the gateway
        migrationBuilder.CreateTable(
            name: "managed_nodes",
            columns: table => new
            {
                id = table.Column<string>(maxLength: 36, nullable: false),
                server_id = table.Column<string>(maxLength: 36, nullable: false),
                node_id = table.Column<string>(maxLength: 500, nullable: false),
                display_name = table.Column<string>(maxLength: 200, nullable: false),
                namespace_index = table.Column<int>(nullable: false),
                data_type = table.Column<string>(maxLength: 100, nullable: true),
                monitoring_enabled = table.Column<bool>(nullable: false, defaultValue: false),
                publishing_interval_ms = table.Column<int>(nullable: false, defaultValue: 500),
                description = table.Column<string>(maxLength: 1000, nullable: true),
                last_value = table.Column<string>(maxLength: 500, nullable: true),
                last_value_at = table.Column<DateTime>(nullable: true),
                tags = table.Column<string>(maxLength: 1000, nullable: true),
                created_at = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                updated_at = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_managed_nodes", x => x.id);
                table.ForeignKey(
                    name: "fk_managed_nodes_opc_servers",
                    column: x => x.server_id,
                    principalTable: "opc_servers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_managed_nodes_server_id",
            table: "managed_nodes",
            column: "server_id");

        migrationBuilder.CreateIndex(
            name: "ix_managed_nodes_monitoring",
            table: "managed_nodes",
            columns: new[] { "server_id", "monitoring_enabled" });

        // Trigger to keep updated_at current
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION update_updated_at_column()
            RETURNS TRIGGER AS $$
            BEGIN
                NEW.updated_at = NOW();
                RETURN NEW;
            END;
            $$ language 'plpgsql';

            CREATE TRIGGER update_opc_servers_updated_at
                BEFORE UPDATE ON opc_servers
                FOR EACH ROW EXECUTE PROCEDURE update_updated_at_column();

            CREATE TRIGGER update_managed_nodes_updated_at
                BEFORE UPDATE ON managed_nodes
                FOR EACH ROW EXECUTE PROCEDURE update_updated_at_column();
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TRIGGER IF EXISTS update_managed_nodes_updated_at ON managed_nodes;
            DROP TRIGGER IF EXISTS update_opc_servers_updated_at ON opc_servers;
            DROP FUNCTION IF EXISTS update_updated_at_column();
            """);

        migrationBuilder.DropTable(name: "managed_nodes");
        migrationBuilder.DropTable(name: "opc_servers");
    }
}
