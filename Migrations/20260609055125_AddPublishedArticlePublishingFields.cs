using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyDergiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPublishedArticlePublishingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AbstractOverride",
                table: "PublishedArticles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Doi",
                table: "PublishedArticles",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Keywords",
                table: "PublishedArticles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalFilePath",
                table: "PublishedArticles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfFilePath",
                table: "PublishedArticles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbstractOverride",
                table: "PublishedArticles");

            migrationBuilder.DropColumn(
                name: "Doi",
                table: "PublishedArticles");

            migrationBuilder.DropColumn(
                name: "Keywords",
                table: "PublishedArticles");

            migrationBuilder.DropColumn(
                name: "OriginalFilePath",
                table: "PublishedArticles");

            migrationBuilder.DropColumn(
                name: "PdfFilePath",
                table: "PublishedArticles");
        }
    }
}
