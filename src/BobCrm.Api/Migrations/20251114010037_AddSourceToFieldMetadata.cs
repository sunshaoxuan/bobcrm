using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BobCrm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceToFieldMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TemplateBindingId",
                table: "RoleFunctionPermissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequiredFunctionCode",
                table: "FormTemplates",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "FormTemplates",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsageType",
                table: "FormTemplates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "FieldMetadatas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TemplateBindings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UsageType = table.Column<int>(type: "integer", nullable: false),
                    TemplateId = table.Column<int>(type: "integer", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    RequiredFunctionCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateBindings_FormTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "FormTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleFunctionPermissions_TemplateBindingId",
                table: "RoleFunctionPermissions",
                column: "TemplateBindingId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateBindings_EntityType_UsageType_IsSystem",
                table: "TemplateBindings",
                columns: new[] { "EntityType", "UsageType", "IsSystem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateBindings_TemplateId",
                table: "TemplateBindings",
                column: "TemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_RoleFunctionPermissions_TemplateBindings_TemplateBindingId",
                table: "RoleFunctionPermissions",
                column: "TemplateBindingId",
                principalTable: "TemplateBindings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoleFunctionPermissions_TemplateBindings_TemplateBindingId",
                table: "RoleFunctionPermissions");

            migrationBuilder.DropTable(
                name: "TemplateBindings");

            migrationBuilder.DropIndex(
                name: "IX_RoleFunctionPermissions_TemplateBindingId",
                table: "RoleFunctionPermissions");

            migrationBuilder.DropColumn(
                name: "TemplateBindingId",
                table: "RoleFunctionPermissions");

            migrationBuilder.DropColumn(
                name: "RequiredFunctionCode",
                table: "FormTemplates");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "FormTemplates");

            migrationBuilder.DropColumn(
                name: "UsageType",
                table: "FormTemplates");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "FieldMetadatas");
        }
    }
}
