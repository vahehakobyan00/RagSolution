using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Rag.Data.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    SessionId = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    MetaType = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: true),
                    ChunkId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeBaseCategories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "text", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeBaseCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeBases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ServiceAgentId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "text", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeBases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LlmProviders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LlmProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantTables",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Seats = table.Column<int>(type: "integer", nullable: false),
                    Location = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantTables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelegramConversations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                    TelegramUserGuid = table.Column<string>(type: "text", nullable: false),
                    SessionId = table.Column<string>(type: "text", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramConversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Turns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConversationId = table.Column<long>(type: "bigint", nullable: false),
                    Index = table.Column<int>(type: "integer", nullable: false),
                    Question = table.Column<string>(type: "text", nullable: false),
                    Answer = table.Column<string>(type: "text", nullable: false),
                    RetrievedChunkIds = table.Column<string[]>(type: "text[]", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    QuestionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AnswerDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Turns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Turns_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeBaseDocuments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "text", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DocId = table.Column<string>(type: "text", nullable: true),
                    VectorStoreSyncDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FileItemId = table.Column<long>(type: "bigint", nullable: false),
                    KnowledgeBaseId = table.Column<long>(type: "bigint", nullable: false),
                    KnowledgeBaseCategoryId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeBaseDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeBaseDocuments_FileItems_FileItemId",
                        column: x => x.FileItemId,
                        principalTable: "FileItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KnowledgeBaseDocuments_KnowledgeBaseCategories_KnowledgeBas~",
                        column: x => x.KnowledgeBaseCategoryId,
                        principalTable: "KnowledgeBaseCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KnowledgeBaseDocuments_KnowledgeBases_KnowledgeBaseId",
                        column: x => x.KnowledgeBaseId,
                        principalTable: "KnowledgeBases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceAgents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    APIToken = table.Column<string>(type: "text", nullable: false),
                    LlmProviderId = table.Column<int>(type: "integer", nullable: false),
                    ModelName = table.Column<string>(type: "text", nullable: false),
                    SystemPrompt = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "text", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceAgents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceAgents_LlmProviders_LlmProviderId",
                        column: x => x.LlmProviderId,
                        principalTable: "LlmProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TableId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Guests = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookings_RestaurantTables_TableId",
                        column: x => x.TableId,
                        principalTable: "RestaurantTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Feedbacks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TurnId = table.Column<long>(type: "bigint", nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false),
                    SetUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Feedbacks_Turns_TurnId",
                        column: x => x.TurnId,
                        principalTable: "Turns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "LlmProviders",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Open AI" },
                    { 2, "Antropic" },
                    { 3, "GemenAI" },
                    { 4, "Deepseek" }
                });

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "Category", "Description", "IsAvailable", "Name", "Price" },
                values: new object[,]
                {
                    { 1L, "Salads", "Tomatoes, cucumbers, sweet peppers, fresh greens and sunflower oil", true, "Armenian seasonal salad", 2200m },
                    { 2L, "Salads", "Romaine, grilled chicken, parmesan, croutons, Caesar dressing", true, "Caesar salad with chicken", 3200m },
                    { 3L, "Salads", "Bulgur with tomato paste, peppers and herbs", true, "Eetch", 1800m },
                    { 4L, "Soups", "Warm yogurt soup with wheat and herbs", true, "Spas", 1600m },
                    { 5L, "Soups", "Traditional beef soup served with garlic, lavash and radish (served until noon)", true, "Khash", 3800m },
                    { 6L, "Soups", "Cold summer soup with kefir and vegetables", false, "Okroshka", 1700m },
                    { 7L, "Grill", "Charcoal-grilled pork with onions and lavash", true, "Pork khorovats", 4500m },
                    { 8L, "Grill", "Charcoal-grilled lamb ribs", true, "Lamb khorovats", 5200m },
                    { 9L, "Grill", "Minced chicken kebab with grilled vegetables", true, "Chicken lula kebab", 3600m },
                    { 10L, "Main courses", "Minced meat and rice wrapped in grape leaves, with matsun", true, "Tolma", 3400m },
                    { 11L, "Main courses", "Slow-cooked wheat and chicken porridge", true, "Harissa", 3000m },
                    { 12L, "Main courses", "Whole trout with lemon and tarragon", true, "Grilled Sevan trout", 5600m },
                    { 13L, "Desserts", "Sweet Armenian pastry", true, "Gata", 1200m },
                    { 14L, "Desserts", "Layered pastry with walnuts and honey", true, "Pakhlava", 1500m },
                    { 15L, "Drinks", "Traditional coffee brewed in a jazzve", true, "Armenian coffee", 800m }
                });

            migrationBuilder.InsertData(
                table: "RestaurantTables",
                columns: new[] { "Id", "Location", "Number", "Seats" },
                values: new object[,]
                {
                    { 1L, "hall", 1, 2 },
                    { 2L, "hall", 2, 2 },
                    { 3L, "hall", 3, 4 },
                    { 4L, "hall", 4, 4 },
                    { 5L, "hall", 5, 6 },
                    { 6L, "terrace", 6, 2 },
                    { 7L, "terrace", 7, 4 },
                    { 8L, "terrace", 8, 4 },
                    { 9L, "VIP", 9, 8 },
                    { 10L, "VIP", 10, 12 }
                });

            migrationBuilder.InsertData(
                table: "Bookings",
                columns: new[] { "Id", "CreatedAt", "CustomerName", "EndTime", "Guests", "Notes", "Phone", "StartTime", "Status", "TableId" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 11, 15, 8, 0, 0, 0, DateTimeKind.Unspecified), "Anna Petrosyan", new DateTime(2026, 11, 20, 21, 0, 0, 0, DateTimeKind.Unspecified), 4, null, "+37491123456", new DateTime(2026, 11, 20, 19, 0, 0, 0, DateTimeKind.Unspecified), "Confirmed", 3L },
                    { 2L, new DateTime(2026, 11, 15, 8, 0, 0, 0, DateTimeKind.Unspecified), "David Sargsyan", new DateTime(2026, 11, 20, 22, 0, 0, 0, DateTimeKind.Unspecified), 7, "Birthday, needs a cake plate", "+37477234567", new DateTime(2026, 11, 20, 20, 0, 0, 0, DateTimeKind.Unspecified), "Confirmed", 9L },
                    { 3L, new DateTime(2026, 11, 15, 8, 0, 0, 0, DateTimeKind.Unspecified), "Elena Ivanova", new DateTime(2026, 11, 21, 15, 0, 0, 0, DateTimeKind.Unspecified), 2, "Terrace if possible", "+37455345678", new DateTime(2026, 11, 21, 13, 0, 0, 0, DateTimeKind.Unspecified), "Confirmed", 6L },
                    { 4L, new DateTime(2026, 11, 15, 8, 0, 0, 0, DateTimeKind.Unspecified), "John Smith", new DateTime(2026, 11, 19, 20, 30, 0, 0, DateTimeKind.Unspecified), 5, null, "+37493456789", new DateTime(2026, 11, 19, 18, 30, 0, 0, DateTimeKind.Unspecified), "Cancelled", 5L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Phone",
                table: "Bookings",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TableId_StartTime",
                table: "Bookings",
                columns: new[] { "TableId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_TurnId",
                table: "Feedbacks",
                column: "TurnId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseDocuments_FileItemId",
                table: "KnowledgeBaseDocuments",
                column: "FileItemId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseDocuments_KnowledgeBaseCategoryId",
                table: "KnowledgeBaseDocuments",
                column: "KnowledgeBaseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseDocuments_KnowledgeBaseId",
                table: "KnowledgeBaseDocuments",
                column: "KnowledgeBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_Category",
                table: "MenuItems",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTables_Number",
                table: "RestaurantTables",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceAgents_LlmProviderId",
                table: "ServiceAgents",
                column: "LlmProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Turns_ConversationId",
                table: "Turns",
                column: "ConversationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "Feedbacks");

            migrationBuilder.DropTable(
                name: "KnowledgeBaseDocuments");

            migrationBuilder.DropTable(
                name: "MenuItems");

            migrationBuilder.DropTable(
                name: "ServiceAgents");

            migrationBuilder.DropTable(
                name: "TelegramConversations");

            migrationBuilder.DropTable(
                name: "RestaurantTables");

            migrationBuilder.DropTable(
                name: "Turns");

            migrationBuilder.DropTable(
                name: "FileItems");

            migrationBuilder.DropTable(
                name: "KnowledgeBaseCategories");

            migrationBuilder.DropTable(
                name: "KnowledgeBases");

            migrationBuilder.DropTable(
                name: "LlmProviders");

            migrationBuilder.DropTable(
                name: "Conversations");
        }
    }
}
