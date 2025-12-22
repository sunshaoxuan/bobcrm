using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BobCrm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPolymorphicTemplateStateBinding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TemplateStateBindings_EntityType_ViewState_IsDefault",
                table: "TemplateStateBindings");

            migrationBuilder.DropIndex(
                name: "IX_TemplateStateBindings_TemplateId_ViewState",
                table: "TemplateStateBindings");

            migrationBuilder.AddColumn<string>(
                name: "MatchFieldName",
                table: "TemplateStateBindings",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchFieldValue",
                table: "TemplateStateBindings",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "TemplateStateBindings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateStateBindings_EntityType_ViewState",
                table: "TemplateStateBindings",
                columns: new[] { "EntityType", "ViewState" },
                unique: true,
                filter: "\"IsDefault\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateStateBindings_TemplateId_ViewState",
                table: "TemplateStateBindings",
                columns: new[] { "TemplateId", "ViewState" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TemplateStateBindings_EntityType_ViewState",
                table: "TemplateStateBindings");

            migrationBuilder.DropIndex(
                name: "IX_TemplateStateBindings_TemplateId_ViewState",
                table: "TemplateStateBindings");

            migrationBuilder.DropColumn(
                name: "MatchFieldName",
                table: "TemplateStateBindings");

            migrationBuilder.DropColumn(
                name: "MatchFieldValue",
                table: "TemplateStateBindings");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "TemplateStateBindings");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateStateBindings_EntityType_ViewState_IsDefault",
                table: "TemplateStateBindings",
                columns: new[] { "EntityType", "ViewState", "IsDefault" },
                filter: "\"IsDefault\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateStateBindings_TemplateId_ViewState",
                table: "TemplateStateBindings",
                columns: new[] { "TemplateId", "ViewState" },
                unique: true);
        }
    }
}
