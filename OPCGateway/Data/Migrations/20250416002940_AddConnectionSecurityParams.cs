using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OPCGateway.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConnectionSecurityParams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "ConnectionParameters",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Password",
                table: "ConnectionParameters",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "Authentication",
                table: "ConnectionParameters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CertificatePassword",
                table: "ConnectionParameters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificatePath",
                table: "ConnectionParameters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecurityMode",
                table: "ConnectionParameters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecurityPolicy",
                table: "ConnectionParameters",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Authentication",
                table: "ConnectionParameters");

            migrationBuilder.DropColumn(
                name: "CertificatePassword",
                table: "ConnectionParameters");

            migrationBuilder.DropColumn(
                name: "CertificatePath",
                table: "ConnectionParameters");

            migrationBuilder.DropColumn(
                name: "SecurityMode",
                table: "ConnectionParameters");

            migrationBuilder.DropColumn(
                name: "SecurityPolicy",
                table: "ConnectionParameters");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "ConnectionParameters",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Password",
                table: "ConnectionParameters",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
