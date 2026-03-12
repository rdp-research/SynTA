using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SynTA.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceAutoSaveWithAutoMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoSaveCypressScripts",
                table: "UserSettings");

            migrationBuilder.RenameColumn(
                name: "AutoSaveGherkinScenarios",
                table: "UserSettings",
                newName: "AutoMode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AutoMode",
                table: "UserSettings",
                newName: "AutoSaveGherkinScenarios");

            migrationBuilder.AddColumn<bool>(
                name: "AutoSaveCypressScripts",
                table: "UserSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
