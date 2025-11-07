using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BobCrm.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityTypeAndFormTemplateTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "UserLayouts",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserLayouts_UserId_EntityType",
                table: "UserLayouts",
                columns: new[] { "UserId", "EntityType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserLayouts_UserId_EntityType",
                table: "UserLayouts");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "UserLayouts");
        }
    }
}
