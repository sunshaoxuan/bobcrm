using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BobCrm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDataSourceInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayNameKey",
                table: "FunctionNodes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DataSourceTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "jsonb", nullable: true),
                    Description = table.Column<string>(type: "jsonb", nullable: true),
                    HandlerType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ConfigSchema = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "General"),
                    Icon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSourceTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermissionFilters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "jsonb", nullable: true),
                    Description = table.Column<string>(type: "jsonb", nullable: true),
                    RequiredFunctionCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DataScopeTag = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FilterRulesJson = table.Column<string>(type: "text", nullable: true),
                    EnableFieldLevelPermissions = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    FieldPermissionsJson = table.Column<string>(type: "text", nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionFilters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QueryDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "jsonb", nullable: true),
                    Description = table.Column<string>(type: "jsonb", nullable: true),
                    ConditionsJson = table.Column<string>(type: "text", nullable: true),
                    ParametersJson = table.Column<string>(type: "text", nullable: true),
                    AggregationsJson = table.Column<string>(type: "text", nullable: true),
                    GroupByFields = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueryDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "jsonb", nullable: true),
                    Description = table.Column<string>(type: "jsonb", nullable: true),
                    DataSourceTypeCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConfigJson = table.Column<string>(type: "text", nullable: true),
                    FieldsJson = table.Column<string>(type: "text", nullable: true),
                    SupportsPaging = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SupportsSorting = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DefaultSortField = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DefaultSortDirection = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "asc"),
                    DefaultPageSize = table.Column<int>(type: "integer", nullable: false, defaultValue: 20),
                    QueryDefinitionId = table.Column<int>(type: "integer", nullable: true),
                    PermissionFilterId = table.Column<int>(type: "integer", nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataSets_PermissionFilters_PermissionFilterId",
                        column: x => x.PermissionFilterId,
                        principalTable: "PermissionFilters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DataSets_QueryDefinitions_QueryDefinitionId",
                        column: x => x.QueryDefinitionId,
                        principalTable: "QueryDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FunctionNodes_TemplateBindingId",
                table: "FunctionNodes",
                column: "TemplateBindingId");

            migrationBuilder.CreateIndex(
                name: "IX_DataSets_Code",
                table: "DataSets",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataSets_DataSourceTypeCode",
                table: "DataSets",
                column: "DataSourceTypeCode");

            migrationBuilder.CreateIndex(
                name: "IX_DataSets_IsEnabled",
                table: "DataSets",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_DataSets_IsSystem",
                table: "DataSets",
                column: "IsSystem");

            migrationBuilder.CreateIndex(
                name: "IX_DataSets_PermissionFilterId",
                table: "DataSets",
                column: "PermissionFilterId");

            migrationBuilder.CreateIndex(
                name: "IX_DataSets_QueryDefinitionId",
                table: "DataSets",
                column: "QueryDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_DataSourceTypes_Category",
                table: "DataSourceTypes",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_DataSourceTypes_Code",
                table: "DataSourceTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataSourceTypes_IsEnabled",
                table: "DataSourceTypes",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_DataSourceTypes_IsSystem",
                table: "DataSourceTypes",
                column: "IsSystem");

            migrationBuilder.CreateIndex(
                name: "IX_DataSourceTypes_SortOrder",
                table: "DataSourceTypes",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionFilters_Code",
                table: "PermissionFilters",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermissionFilters_DataScopeTag",
                table: "PermissionFilters",
                column: "DataScopeTag");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionFilters_EntityType",
                table: "PermissionFilters",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionFilters_IsEnabled",
                table: "PermissionFilters",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionFilters_IsSystem",
                table: "PermissionFilters",
                column: "IsSystem");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionFilters_RequiredFunctionCode",
                table: "PermissionFilters",
                column: "RequiredFunctionCode");

            migrationBuilder.CreateIndex(
                name: "IX_QueryDefinitions_Code",
                table: "QueryDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QueryDefinitions_IsEnabled",
                table: "QueryDefinitions",
                column: "IsEnabled");

            migrationBuilder.AddForeignKey(
                name: "FK_FunctionNodes_TemplateBindings_TemplateBindingId",
                table: "FunctionNodes",
                column: "TemplateBindingId",
                principalTable: "TemplateBindings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FunctionNodes_TemplateBindings_TemplateBindingId",
                table: "FunctionNodes");

            migrationBuilder.DropTable(
                name: "DataSets");

            migrationBuilder.DropTable(
                name: "DataSourceTypes");

            migrationBuilder.DropTable(
                name: "PermissionFilters");

            migrationBuilder.DropTable(
                name: "QueryDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_FunctionNodes_TemplateBindingId",
                table: "FunctionNodes");

            migrationBuilder.DropColumn(
                name: "DisplayNameKey",
                table: "FunctionNodes");
        }
    }
}
