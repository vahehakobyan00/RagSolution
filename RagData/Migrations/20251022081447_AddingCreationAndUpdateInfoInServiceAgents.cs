using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rag.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddingCreationAndUpdateInfoInServiceAgents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "ServiceAgents",
                newName: "CreatedByUserId");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreationDate",
                table: "ServiceAgents",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedDate",
                table: "ServiceAgents",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "ServiceAgents",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreationDate",
                table: "ServiceAgents");

            migrationBuilder.DropColumn(
                name: "LastModifiedDate",
                table: "ServiceAgents");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "ServiceAgents");

            migrationBuilder.RenameColumn(
                name: "CreatedByUserId",
                table: "ServiceAgents",
                newName: "UserId");
        }
    }
}
