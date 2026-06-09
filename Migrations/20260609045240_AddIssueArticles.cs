using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MyDergiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddIssueArticles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IssueArticles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IssueId = table.Column<int>(type: "integer", nullable: false),
                    SubmissionId = table.Column<int>(type: "integer", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    SectionTitle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PageRange = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Doi = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    PublishedTitle = table.Column<string>(type: "text", nullable: true),
                    PublishedAuthors = table.Column<string>(type: "text", nullable: true),
                    PublishedAbstract = table.Column<string>(type: "text", nullable: true),
                    PdfFilePath = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssueArticles");
        }
    }
}
