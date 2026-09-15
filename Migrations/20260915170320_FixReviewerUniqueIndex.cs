using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyDergiApp.Migrations
{
    /// <inheritdoc />
    public partial class FixReviewerUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Benzersiz hakem indeksi ReviewRound'u da kapsamali (ayni hakem farkli turlarda tekrar atanabilir).
            // Onceki migration'larin Up kodu bosaltildigi icin bazi ortamlarda eski indeks kalmis olabilir;
            // bu yuzden IF EXISTS / IF NOT EXISTS ile her ortamda guvenle calisir.
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ""IX_SubmissionReviewers_SubmissionId_ReviewerId"";
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_SubmissionReviewers_SubmissionId_ReviewerId_ReviewRound""
                    ON ""SubmissionReviewers"" (""SubmissionId"", ""ReviewerId"", ""ReviewRound"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ""IX_SubmissionReviewers_SubmissionId_ReviewerId_ReviewRound"";
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_SubmissionReviewers_SubmissionId_ReviewerId""
                    ON ""SubmissionReviewers"" (""SubmissionId"", ""ReviewerId"");
            ");
        }
    }
}
