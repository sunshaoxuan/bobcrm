using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BobCrm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplateStateBindingToFunctionNode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TemplateStateBindingId",
                table: "FunctionNodes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystem",
                table: "EnumOptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "FieldPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FieldName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CanRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CanWrite = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Remarks = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldPermissions_RoleProfiles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "RoleProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateStateBindings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ViewState = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TemplateId = table.Column<int>(type: "integer", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    RequiredPermission = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateStateBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateStateBindings_FormTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "FormTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FunctionNodes_TemplateStateBindingId",
                table: "FunctionNodes",
                column: "TemplateStateBindingId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldPermissions_Role_Entity_Field",
                table: "FieldPermissions",
                columns: new[] { "RoleId", "EntityType", "FieldName" },
                unique: true);

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

            migrationBuilder.AddForeignKey(
                name: "FK_FunctionNodes_TemplateStateBindings_TemplateStateBindingId",
                table: "FunctionNodes",
                column: "TemplateStateBindingId",
                principalTable: "TemplateStateBindings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FunctionNodes_TemplateStateBindings_TemplateStateBindingId",
                table: "FunctionNodes");

            migrationBuilder.DropTable(
                name: "FieldPermissions");

            migrationBuilder.DropTable(
                name: "TemplateStateBindings");

            migrationBuilder.DropIndex(
                name: "IX_FunctionNodes_TemplateStateBindingId",
                table: "FunctionNodes");

            migrationBuilder.DropColumn(
                name: "TemplateStateBindingId",
                table: "FunctionNodes");

            migrationBuilder.DropColumn(
                name: "IsSystem",
                table: "EnumOptions");

        }
    }
}
