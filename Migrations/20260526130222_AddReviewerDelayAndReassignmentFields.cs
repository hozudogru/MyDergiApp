using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyDergiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewerDelayAndReassignmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "SubmissionReviewers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "SubmissionReviewers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledByUserId",
                table: "SubmissionReviewers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "SubmissionReviewers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReminderCount",
                table: "SubmissionReviewers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderSentAt",
                table: "SubmissionReviewers",
                type: "timestamp with time zone",
                nullable: true);

            
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubmissionReviewers_SubmissionId_ReviewerId_ReviewRound",
                table: "SubmissionReviewers");

            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "SubmissionReviewers");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "SubmissionReviewers");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "SubmissionReviewers");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "SubmissionReviewers");

            migrationBuilder.DropColumn(
                name: "ReminderCount",
                table: "SubmissionReviewers");

            migrationBuilder.DropColumn(
                name: "ReminderSentAt",
                table: "SubmissionReviewers");
        }
    }
}
