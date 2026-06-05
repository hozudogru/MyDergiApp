using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyDergiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewerAttachmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReviewerAttachmentNote",
                table: "Reviews",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewerAttachmentOriginalFileName",
                table: "Reviews",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewerAttachmentPath",
                table: "Reviews",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SendAttachmentToAuthor",
                table: "Reviews",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReviewerAttachmentNote",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ReviewerAttachmentOriginalFileName",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ReviewerAttachmentPath",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "SendAttachmentToAuthor",
                table: "Reviews");
        }
    }
}
