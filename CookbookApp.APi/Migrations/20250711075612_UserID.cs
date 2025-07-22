// Location: Migrations/20250711075612_UserID.cs

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookbookApp.APi.Migrations
{
    /// <inheritdoc />
    public partial class UserID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The original code in this method was trying to add a 'UserId' column
            // that already exists. By leaving this empty, we tell EF Core to
            // simply mark this migration as complete and move to the next one.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The original code in this method was trying to drop the 'UserId' column.
            // We leave this empty as well to prevent errors if you ever need to
            // roll back this migration.
        }
    }
}