using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MyDergiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionFileAndEditorialFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FilePath",
                table: "Submissions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "AssignedChiefEditorId",
                table: "Submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedSectionEditorId",
                table: "Submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverLetter",
                table: "Submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Prefix",
                table: "Submissions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReferencesText",
                table: "Submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subtitle",
                table: "Submissions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SubmissionFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubmissionId = table.Column<int>(type: "integer", nullable: false),
                    FileType = table.Column<string>(type: "text", nullable: false),
                    OriginalFileName = table.Column<string>(type: "text", nullable: false),
                    StoredFilePath = table.Column<string>(type: "text", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    UploadedByUserId = table.Column<string>(type: "text", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionFiles_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SubmissionFiles_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_AssignedChiefEditorId",
                table: "Submissions",
                column: "AssignedChiefEditorId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_AssignedSectionEditorId",
                table: "Submissions",
                column: "AssignedSectionEditorId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_DecisionByUserId",
                table: "Submissions",
                column: "DecisionByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionFiles_SubmissionId",
                table: "SubmissionFiles",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionFiles_UploadedByUserId",
                table: "SubmissionFiles",
                column: "UploadedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_AspNetUsers_AssignedChiefEditorId",
                table: "Submissions",
                column: "AssignedChiefEditorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_AspNetUsers_AssignedSectionEditorId",
                table: "Submissions",
                column: "AssignedSectionEditorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_AspNetUsers_DecisionByUserId",
                table: "Submissions",
                column: "DecisionByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_AspNetUsers_AssignedChiefEditorId",
                table: "Submissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_AspNetUsers_AssignedSectionEditorId",
                table: "Submissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_AspNetUsers_DecisionByUserId",
                table: "Submissions");

            migrationBuilder.DropTable(
                name: "SubmissionFiles");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_AssignedChiefEditorId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_AssignedSectionEditorId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_DecisionByUserId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "AssignedChiefEditorId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "AssignedSectionEditorId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "CoverLetter",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "Prefix",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "ReferencesText",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "Subtitle",
                table: "Submissions");

            migrationBuilder.AlterColumn<string>(
                name: "FilePath",
                table: "Submissions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
