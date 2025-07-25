using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookbookApp.APi.Migrations
{
    /// <inheritdoc />
    public partial class FixedChallengeIdType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_Challenges_ChallengeId1",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_ChallengeId1",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "ChallengeId1",
                table: "Submissions");

            migrationBuilder.AlterColumn<int>(
                name: "ChallengeId",
                table: "Submissions",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ChallengeId",
                table: "Submissions",
                column: "ChallengeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_Challenges_ChallengeId",
                table: "Submissions",
                column: "ChallengeId",
                principalTable: "Challenges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_Challenges_ChallengeId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_ChallengeId",
                table: "Submissions");

            migrationBuilder.AlterColumn<string>(
                name: "ChallengeId",
                table: "Submissions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ChallengeId1",
                table: "Submissions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ChallengeId1",
                table: "Submissions",
                column: "ChallengeId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_Challenges_ChallengeId1",
                table: "Submissions",
                column: "ChallengeId1",
                principalTable: "Challenges",
                principalColumn: "Id");
        }
    }
}
