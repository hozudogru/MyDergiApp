using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyDergiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedReviewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EthicalConcerns",
                table: "Reviews",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasEthicalIssue",
                table: "Reviews",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LiteratureScore",
                table: "Reviews",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MethodologyScore",
                table: "Reviews",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginalityScore",
                table: "Reviews",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OverallScore",
                table: "Reviews",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WritingQualityScore",
                table: "Reviews",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EthicalConcerns",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "HasEthicalIssue",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "LiteratureScore",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "MethodologyScore",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "OriginalityScore",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "OverallScore",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "WritingQualityScore",
                table: "Reviews");
        }
    }
}
