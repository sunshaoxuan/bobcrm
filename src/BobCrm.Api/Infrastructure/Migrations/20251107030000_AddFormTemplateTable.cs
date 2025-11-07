using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BobCrm.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFormTemplateTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FormTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    IsUserDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystemDefault = table.Column<bool>(type: "boolean", nullable: false),
                    LayoutJson = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsInUse = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FormTemplates_UserId_EntityType",
                table: "FormTemplates",
                columns: new[] { "UserId", "EntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_FormTemplates_UserId_EntityType_IsUserDefault",
                table: "FormTemplates",
                columns: new[] { "UserId", "EntityType", "IsUserDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_FormTemplates_EntityType_IsSystemDefault",
                table: "FormTemplates",
                columns: new[] { "EntityType", "IsSystemDefault" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FormTemplates");
        }
    }
}
