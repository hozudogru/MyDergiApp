using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyDergiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddBannerFieldsToHomePageSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannerDescription",
                table: "HomePageSettings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BannerImagePath",
                table: "HomePageSettings",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BannerLabel",
                table: "HomePageSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BannerPrimaryButtonText",
                table: "HomePageSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BannerPrimaryButtonUrl",
                table: "HomePageSettings",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BannerSecondaryButtonText",
                table: "HomePageSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BannerSecondaryButtonUrl",
                table: "HomePageSettings",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BannerTitle",
                table: "HomePageSettings",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowBanner",
                table: "HomePageSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannerDescription",
                table: "HomePageSettings");

            migrationBuilder.DropColumn(
                name: "BannerImagePath",
                table: "HomePageSettings");

            migrationBuilder.DropColumn(
                name: "BannerLabel",
                table: "HomePageSettings");

            migrationBuilder.DropColumn(
                name: "BannerPrimaryButtonText",
                table: "HomePageSettings");

            migrationBuilder.DropColumn(
                name: "BannerPrimaryButtonUrl",
                table: "HomePageSettings");

            migrationBuilder.DropColumn(
                name: "BannerSecondaryButtonText",
                table: "HomePageSettings");

            migrationBuilder.DropColumn(
                name: "BannerSecondaryButtonUrl",
                table: "HomePageSettings");

            migrationBuilder.DropColumn(
                name: "BannerTitle",
                table: "HomePageSettings");

            migrationBuilder.DropColumn(
                name: "ShowBanner",
                table: "HomePageSettings");
        }
    }
}
