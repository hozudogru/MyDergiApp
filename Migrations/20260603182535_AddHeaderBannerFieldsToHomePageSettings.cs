using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyDergiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddHeaderBannerFieldsToHomePageSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HeaderBackgroundImagePath",
                table: "HomePageSettings",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeaderLogoPath",
                table: "HomePageSettings",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeaderRightText",
                table: "HomePageSettings",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeaderSubtitle",
                table: "HomePageSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeaderTitle",
                table: "HomePageSettings",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowHeaderLogo",
                table: "HomePageSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeaderBackgroundImagePath",
                table: "HomePageSettings");

            migrationBuilder.DropColumn(
                name: "HeaderLogoPath",
                table: "HomePageSettings");

            migrationBuilder.DropColumn(
                name: "HeaderRightText",
                table: "HomePageSettings");

            migrationBuilder.DropColumn(
                name: "HeaderSubtitle",
                table: "HomePageSettings");

            migrationBuilder.DropColumn(
                name: "HeaderTitle",
                table: "HomePageSettings");

            migrationBuilder.DropColumn(
                name: "ShowHeaderLogo",
                table: "HomePageSettings");
        }
    }
}
