using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyDergiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionReviewerUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubmissionReviewers_SubmissionId",
                table: "SubmissionReviewers");

            migrationBuilder.AlterColumn<string>(
                name: "ReviewNote",
                table: "SubmissionReviewers",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Recommendation",
                table: "SubmissionReviewers",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewText",
                table: "SubmissionReviewers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionReviewers_SubmissionId_ReviewerId",
                table: "SubmissionReviewers",
                columns: new[] { "SubmissionId", "ReviewerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubmissionReviewers_SubmissionId_ReviewerId",
                table: "SubmissionReviewers");

            migrationBuilder.DropColumn(
                name: "ReviewText",
                table: "SubmissionReviewers");

            migrationBuilder.AlterColumn<string>(
                name: "ReviewNote",
                table: "SubmissionReviewers",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Recommendation",
                table: "SubmissionReviewers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionReviewers_SubmissionId",
                table: "SubmissionReviewers",
                column: "SubmissionId");
        }
    }
}
