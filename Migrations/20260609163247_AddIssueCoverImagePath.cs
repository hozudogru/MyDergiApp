using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyDergiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddIssueCoverImagePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverImagePath",
                table: "Issues",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverImagePath",
                table: "Issues");
        }
    }
}
