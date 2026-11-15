using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rag.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddinRelationInServiceAgentForLlmProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ServiceAgents_LlmProviderId",
                table: "ServiceAgents",
                column: "LlmProviderId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceAgents_LlmProviders_LlmProviderId",
                table: "ServiceAgents",
                column: "LlmProviderId",
                principalTable: "LlmProviders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceAgents_LlmProviders_LlmProviderId",
                table: "ServiceAgents");

            migrationBuilder.DropIndex(
                name: "IX_ServiceAgents_LlmProviderId",
                table: "ServiceAgents");
        }
    }
}
