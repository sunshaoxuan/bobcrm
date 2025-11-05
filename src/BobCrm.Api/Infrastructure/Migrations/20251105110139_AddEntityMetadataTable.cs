using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BobCrm.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityMetadataTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EntityMetadata",
                columns: table => new
                {
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityName = table.Column<string>(type: "text", nullable: false),
                    EntityRoute = table.Column<string>(type: "text", nullable: false),
                    DisplayNameKey = table.Column<string>(type: "text", nullable: false),
                    DescriptionKey = table.Column<string>(type: "text", nullable: true),
                    ApiEndpoint = table.Column<string>(type: "text", nullable: false),
                    IsRootEntity = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Icon = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityMetadata", x => x.EntityType);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntityMetadata_IsRootEntity_IsEnabled_Order",
                table: "EntityMetadata",
                columns: new[] { "IsRootEntity", "IsEnabled", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntityMetadata");
        }
    }
}
