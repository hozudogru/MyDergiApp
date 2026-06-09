using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyDergiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddIssueFullIssuePdfPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FullIssuePdfPath",
                table: "Issues",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FullIssuePdfPath",
                table: "Issues");
        }
    }
}
