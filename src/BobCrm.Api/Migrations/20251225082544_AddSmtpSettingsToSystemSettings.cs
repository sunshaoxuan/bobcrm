using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BobCrm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSmtpSettingsToSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SmtpDisplayName",
                table: "SystemSettings",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SmtpEnableSsl",
                table: "SystemSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SmtpFromAddress",
                table: "SystemSettings",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpHost",
                table: "SystemSettings",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpPasswordEncrypted",
                table: "SystemSettings",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SmtpPort",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                defaultValue: 25);

            migrationBuilder.AddColumn<string>(
                name: "SmtpUsername",
                table: "SystemSettings",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SmtpDisplayName",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SmtpEnableSsl",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SmtpFromAddress",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SmtpHost",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SmtpPasswordEncrypted",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SmtpPort",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SmtpUsername",
                table: "SystemSettings");
        }
    }
}
