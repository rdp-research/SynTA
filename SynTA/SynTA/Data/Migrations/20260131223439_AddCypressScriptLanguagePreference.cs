using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SynTA.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCypressScriptLanguagePreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PreferredCypressLanguage",
                table: "UserSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredCypressLanguage",
                table: "UserSettings");
        }
    }
}
