using SynTA.Models.Domain;

namespace SynTA.Services.AI.Prompts;

/// <summary>
/// Service for building AI prompts for Gherkin scenario and Cypress script generation.
/// Centralizes prompt engineering logic to eliminate duplication across AI providers.
/// </summary>
public interface IPromptService
{
    /// <summary>
    /// Builds a prompt for generating Gherkin scenarios from a user story.
    /// </summary>
    /// <param name="title">The user story title</param>
    /// <param name="userStoryText">The user story text</param>
    /// <param name="description">Optional description</param>
    /// <param name="acceptanceCriteria">Optional acceptance criteria</param>
    /// <param name="maxScenarios">Maximum number of scenarios (0 for auto)</param>
    /// <param name="language">Language code for output</param>
    /// <returns>The constructed prompt string</returns>
    string BuildGherkinPrompt(
        string title,
        string userStoryText,
        string? description = null,
        string? acceptanceCriteria = null,
        int maxScenarios = 10,
        string language = "en");

    /// <summary>
    /// Builds a prompt for generating Cypress scripts from Gherkin scenarios.
    /// </summary>
    /// <param name="gherkinScenarios">The Gherkin scenarios to convert</param>
    /// <param name="targetUrl">The target URL for the tests</param>
    /// <param name="userStoryTitle">The user story title for context</param>
    /// <param name="userStoryText">The user story text for context</param>
    /// <param name="description">Optional description</param>
    /// <param name="acceptanceCriteria">Optional acceptance criteria</param>
    /// <param name="htmlContext">Optional HTML context for selector generation</param>
    /// <param name="scriptLanguage">The target script language (TypeScript or JavaScript)</param>
    /// <param name="hasScreenshot">Indicates whether a screenshot is provided for multimodal context</param>
    /// <param name="hasPageMetadata">Whether page metadata is included in extraction context</param>
    /// <param name="hasUiElementMap">Whether UI element mapping is included in extraction context</param>
    /// <param name="hasAccessibilityTree">Whether accessibility tree is included in extraction context</param>
    /// <returns>The constructed prompt string</returns>
    string BuildCypressPrompt(
        string gherkinScenarios,
        string targetUrl,
        string userStoryTitle,
        string userStoryText,
        string? description = null,
        string? acceptanceCriteria = null,
        string? htmlContext = null,
        CypressScriptLanguage scriptLanguage = CypressScriptLanguage.TypeScript,
        bool hasScreenshot = false,
        bool hasPageMetadata = true,
        bool hasUiElementMap = true,
        bool hasAccessibilityTree = true);

    /// <summary>
    /// Converts a language code to a human-readable language name for use in prompts.
    /// </summary>
    /// <param name="languageCode">The ISO language code</param>
    /// <returns>The human-readable language name</returns>
    string GetLanguageName(string languageCode);
}
