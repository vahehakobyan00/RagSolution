using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rag.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChunkIdAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ChunkId",
                table: "FileItems",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChunkId",
                table: "FileItems");
        }
    }
}
