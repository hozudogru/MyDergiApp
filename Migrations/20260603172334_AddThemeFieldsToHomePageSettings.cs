using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyDergiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddThemeFieldsToHomePageSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BodyBgColor",
                table: "HomePageSettings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HeaderBgColor",
                table: "HomePageSettings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NavBgColor",
                table: "HomePageSettings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryColor",
                table: "HomePageSettings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SecondaryColor",
                table: "HomePageSettings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TextColor",
                table: "HomePageSettings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ThemeName",
                table: "HomePageSettings",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BodyBgColor",
                table: "HomePageSettings");

            migrationBuilder.DropColumn(
                name: "HeaderBgColor",
                table: "HomePageSettings");

            migrationBuilder.DropColumn(
                name: "NavBgColor",
                table: "HomePageSettings");

            migrationBuilder.DropColumn(
                name: "PrimaryColor",
                table: "HomePageSettings");

            migrationBuilder.DropColumn(
                name: "SecondaryColor",
                table: "HomePageSettings");

            migrationBuilder.DropColumn(
                name: "TextColor",
                table: "HomePageSettings");

            migrationBuilder.DropColumn(
                name: "ThemeName",
                table: "HomePageSettings");
        }
    }
}
