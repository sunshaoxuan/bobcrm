using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BobCrm.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityTypeToUserLayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 添加EntityType列
            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "UserLayouts",
                type: "text",
                nullable: true);

            // 为现有记录设置默认值 "customer"（兼容旧数据）
            migrationBuilder.Sql(
                "UPDATE \"UserLayouts\" SET \"EntityType\" = 'customer' WHERE \"EntityType\" IS NULL OR \"EntityType\" = ''");

            // 创建复合索引以提高查询性能
            migrationBuilder.CreateIndex(
                name: "IX_UserLayouts_UserId_EntityType",
                table: "UserLayouts",
                columns: new[] { "UserId", "EntityType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 删除索引
            migrationBuilder.DropIndex(
                name: "IX_UserLayouts_UserId_EntityType",
                table: "UserLayouts");

            // 删除列
            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "UserLayouts");
        }
    }
}
