using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BobCrm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSubEntityDefinitionIdToFieldMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FieldMetadatas_EntityDefinitionId_PropertyName",
                table: "FieldMetadatas");

            migrationBuilder.AddColumn<Guid>(
                name: "SubEntityDefinitionId",
                table: "FieldMetadatas",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SubEntityDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "jsonb", nullable: false),
                    Description = table.Column<string>(type: "jsonb", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DefaultSortField = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsDescending = table.Column<bool>(type: "boolean", nullable: false),
                    ForeignKeyField = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CollectionPropertyName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CascadeDeleteBehavior = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubEntityDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubEntityDefinitions_EntityDefinitions_EntityDefinitionId",
                        column: x => x.EntityDefinitionId,
                        principalTable: "EntityDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FieldMetadatas_EntityDefinitionId_PropertyName",
                table: "FieldMetadatas",
                columns: new[] { "EntityDefinitionId", "PropertyName" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldMetadatas_SubEntityDefinitionId",
                table: "FieldMetadatas",
                column: "SubEntityDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubEntityDefinitions_EntityDefinitionId",
                table: "SubEntityDefinitions",
                column: "EntityDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubEntityDefinitions_EntityDefinitionId_Code",
                table: "SubEntityDefinitions",
                columns: new[] { "EntityDefinitionId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubEntityDefinitions_SortOrder",
                table: "SubEntityDefinitions",
                column: "SortOrder");

            migrationBuilder.AddForeignKey(
                name: "FK_FieldMetadatas_SubEntityDefinitions_SubEntityDefinitionId",
                table: "FieldMetadatas",
                column: "SubEntityDefinitionId",
                principalTable: "SubEntityDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FieldMetadatas_SubEntityDefinitions_SubEntityDefinitionId",
                table: "FieldMetadatas");

            migrationBuilder.DropTable(
                name: "SubEntityDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_FieldMetadatas_EntityDefinitionId_PropertyName",
                table: "FieldMetadatas");

            migrationBuilder.DropIndex(
                name: "IX_FieldMetadatas_SubEntityDefinitionId",
                table: "FieldMetadatas");

            migrationBuilder.DropColumn(
                name: "SubEntityDefinitionId",
                table: "FieldMetadatas");

            migrationBuilder.CreateIndex(
                name: "IX_FieldMetadatas_EntityDefinitionId_PropertyName",
                table: "FieldMetadatas",
                columns: new[] { "EntityDefinitionId", "PropertyName" },
                unique: true);
        }
    }
}
