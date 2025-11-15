using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BobCrm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuAuditTrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "FunctionNodes",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TemplateId",
                table: "FunctionNodes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ActorId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ActorName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Target = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Payload = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FunctionNodes_TemplateId",
                table: "FunctionNodes",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Category",
                table: "AuditLogs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Category_Action",
                table: "AuditLogs",
                columns: new[] { "Category", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.AddForeignKey(
                name: "FK_FunctionNodes_FormTemplates_TemplateId",
                table: "FunctionNodes",
                column: "TemplateId",
                principalTable: "FormTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FunctionNodes_FormTemplates_TemplateId",
                table: "FunctionNodes");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_FunctionNodes_TemplateId",
                table: "FunctionNodes");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "FunctionNodes");

            migrationBuilder.DropColumn(
                name: "TemplateId",
                table: "FunctionNodes");
        }
    }
}
