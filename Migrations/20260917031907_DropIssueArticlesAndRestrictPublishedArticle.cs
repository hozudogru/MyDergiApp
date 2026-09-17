using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MyDergiApp.Migrations
{
    /// <inheritdoc />
    public partial class DropIssueArticlesAndRestrictPublishedArticle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PublishedArticles_Submissions_SubmissionId",
                table: "PublishedArticles");

            migrationBuilder.DropTable(
                name: "IssueArticles");

            migrationBuilder.AddForeignKey(
                name: "FK_PublishedArticles_Submissions_SubmissionId",
                table: "PublishedArticles",
                column: "SubmissionId",
                principalTable: "Submissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PublishedArticles_Submissions_SubmissionId",
                table: "PublishedArticles");

            migrationBuilder.CreateTable(
                name: "IssueArticles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IssueId = table.Column<int>(type: "integer", nullable: false),
                    SubmissionId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Doi = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    PageRange = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PdfFilePath = table.Column<string>(type: "text", nullable: true),
                    PublishedAbstract = table.Column<string>(type: "text", nullable: true),
                    PublishedAuthors = table.Column<string>(type: "text", nullable: true),
                    PublishedTitle = table.Column<string>(type: "text", nullable: true),
                    SectionTitle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueArticles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssueArticles_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IssueArticles_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssueArticles_IssueId",
                table: "IssueArticles",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueArticles_SubmissionId",
                table: "IssueArticles",
                column: "SubmissionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PublishedArticles_Submissions_SubmissionId",
                table: "PublishedArticles",
                column: "SubmissionId",
                principalTable: "Submissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
