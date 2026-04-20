using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyDergiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddDecisionNoteAndNoteToEditorToSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_AspNetUsers_EditorId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_EditorId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "EditorId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "EditorNote",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "FinalDecision",
                table: "Submissions");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Submissions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<string>(
                name: "Keywords",
                table: "Submissions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FilePath",
                table: "Submissions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionNote",
                table: "Submissions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NoteToEditor",
                table: "Submissions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DecisionNote",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "NoteToEditor",
                table: "Submissions");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Submissions",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Keywords",
                table: "Submissions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "FilePath",
                table: "Submissions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "EditorId",
                table: "Submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EditorNote",
                table: "Submissions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalDecision",
                table: "Submissions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_EditorId",
                table: "Submissions",
                column: "EditorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_AspNetUsers_EditorId",
                table: "Submissions",
                column: "EditorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
