using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BobCrm.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropEntityMetadataTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 删除EntityMetadata表（已被EntityDefinition替代）
            migrationBuilder.DropTable(
                name: "EntityMetadata");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 重新创建EntityMetadata表（用于回滚）
            migrationBuilder.CreateTable(
                name: "EntityMetadata",
                columns: table => new
                {
                    EntityType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EntityName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityRoute = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayNameKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DescriptionKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApiEndpoint = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsRootEntity = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EntitySource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    SourceDefinitionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityMetadata", x => x.EntityType);
                    table.ForeignKey(
                        name: "FK_EntityMetadata_EntityDefinitions_SourceDefinitionId",
                        column: x => x.SourceDefinitionId,
                        principalTable: "EntityDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntityMetadata_EntitySource",
                table: "EntityMetadata",
                column: "EntitySource");

            migrationBuilder.CreateIndex(
                name: "IX_EntityMetadata_IsRootEntity_IsEnabled_Order",
                table: "EntityMetadata",
                columns: new[] { "IsRootEntity", "IsEnabled", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_EntityMetadata_SourceDefinitionId",
                table: "EntityMetadata",
                column: "SourceDefinitionId");
        }
    }
}
