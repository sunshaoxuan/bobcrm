using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BobCrm.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExtendEntityMetadataForCustomEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 添加 EntitySource 字段
            migrationBuilder.AddColumn<string>(
                name: "EntitySource",
                table: "EntityMetadata",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "System");

            // 添加 SourceDefinitionId 字段
            migrationBuilder.AddColumn<Guid>(
                name: "SourceDefinitionId",
                table: "EntityMetadata",
                type: "uuid",
                nullable: true);

            // 创建索引
            migrationBuilder.CreateIndex(
                name: "IX_EntityMetadata_EntitySource",
                table: "EntityMetadata",
                column: "EntitySource");

            migrationBuilder.CreateIndex(
                name: "IX_EntityMetadata_SourceDefinitionId",
                table: "EntityMetadata",
                column: "SourceDefinitionId");

            // 创建外键（如果 EntityDefinitions 表已存在）
            migrationBuilder.AddForeignKey(
                name: "FK_EntityMetadata_EntityDefinitions_SourceDefinitionId",
                table: "EntityMetadata",
                column: "SourceDefinitionId",
                principalTable: "EntityDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 删除外键
            migrationBuilder.DropForeignKey(
                name: "FK_EntityMetadata_EntityDefinitions_SourceDefinitionId",
                table: "EntityMetadata");

            // 删除索引
            migrationBuilder.DropIndex(
                name: "IX_EntityMetadata_SourceDefinitionId",
                table: "EntityMetadata");

            migrationBuilder.DropIndex(
                name: "IX_EntityMetadata_EntitySource",
                table: "EntityMetadata");

            // 删除列
            migrationBuilder.DropColumn(
                name: "SourceDefinitionId",
                table: "EntityMetadata");

            migrationBuilder.DropColumn(
                name: "EntitySource",
                table: "EntityMetadata");
        }
    }
}
