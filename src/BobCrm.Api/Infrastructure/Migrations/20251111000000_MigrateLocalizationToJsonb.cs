using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BobCrm.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MigrateLocalizationToJsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. 添加 Translations jsonb 列
            migrationBuilder.AddColumn<string>(
                name: "Translations",
                table: "LocalizationResources",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            // 2. 迁移现有数据：将 EN, JA, ZH 列的值转换为 JSON 格式存入 Translations 列
            migrationBuilder.Sql(@"
                UPDATE ""LocalizationResources""
                SET ""Translations"" = jsonb_build_object(
                    'zh', COALESCE(""ZH"", ''),
                    'ja', COALESCE(""JA"", ''),
                    'en', COALESCE(""EN"", '')
                )
                WHERE ""Translations""::text = '{}'::text OR ""Translations"" IS NULL;
            ");

            // 3. 删除旧的 EN, JA, ZH 列
            migrationBuilder.DropColumn(
                name: "EN",
                table: "LocalizationResources");

            migrationBuilder.DropColumn(
                name: "JA",
                table: "LocalizationResources");

            migrationBuilder.DropColumn(
                name: "ZH",
                table: "LocalizationResources");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 回滚：重新添加 EN, JA, ZH 列
            migrationBuilder.AddColumn<string>(
                name: "EN",
                table: "LocalizationResources",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JA",
                table: "LocalizationResources",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZH",
                table: "LocalizationResources",
                type: "text",
                nullable: true);

            // 从 Translations jsonb 列恢复数据到 EN, JA, ZH 列
            migrationBuilder.Sql(@"
                UPDATE ""LocalizationResources""
                SET
                    ""EN"" = ""Translations""->>'en',
                    ""JA"" = ""Translations""->>'ja',
                    ""ZH"" = ""Translations""->>'zh'
                WHERE ""Translations"" IS NOT NULL;
            ");

            // 删除 Translations 列
            migrationBuilder.DropColumn(
                name: "Translations",
                table: "LocalizationResources");
        }
    }
}
