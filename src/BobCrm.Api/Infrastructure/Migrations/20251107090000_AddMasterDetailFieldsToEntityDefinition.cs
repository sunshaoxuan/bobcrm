using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BobCrm.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMasterDetailFieldsToEntityDefinition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 添加主子表结构配置字段
            migrationBuilder.AddColumn<Guid>(
                name: "ParentEntityId",
                table: "EntityDefinitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentEntityName",
                table: "EntityDefinitions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentForeignKeyField",
                table: "EntityDefinitions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentCollectionProperty",
                table: "EntityDefinitions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CascadeDeleteBehavior",
                table: "EntityDefinitions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NoAction");

            migrationBuilder.AddColumn<bool>(
                name: "AutoCascadeSave",
                table: "EntityDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // 添加锁定字段（模板引用时锁定实体定义）
            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "EntityDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // 创建父实体ID的外键约束
            migrationBuilder.CreateIndex(
                name: "IX_EntityDefinitions_ParentEntityId",
                table: "EntityDefinitions",
                column: "ParentEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_EntityDefinitions_EntityDefinitions_ParentEntityId",
                table: "EntityDefinitions",
                column: "ParentEntityId",
                principalTable: "EntityDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 删除外键约束
            migrationBuilder.DropForeignKey(
                name: "FK_EntityDefinitions_EntityDefinitions_ParentEntityId",
                table: "EntityDefinitions");

            // 删除索引
            migrationBuilder.DropIndex(
                name: "IX_EntityDefinitions_ParentEntityId",
                table: "EntityDefinitions");

            // 删除列
            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "EntityDefinitions");

            migrationBuilder.DropColumn(
                name: "AutoCascadeSave",
                table: "EntityDefinitions");

            migrationBuilder.DropColumn(
                name: "CascadeDeleteBehavior",
                table: "EntityDefinitions");

            migrationBuilder.DropColumn(
                name: "ParentCollectionProperty",
                table: "EntityDefinitions");

            migrationBuilder.DropColumn(
                name: "ParentForeignKeyField",
                table: "EntityDefinitions");

            migrationBuilder.DropColumn(
                name: "ParentEntityName",
                table: "EntityDefinitions");

            migrationBuilder.DropColumn(
                name: "ParentEntityId",
                table: "EntityDefinitions");
        }
    }
}
