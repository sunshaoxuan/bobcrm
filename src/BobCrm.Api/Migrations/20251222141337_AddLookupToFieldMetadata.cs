using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BobCrm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLookupToFieldMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ForeignKeyAction",
                table: "FieldMetadatas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LookupDisplayField",
                table: "FieldMetadatas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LookupEntityName",
                table: "FieldMetadatas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ForeignKeyAction",
                table: "FieldMetadatas");

            migrationBuilder.DropColumn(
                name: "LookupDisplayField",
                table: "FieldMetadatas");

            migrationBuilder.DropColumn(
                name: "LookupEntityName",
                table: "FieldMetadatas");
        }
    }
}
