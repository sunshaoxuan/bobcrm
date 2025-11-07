using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BobCrm.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MergeEntityMetadataIntoEntityDefinition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 添加EntityMetadata的字段到EntityDefinitions表
            migrationBuilder.AddColumn<string>(
                name: "FullTypeName",
                table: "EntityDefinitions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EntityRoute",
                table: "EntityDefinitions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApiEndpoint",
                table: "EntityDefinitions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsRootEntity",
                table: "EntityDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "EntityDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "EntityDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "EntityDefinitions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "EntityDefinitions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "EntityDefinitions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Custom");

            // 创建FullTypeName的唯一索引
            migrationBuilder.CreateIndex(
                name: "IX_EntityDefinitions_FullTypeName",
                table: "EntityDefinitions",
                column: "FullTypeName",
                unique: true);

            // 创建Source索引
            migrationBuilder.CreateIndex(
                name: "IX_EntityDefinitions_Source",
                table: "EntityDefinitions",
                column: "Source");

            // 创建综合索引用于查询根实体
            migrationBuilder.CreateIndex(
                name: "IX_EntityDefinitions_IsRootEntity_IsEnabled_Order",
                table: "EntityDefinitions",
                columns: new[] { "IsRootEntity", "IsEnabled", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 删除索引
            migrationBuilder.DropIndex(
                name: "IX_EntityDefinitions_IsRootEntity_IsEnabled_Order",
                table: "EntityDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_EntityDefinitions_Source",
                table: "EntityDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_EntityDefinitions_FullTypeName",
                table: "EntityDefinitions");

            // 删除列
            migrationBuilder.DropColumn(
                name: "Source",
                table: "EntityDefinitions");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "EntityDefinitions");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "EntityDefinitions");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "EntityDefinitions");

            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "EntityDefinitions");

            migrationBuilder.DropColumn(
                name: "IsRootEntity",
                table: "EntityDefinitions");

            migrationBuilder.DropColumn(
                name: "ApiEndpoint",
                table: "EntityDefinitions");

            migrationBuilder.DropColumn(
                name: "EntityRoute",
                table: "EntityDefinitions");

            migrationBuilder.DropColumn(
                name: "FullTypeName",
                table: "EntityDefinitions");
        }
    }
}
