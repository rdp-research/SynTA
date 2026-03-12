using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SynTA.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveApprovalAndValidationProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoMode",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "GherkinScenarios");

            migrationBuilder.DropColumn(
                name: "IsValidated",
                table: "CypressScripts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoMode",
                table: "UserSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "GherkinScenarios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsValidated",
                table: "CypressScripts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
