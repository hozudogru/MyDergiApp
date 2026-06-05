using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MyDergiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubmissionReviewers_SubmissionId_ReviewerId",
                table: "SubmissionReviewers");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionReviewers_SubmissionId_ReviewerId_ReviewRound",
                table: "SubmissionReviewers",
                columns: new[] { "SubmissionId", "ReviewerId", "ReviewRound" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubmissionReviewers_SubmissionId_ReviewerId_ReviewRound",
                table: "SubmissionReviewers");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionReviewers_SubmissionId_ReviewerId",
                table: "SubmissionReviewers",
                columns: new[] { "SubmissionId", "ReviewerId" },
                unique: true);
        }
    }
}
