using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BobCrm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceToFieldMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "FieldMetadatas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            // 为现有数据设置默认值
            // 所有现有字段默认为 Custom 来源
            migrationBuilder.Sql(@"UPDATE ""FieldMetadatas"" SET ""Source"" = 'Custom' WHERE ""Source"" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                table: "FieldMetadatas");
        }
    }
}
