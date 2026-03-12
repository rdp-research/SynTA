using SynTA.Models.Domain;
using SynTA.Services.AI.Prompts;
using SynTA.Services.AI.TextProcessing;

namespace SynTA.Services.AI;

/// <summary>
/// Abstract base class for AI generation services that provides common functionality
/// for generating Gherkin scenarios and Cypress scripts.
/// Concrete implementations only need to implement provider-specific API calls.
/// </summary>
public abstract class BaseAIService : IAIGenerationService
{
    protected readonly ILogger Logger;
    protected readonly IPromptService PromptService;
    protected readonly float Temperature;
    private AIModelTier _modelTier = AIModelTier.Fast;

    public abstract string ProviderName { get; }

    public AIModelTier ModelTier
    {
        get => _modelTier;
        set => _modelTier = value;
    }

    public virtual string? CustomModelName { get; set; }

    /// <summary>
    /// Gets the model name based on the current model tier.
    /// Must be implemented by concrete classes to return provider-specific model names.
    /// </summary>
    protected abstract string CurrentModelName { get; }

    protected BaseAIService(
        ILogger logger,
        IPromptService promptService,
        float temperature)
    {
        Logger = logger;
        PromptService = promptService;
        Temperature = temperature;
    }

    public async Task<string> GenerateGherkinScenariosAsync(
        string userStoryTitle,
        string userStoryText,
        string? description = null,
        string? acceptanceCriteria = null,
        int maxScenarios = 10,
        string language = "en")
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];
        Logger.LogInformation(
            "[{OperationId}] Starting Gherkin generation via {Provider} - UserStory: '{Title}', MaxScenarios: {MaxScenarios}, Language: {Language}, ModelTier: {ModelTier}",
            operationId, ProviderName, userStoryTitle, maxScenarios, language, _modelTier);

        try
        {
            var prompt = PromptService.BuildGherkinPrompt(userStoryTitle, userStoryText, description, acceptanceCriteria, maxScenarios, language);
            Logger.LogDebug("[{OperationId}] Constructed Gherkin prompt - Length: {Length} chars", operationId, prompt.Length);

            var response = await GenerateGherkinContentAsync(prompt, language);

            // Strip any markdown code blocks the AI may have included
            response = AIResponseCleaner.StripMarkdownCodeBlocks(response);

            Logger.LogInformation(
                "[{OperationId}] Successfully generated Gherkin scenarios for '{Title}' - ResponseLength: {ResponseLength} chars, Model: {Model}",
                operationId, userStoryTitle, response.Length, CurrentModelName);
            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "[{OperationId}] Failed to generate Gherkin scenarios for '{Title}' - Model: {Model}, Error: {ErrorMessage}",
                operationId, userStoryTitle, CurrentModelName, ex.Message);
            throw;
        }
    }

    public async Task<string> GenerateCypressScriptAsync(
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
        bool hasAccessibilityTree = true)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];
        Logger.LogInformation(
            "[{OperationId}] Starting Cypress script generation via {Provider} - TargetUrl: {Url}, UserStory: '{Title}', GherkinLength: {GherkinLength} chars, HtmlContextProvided: {HasHtml}, ScreenshotProvided: {HasScreenshot}, ScriptLanguage: {ScriptLanguage}, ModelTier: {ModelTier}",
            operationId, ProviderName, targetUrl, userStoryTitle, gherkinScenarios?.Length ?? 0, !string.IsNullOrEmpty(htmlContext), screenshot != null, scriptLanguage, _modelTier);

        try
        {
            var prompt = PromptService.BuildCypressPrompt(
                gherkinScenarios ?? string.Empty,
                targetUrl,
                userStoryTitle,
                userStoryText,
                description,
                acceptanceCriteria,
                htmlContext,
                scriptLanguage,
                screenshot != null,
                hasPageMetadata,
                hasUiElementMap,
                hasAccessibilityTree);

            Logger.LogDebug("[{OperationId}] Constructed Cypress prompt - PromptLength: {PromptLength} chars, HtmlContextLength: {HtmlLength} chars, ScreenshotSize: {ScreenshotSize} bytes",
                operationId, prompt.Length, htmlContext?.Length ?? 0, screenshot?.Length ?? 0);

            if (!string.IsNullOrEmpty(htmlContext))
            {
                Logger.LogDebug("[{OperationId}] HTML context preview (first 200 chars): {Preview}...",
                    operationId, htmlContext.Length > 200 ? htmlContext[..200] : htmlContext);
            }
            else
            {
                Logger.LogWarning("[{OperationId}] No HTML context provided - selectors may be less accurate", operationId);
            }

            if (screenshot != null)
            {
                Logger.LogInformation("[{OperationId}] Sending multimodal request to {Provider} API - Model: {Model}, PromptLength: {Length} chars, ScreenshotSize: {ScreenshotSize} bytes",
                    operationId, ProviderName, CurrentModelName, prompt.Length, screenshot.Length);
            }
            else
            {
                Logger.LogInformation("[{OperationId}] Sending request to {Provider} API - Model: {Model}, PromptLength: {Length} chars",
                    operationId, ProviderName, CurrentModelName, prompt.Length);
            }

            var response = await GenerateCypressContentAsync(prompt, screenshot);

            Logger.LogDebug("[{OperationId}] Received raw {Provider} response - Length: {Length} chars", operationId, ProviderName, response.Length);

            // Strip any markdown code blocks the AI may have included
            response = AIResponseCleaner.StripMarkdownCodeBlocks(response);

            // Remove any raw Gherkin syntax that the AI may have incorrectly included
            response = AIResponseCleaner.StripRawGherkinSyntax(response);

            // Validate we still have content after stripping
            if (string.IsNullOrWhiteSpace(response))
            {
                Logger.LogError("[{OperationId}] Cypress script content is empty after processing", operationId);
                throw new InvalidOperationException("Generated Cypress script content is empty");
            }

            Logger.LogInformation(
                "[{OperationId}] Successfully generated Cypress script for '{Url}' - ResponseLength: {ResponseLength} chars, Model: {Model}",
                operationId, targetUrl, response.Length, CurrentModelName);
            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "[{OperationId}] Failed to generate Cypress script for '{Url}' - Model: {Model}, UserStory: '{Title}', Error: {ErrorMessage}",
                operationId, targetUrl, CurrentModelName, userStoryTitle, ex.Message);
            throw;
        }
    }

    public abstract Task<bool> TestConnectionAsync();

    /// <summary>
    /// Generates Gherkin content using the provider-specific API.
    /// </summary>
    /// <param name="prompt">The user prompt for generating Gherkin scenarios</param>
    /// <param name="language">The language code for the scenarios</param>
    /// <returns>The generated content as a string</returns>
    protected abstract Task<string> GenerateGherkinContentAsync(string prompt, string language);

    /// <summary>
    /// Generates Cypress script content using the provider-specific API.
    /// </summary>
    /// <param name="prompt">The user prompt for generating the Cypress script</param>
    /// <param name="screenshot">Optional screenshot bytes for multimodal generation</param>
    /// <returns>The generated content as a string</returns>
    protected abstract Task<string> GenerateCypressContentAsync(string prompt, byte[]? screenshot);
}
