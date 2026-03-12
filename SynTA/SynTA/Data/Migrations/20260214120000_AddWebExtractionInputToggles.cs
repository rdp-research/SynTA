using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace SynTA.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(global::SynTA.Data.ApplicationDbContext))]
    [Migration("20260214120000_AddWebExtractionInputToggles")]
    public partial class AddWebExtractionInputToggles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IncludeAccessibilityTreeInExtraction",
                table: "UserSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeSimplifiedHtmlInExtraction",
                table: "UserSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeUiElementMapInExtraction",
                table: "UserSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeWebPageMetadataInExtraction",
                table: "UserSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "WebExtractionEnabledForCypressGeneration",
                table: "UserSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IncludeAccessibilityTreeInExtraction",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "IncludeSimplifiedHtmlInExtraction",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "IncludeUiElementMapInExtraction",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "IncludeWebPageMetadataInExtraction",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "WebExtractionEnabledForCypressGeneration",
                table: "UserSettings");
        }
    }
}
