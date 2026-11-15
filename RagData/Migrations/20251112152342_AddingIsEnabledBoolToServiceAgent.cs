using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rag.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddingIsEnabledBoolToServiceAgent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "ServiceAgents",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "ServiceAgents");
        }
    }
}
