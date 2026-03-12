using SynTA.Models.Domain;

namespace SynTA.Services.Utilities;

/// <summary>
/// Service for generating and sanitizing filenames for exported files.
/// Centralizes filename generation logic to eliminate duplication across controllers and services.
/// </summary>
public interface IFileNameService
{
    /// <summary>
    /// Generates a Cypress filename based on the provided pattern, user story title, project name, and script language.
    /// </summary>
    /// <param name="pattern">The filename pattern with placeholders (e.g., "{UserStory}_{Date}")</param>
    /// <param name="storyTitle">The user story title</param>
    /// <param name="projectName">The project name</param>
    /// <param name="language">The Cypress script language (TypeScript or JavaScript)</param>
    /// <returns>The generated filename with appropriate extension</returns>
    string GenerateCypressFileName(string pattern, string storyTitle, string projectName, CypressScriptLanguage language);

    /// <summary>
    /// Generates a Gherkin filename based on the user story title.
    /// </summary>
    /// <param name="storyTitle">The user story title</param>
    /// <returns>The generated filename with .feature extension</returns>
    string GenerateGherkinFileName(string storyTitle);

    /// <summary>
    /// Sanitizes a string to be safe for use in filenames.
    /// Removes invalid characters, replaces spaces with hyphens, and limits length.
    /// </summary>
    /// <param name="input">The string to sanitize</param>
    /// <returns>The sanitized string safe for use in filenames</returns>
    string Sanitize(string input);

    /// <summary>
    /// Strips any Cypress-related file extensions from a filename.
    /// Handles: .cy.ts, .cy.js, .ts, .js (in that order to handle compound extensions)
    /// </summary>
    /// <param name="fileName">The filename to strip extensions from</param>
    /// <returns>The filename without Cypress-related extensions</returns>
    string StripCypressExtensions(string fileName);
}
