using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SynTA.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserStoryTextField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, add the new column with a temporary default
            migrationBuilder.AddColumn<string>(
                name: "UserStoryText",
                table: "UserStories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            // Copy existing Description values to UserStoryText for data migration
            migrationBuilder.Sql("UPDATE UserStories SET UserStoryText = Description WHERE Description IS NOT NULL AND Description != ''");

            // Now make Description nullable
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "UserStories",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // Clear Description for existing records (it's now been moved to UserStoryText)
            migrationBuilder.Sql("UPDATE UserStories SET Description = NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Copy UserStoryText back to Description before dropping the column
            migrationBuilder.Sql("UPDATE UserStories SET Description = UserStoryText WHERE UserStoryText IS NOT NULL AND UserStoryText != ''");

            migrationBuilder.DropColumn(
                name: "UserStoryText",
                table: "UserStories");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "UserStories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
