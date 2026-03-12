using SynTA.Models.Domain;

namespace SynTA.Services.Utilities;

/// <summary>
/// Implementation of the filename service for generating and sanitizing filenames.
/// Centralizes filename generation logic to eliminate duplication across controllers and services.
/// </summary>
public class FileNameService : IFileNameService
{
    /// <inheritdoc />
    public string GenerateCypressFileName(string pattern, string storyTitle, string projectName, CypressScriptLanguage language)
    {
        // Determine file extension based on script language
        var fileExtension = language == CypressScriptLanguage.TypeScript ? ".cy.ts" : ".cy.js";

        var sanitizedTitle = Sanitize(storyTitle);
        var sanitizedProject = Sanitize(projectName);
        var now = DateTime.Now;

        // Replace placeholders in pattern
        // Note: {DateTime} must be replaced before {Date} to avoid partial replacement
        var fileName = pattern
            .Replace("{DateTime}", now.ToString("yyyy-MM-dd_HH-mm-ss"))
            .Replace("{UserStory}", sanitizedTitle)
            .Replace("{Project}", sanitizedProject)
            .Replace("{Date}", now.ToString("yyyy-MM-dd"));

        // Always strip any existing Cypress/JS/TS extensions before adding the correct one
        // This handles cases where pattern already contains an extension
        fileName = StripCypressExtensions(fileName);

        // Ensure we have a clean filename without trailing dots before adding extension
        fileName = fileName.TrimEnd('.');

        // Add the correct extension based on language preference
        fileName += fileExtension;

        return fileName;
    }

    /// <inheritdoc />
    public string GenerateGherkinFileName(string storyTitle)
    {
        var sanitizedTitle = Sanitize(storyTitle);
        return $"{sanitizedTitle}.feature";
    }

    /// <inheritdoc />
    public string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "unnamed";

        // Remove special characters and convert to lowercase
        // Preserve dots as they may be part of the filename pattern extensions
        var sanitized = new string(input
            .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || c == '-' || c == '_' || c == '.')
            .ToArray());

        sanitized = sanitized.Replace(" ", "-").ToLowerInvariant();

        // Remove consecutive dashes
        while (sanitized.Contains("--"))
        {
            sanitized = sanitized.Replace("--", "-");
        }

        // Trim dashes from start and end
        sanitized = sanitized.Trim('-');

        // Limit length
        if (sanitized.Length > 50)
        {
            sanitized = sanitized.Substring(0, 50).TrimEnd('-');
        }

        return string.IsNullOrWhiteSpace(sanitized) ? "unnamed" : sanitized;
    }

    /// <inheritdoc />
    public string StripCypressExtensions(string fileName)
    {
        // Keep stripping extensions until none remain (handles cases like .cy.js.cy.ts)
        bool extensionRemoved;
        do
        {
            extensionRemoved = false;

            if (fileName.EndsWith(".cy.ts", StringComparison.OrdinalIgnoreCase))
            {
                fileName = fileName.Substring(0, fileName.Length - 7); // Remove 7 characters: .cy.ts
                extensionRemoved = true;
            }
            else if (fileName.EndsWith(".cy.js", StringComparison.OrdinalIgnoreCase))
            {
                fileName = fileName.Substring(0, fileName.Length - 7); // Remove 7 characters: .cy.js
                extensionRemoved = true;
            }
            else if (fileName.EndsWith(".ts", StringComparison.OrdinalIgnoreCase))
            {
                fileName = fileName.Substring(0, fileName.Length - 3);
                extensionRemoved = true;
            }
            else if (fileName.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
            {
                fileName = fileName.Substring(0, fileName.Length - 3);
                extensionRemoved = true;
            }
        } while (extensionRemoved);

        return fileName;
    }
}
