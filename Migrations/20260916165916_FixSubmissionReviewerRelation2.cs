using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyDergiApp.Migrations
{
    /// <inheritdoc />
    public partial class FixSubmissionReviewerRelation2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Golge FK kolonu "SubmissionId1" (AppDbContext'te ayni iliskinin iki kez WithMany() ile tanimlanmasindan
            // olusmustu) kaldiriliyor. Eski iki kolonlu benzersiz indeks 20260915170320 ile ham SQL'le silinmisti;
            // hepsi IF EXISTS ile yaziliyor ki canli/yerel farklari migration'i patlatmasin.
            migrationBuilder.Sql(@"
                ALTER TABLE ""SubmissionReviewers"" DROP CONSTRAINT IF EXISTS ""FK_SubmissionReviewers_Submissions_SubmissionId1"";
                DROP INDEX IF EXISTS ""IX_SubmissionReviewers_SubmissionId1"";
                DROP INDEX IF EXISTS ""IX_SubmissionReviewers_SubmissionId_ReviewerId"";
                ALTER TABLE ""SubmissionReviewers"" DROP COLUMN IF EXISTS ""SubmissionId1"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Golge kolon geri eklenir; eski iki kolonlu benzersiz indeks bilerek geri getirilmez
            // (ayni hakemin farkli turlarda atanmasini engelliyordu).
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
    }
}
