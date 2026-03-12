using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SynTA.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOpenRouterModelNameToUserSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OpenRouterModelName",
                table: "UserSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpenRouterModelName",
                table: "UserSettings");
        }
    }
}
