using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BobCrm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityDomains : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EntityDomains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "jsonb", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityDomains", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntityDomains_Code",
                table: "EntityDomains",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntityDomains_SortOrder",
                table: "EntityDomains",
                column: "SortOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntityDomains");
        }
    }
}

