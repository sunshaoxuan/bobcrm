using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BobCrm.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityCustomizationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 创建 EntityDefinitions 表
            migrationBuilder.CreateTable(
                name: "EntityDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Namespace = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EntityName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayNameKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DescriptionKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StructureType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityDefinitions", x => x.Id);
                });

            // 创建 FieldMetadatas 表
            migrationBuilder.CreateTable(
                name: "FieldMetadatas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentFieldId = table.Column<Guid>(type: "uuid", nullable: true),
                    PropertyName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayNameKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DataType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Length = table.Column<int>(type: "integer", nullable: true),
                    Precision = table.Column<int>(type: "integer", nullable: true),
                    Scale = table.Column<int>(type: "integer", nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsEntityRef = table.Column<bool>(type: "boolean", nullable: false),
                    ReferencedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    TableName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DefaultValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ValidationRules = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldMetadatas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldMetadatas_EntityDefinitions_EntityDefinitionId",
                        column: x => x.EntityDefinitionId,
                        principalTable: "EntityDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FieldMetadatas_FieldMetadatas_ParentFieldId",
                        column: x => x.ParentFieldId,
                        principalTable: "FieldMetadatas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FieldMetadatas_EntityDefinitions_ReferencedEntityId",
                        column: x => x.ReferencedEntityId,
                        principalTable: "EntityDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // 创建 EntityInterfaces 表
            migrationBuilder.CreateTable(
                name: "EntityInterfaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    InterfaceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityInterfaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntityInterfaces_EntityDefinitions_EntityDefinitionId",
                        column: x => x.EntityDefinitionId,
                        principalTable: "EntityDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 创建 DDLScripts 表
            migrationBuilder.CreateTable(
                name: "DDLScripts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScriptType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SqlScript = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DDLScripts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DDLScripts_EntityDefinitions_EntityDefinitionId",
                        column: x => x.EntityDefinitionId,
                        principalTable: "EntityDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 创建索引 - EntityDefinitions
            migrationBuilder.CreateIndex(
                name: "IX_EntityDefinitions_Namespace_EntityName",
                table: "EntityDefinitions",
                columns: new[] { "Namespace", "EntityName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntityDefinitions_Status",
                table: "EntityDefinitions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EntityDefinitions_IsLocked",
                table: "EntityDefinitions",
                column: "IsLocked");

            // 创建索引 - FieldMetadatas
            migrationBuilder.CreateIndex(
                name: "IX_FieldMetadatas_EntityDefinitionId",
                table: "FieldMetadatas",
                column: "EntityDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldMetadatas_ParentFieldId",
                table: "FieldMetadatas",
                column: "ParentFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldMetadatas_EntityDefinitionId_PropertyName",
                table: "FieldMetadatas",
                columns: new[] { "EntityDefinitionId", "PropertyName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldMetadatas_ReferencedEntityId",
                table: "FieldMetadatas",
                column: "ReferencedEntityId");

            // 创建索引 - EntityInterfaces
            migrationBuilder.CreateIndex(
                name: "IX_EntityInterfaces_EntityDefinitionId_InterfaceType",
                table: "EntityInterfaces",
                columns: new[] { "EntityDefinitionId", "InterfaceType" },
                unique: true);

            // 创建索引 - DDLScripts
            migrationBuilder.CreateIndex(
                name: "IX_DDLScripts_EntityDefinitionId",
                table: "DDLScripts",
                column: "EntityDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_DDLScripts_Status",
                table: "DDLScripts",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DDLScripts");
            migrationBuilder.DropTable(name: "EntityInterfaces");
            migrationBuilder.DropTable(name: "FieldMetadatas");
            migrationBuilder.DropTable(name: "EntityDefinitions");
        }
    }
}
