using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyDergiApp.Migrations
{
    /// <inheritdoc />
    public partial class FixSubmissionReviewerRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Recommendation",
                table: "SubmissionReviewers");

            migrationBuilder.DropColumn(
                name: "ReviewText",
                table: "SubmissionReviewers");

            migrationBuilder.AlterColumn<string>(
                name: "ReviewNote",
                table: "SubmissionReviewers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "SubmissionId1",
                table: "SubmissionReviewers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionReviewers_SubmissionId1",
                table: "SubmissionReviewers",
                column: "SubmissionId1");

            migrationBuilder.AddForeignKey(
                name: "FK_SubmissionReviewers_Submissions_SubmissionId1",
                table: "SubmissionReviewers",
                column: "SubmissionId1",
                principalTable: "Submissions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubmissionReviewers_Submissions_SubmissionId1",
                table: "SubmissionReviewers");

            migrationBuilder.DropIndex(
                name: "IX_SubmissionReviewers_SubmissionId1",
                table: "SubmissionReviewers");

            migrationBuilder.DropColumn(
                name: "SubmissionId1",
                table: "SubmissionReviewers");

            migrationBuilder.AlterColumn<string>(
                name: "ReviewNote",
                table: "SubmissionReviewers",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Recommendation",
                table: "SubmissionReviewers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReviewText",
                table: "SubmissionReviewers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
