using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BobCrm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FunctionNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Route = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Icon = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsMenu = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunctionNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FunctionNodes_FunctionNodes_ParentId",
                        column: x => x.ParentId,
                        principalTable: "FunctionNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleAssignments_RoleProfiles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "RoleProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleDataScopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ScopeType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FilterExpression = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleDataScopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleDataScopes_RoleProfiles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "RoleProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleFunctionPermissions",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    FunctionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleFunctionPermissions", x => new { x.RoleId, x.FunctionId });
                    table.ForeignKey(
                        name: "FK_RoleFunctionPermissions_FunctionNodes_FunctionId",
                        column: x => x.FunctionId,
                        principalTable: "FunctionNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleFunctionPermissions_RoleProfiles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "RoleProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FunctionNodes_Code",
                table: "FunctionNodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FunctionNodes_ParentId",
                table: "FunctionNodes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAssignments_RoleId",
                table: "RoleAssignments",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAssignments_UserId_RoleId_OrganizationId",
                table: "RoleAssignments",
                columns: new[] { "UserId", "RoleId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleDataScopes_RoleId",
                table: "RoleDataScopes",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleFunctionPermissions_FunctionId",
                table: "RoleFunctionPermissions",
                column: "FunctionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleProfiles_Code_OrganizationId",
                table: "RoleProfiles",
                columns: new[] { "Code", "OrganizationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleAssignments");

            migrationBuilder.DropTable(
                name: "RoleDataScopes");

            migrationBuilder.DropTable(
                name: "RoleFunctionPermissions");

            migrationBuilder.DropTable(
                name: "FunctionNodes");

            migrationBuilder.DropTable(
                name: "RoleProfiles");
        }
    }
}
