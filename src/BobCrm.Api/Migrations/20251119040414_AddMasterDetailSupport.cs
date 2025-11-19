using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BobCrm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMasterDetailSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DetailDisplayMode",
                table: "FormTemplates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DetailRoute",
                table: "FormTemplates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LayoutMode",
                table: "FormTemplates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ModalSize",
                table: "FormTemplates",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DetailDisplayMode",
                table: "FormTemplates");

            migrationBuilder.DropColumn(
                name: "DetailRoute",
                table: "FormTemplates");

            migrationBuilder.DropColumn(
                name: "LayoutMode",
                table: "FormTemplates");

            migrationBuilder.DropColumn(
                name: "ModalSize",
                table: "FormTemplates");
        }
    }
}
