using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookbookApp.APi.Migrations
{
    /// <inheritdoc />
    public partial class AuthorizationDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserID",
                table: "Recipes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_UserID",
                table: "Recipes",
                column: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Recipes_Users_UserID",
                table: "Recipes",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Recipes_Users_UserID",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_UserID",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "UserID",
                table: "Recipes");
        }
    }
}
