using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LlmApiUrl",
                table: "SystemConfigs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LlmModelName",
                table: "SystemConfigs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LlmApiUrl",
                table: "SystemConfigs");

            migrationBuilder.DropColumn(
                name: "LlmModelName",
                table: "SystemConfigs");
        }
    }
}
