using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BobCrm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicEnumSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EnumDefinitionId",
                table: "FieldMetadatas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMultiSelect",
                table: "FieldMetadatas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "EnumDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "jsonb", nullable: false),
                    Description = table.Column<string>(type: "jsonb", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnumDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnumOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnumDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "jsonb", nullable: false),
                    Description = table.Column<string>(type: "jsonb", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ColorTag = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Icon = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnumOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnumOptions_EnumDefinitions_EnumDefinitionId",
                        column: x => x.EnumDefinitionId,
                        principalTable: "EnumDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FieldMetadatas_EnumDefinitionId",
                table: "FieldMetadatas",
                column: "EnumDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_EnumDefinitions_Code",
                table: "EnumDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnumDefinitions_IsEnabled",
                table: "EnumDefinitions",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_EnumDefinitions_IsSystem",
                table: "EnumDefinitions",
                column: "IsSystem");

            migrationBuilder.CreateIndex(
                name: "IX_EnumOptions_EnumDefinitionId",
                table: "EnumOptions",
                column: "EnumDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_EnumOptions_EnumDefinitionId_SortOrder",
                table: "EnumOptions",
                columns: new[] { "EnumDefinitionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_EnumOptions_EnumDefinitionId_Value",
                table: "EnumOptions",
                columns: new[] { "EnumDefinitionId", "Value" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldMetadatas_EnumDefinitions_EnumDefinitionId",
                table: "FieldMetadatas",
                column: "EnumDefinitionId",
                principalTable: "EnumDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FieldMetadatas_EnumDefinitions_EnumDefinitionId",
                table: "FieldMetadatas");

            migrationBuilder.DropTable(
                name: "EnumOptions");

            migrationBuilder.DropTable(
                name: "EnumDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_FieldMetadatas_EnumDefinitionId",
                table: "FieldMetadatas");

            migrationBuilder.DropColumn(
                name: "EnumDefinitionId",
                table: "FieldMetadatas");

            migrationBuilder.DropColumn(
                name: "IsMultiSelect",
                table: "FieldMetadatas");
        }
    }
}
