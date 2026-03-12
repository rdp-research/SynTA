using SynTA.Models.Domain;

namespace SynTA.Services.AI
{
    /// <summary>
    /// Defines the contract for AI-powered test generation services.
    /// This interface abstracts the AI provider, allowing for multiple implementations
    /// (e.g., OpenAI, Google Gemini) to be used interchangeably.
    /// </summary>
    public interface IAIGenerationService
    {
        /// <summary>
        /// Gets the name of the AI provider (e.g., "OpenAI", "Gemini")
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Gets or sets the current model tier for this service instance.
        /// </summary>
        AIModelTier ModelTier { get; set; }

        /// <summary>
        /// Optional custom model identifier for providers that support explicit model selection.
        /// </summary>
        string? CustomModelName { get; set; }

        /// <summary>
        /// Generates Gherkin test scenarios from a user story description
        /// </summary>
        /// <param name="userStoryTitle">The title of the user story (for context and organization)</param>
        /// <param name="userStoryText">The actual user story text (e.g., "As a user, I want to...")</param>
        /// <param name="description">Optional additional description or context</param>
        /// <param name="acceptanceCriteria">Optional acceptance criteria for the user story</param>
        /// <param name="maxScenarios">Maximum number of scenarios to generate (default: 10)</param>
        /// <param name="language">Language for the generated scenarios (e.g., "en", "uk", "fr")</param>
        /// <returns>AI-generated Gherkin scenarios as a string</returns>
        Task<string> GenerateGherkinScenariosAsync(
            string userStoryTitle,
            string userStoryText,
            string? description = null,
            string? acceptanceCriteria = null,
            int maxScenarios = 10,
            string language = "en");

        /// <summary>
        /// Generates Cypress test script from Gherkin scenarios
        /// </summary>
        /// <param name="gherkinScenarios">The Gherkin scenarios to convert</param>
        /// <param name="targetUrl">The URL of the web page to test</param>
        /// <param name="userStoryTitle">The title of the user story (for context)</param>
        /// <param name="userStoryText">The actual user story text (for context)</param>
        /// <param name="description">Optional additional description (for context)</param>
        /// <param name="acceptanceCriteria">Optional acceptance criteria of the user story (for context)</param>
        /// <param name="htmlContext">Optional HTML context of the page</param>
        /// <param name="scriptLanguage">The programming language for the generated script (TypeScript or JavaScript)</param>
        /// <param name="screenshot">Optional screenshot of the page as JPEG bytes for multimodal AI processing</param>
        /// <param name="hasPageMetadata">Whether extraction context includes page metadata</param>
        /// <param name="hasUiElementMap">Whether extraction context includes UI element mapping</param>
        /// <param name="hasAccessibilityTree">Whether extraction context includes accessibility tree</param>
        /// <returns>AI-generated Cypress test script</returns>
        Task<string> GenerateCypressScriptAsync(
            string gherkinScenarios, 
            string targetUrl,
            string userStoryTitle,
            string userStoryText,
            string? description = null,
            string? acceptanceCriteria = null,
            string? htmlContext = null,
            CypressScriptLanguage scriptLanguage = CypressScriptLanguage.TypeScript,
            byte[]? screenshot = null,
            bool hasPageMetadata = true,
            bool hasUiElementMap = true,
            bool hasAccessibilityTree = true);

        /// <summary>
        /// Tests the connection to the AI provider
        /// </summary>
        /// <returns>True if the connection is successful, false otherwise</returns>
        Task<bool> TestConnectionAsync();
    }
}
