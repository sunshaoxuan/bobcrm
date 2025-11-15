using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BobCrm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFunctionNodeMultilingual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "FunctionNodes",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayNameKey",
                table: "FunctionNodes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TemplateBindingId",
                table: "FunctionNodes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FunctionNodes_TemplateBindingId",
                table: "FunctionNodes",
                column: "TemplateBindingId");

            migrationBuilder.AddForeignKey(
                name: "FK_FunctionNodes_TemplateBindings_TemplateBindingId",
                table: "FunctionNodes",
                column: "TemplateBindingId",
                principalTable: "TemplateBindings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FunctionNodes_TemplateBindings_TemplateBindingId",
                table: "FunctionNodes");

            migrationBuilder.DropIndex(
                name: "IX_FunctionNodes_TemplateBindingId",
                table: "FunctionNodes");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "FunctionNodes");

            migrationBuilder.DropColumn(
                name: "DisplayNameKey",
                table: "FunctionNodes");

            migrationBuilder.DropColumn(
                name: "TemplateBindingId",
                table: "FunctionNodes");
        }
    }
}
