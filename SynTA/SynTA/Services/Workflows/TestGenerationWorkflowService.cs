using SynTA.Models.Domain;
using SynTA.Services.AI;
using SynTA.Services.Database;
using SynTA.Services.Utilities;

namespace SynTA.Services.Workflows;

/// <summary>
/// Service responsible for orchestrating the complete test generation workflow.
/// Coordinates between AI services, database services, and HTML context extraction.
/// Extracts complex workflow logic from controllers for better testability and maintainability.
/// </summary>
public class TestGenerationWorkflowService : ITestGenerationWorkflowService
{
    private readonly IUserStoryService _userStoryService;
    private readonly IProjectService _projectService;
    private readonly IGherkinScenarioService _gherkinService;
    private readonly ICypressScriptService _cypressService;
    private readonly IAIServiceFactory _aiServiceFactory;
    private readonly ISettingsService _settingsService;
    private readonly IHtmlContextService _htmlContextService;
    private readonly IFileNameService _fileNameService;
    private readonly ILogger<TestGenerationWorkflowService> _logger;

    public TestGenerationWorkflowService(
        IUserStoryService userStoryService,
        IProjectService projectService,
        IGherkinScenarioService gherkinService,
        ICypressScriptService cypressService,
        IAIServiceFactory aiServiceFactory,
        ISettingsService settingsService,
        IHtmlContextService htmlContextService,
        IFileNameService fileNameService,
        ILogger<TestGenerationWorkflowService> logger)
    {
        _userStoryService = userStoryService;
        _projectService = projectService;
        _gherkinService = gherkinService;
        _cypressService = cypressService;
        _aiServiceFactory = aiServiceFactory;
        _settingsService = settingsService;
        _htmlContextService = htmlContextService;
        _fileNameService = fileNameService;
        _logger = logger;
    }

    public async Task<TestGenerationResult> GenerateCompleteTestSuiteAsync(int userStoryId, string userId, string? targetUrl = null)
    {
        _logger.LogInformation(
            "Starting complete test suite generation for UserStoryId: {UserStoryId}, UserId: {UserId}, TargetUrl: {TargetUrl}",
            userStoryId, userId, targetUrl ?? "none");

        try
        {
            // Step 0: Validate user story exists and user has access
            var userStory = await ValidateAndGetUserStoryAsync(userStoryId, userId);

            // Step 1: Get user settings
            var settings = await _settingsService.GetSettingsAsync(userId);
            var aiService = _aiServiceFactory.CreateService(settings.PreferredAIProvider, settings.PreferredModelTier);

            if (settings.PreferredAIProvider == AIProviderType.OpenRouter)
            {
                aiService.CustomModelName = settings.OpenRouterModelName;
            }

            _logger.LogInformation(
                "Using AI Provider: {Provider}, ModelTier: {ModelTier}, Language: {Language}, CypressLanguage: {CypressLanguage}",
                settings.PreferredAIProvider, settings.PreferredModelTier, settings.PreferredLanguage, settings.PreferredCypressLanguage);

            // Step 2: Generate Gherkin scenarios
            var gherkinScenarioId = await GenerateGherkinScenariosAsync(
                userStory,
                aiService,
                settings);

            // Step 3: Fetch HTML context if URL provided (optional, non-blocking)
            var (htmlContext, screenshot) = await FetchHtmlContextAsync(targetUrl, settings);

            // Step 4: Generate Cypress script
            var cypressScriptId = await GenerateCypressScriptAsync(
                userStory,
                gherkinScenarioId,
                aiService,
                settings,
                targetUrl,
                htmlContext,
                screenshot);

            _logger.LogInformation(
                "Successfully completed test suite generation - UserStoryId: {UserStoryId}, GherkinScenarioId: {GherkinId}, CypressScriptId: {CypressId}",
                userStoryId, gherkinScenarioId, cypressScriptId);

            return new TestGenerationResult
            {
                Success = true,
                GherkinScenarioId = gherkinScenarioId,
                CypressScriptId = cypressScriptId,
                DetailedInfo = $"Generated Gherkin scenario (ID: {gherkinScenarioId}) and Cypress script (ID: {cypressScriptId})"
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt for UserStoryId: {UserStoryId}, UserId: {UserId}", userStoryId, userId);
            return new TestGenerationResult
            {
                Success = false,
                ErrorMessage = "You don't have permission to access this user story."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating complete test suite for UserStoryId: {UserStoryId}", userStoryId);
            return new TestGenerationResult
            {
                Success = false,
                ErrorMessage = "An error occurred during test generation. Please try again.",
                DetailedInfo = ex.Message
            };
        }
    }

    /// <summary>
    /// Validates that the user story exists and the user has access to it.
    /// </summary>
    private async Task<UserStory> ValidateAndGetUserStoryAsync(int userStoryId, string userId)
    {
        var userStory = await _userStoryService.GetUserStoryByIdAsync(userStoryId);
        if (userStory == null)
        {
            _logger.LogWarning("User story not found - UserStoryId: {UserStoryId}", userStoryId);
            throw new KeyNotFoundException($"User story with ID {userStoryId} not found.");
        }

        var hasAccess = await _projectService.UserOwnsProjectAsync(userStory.ProjectId, userId);
        if (!hasAccess)
        {
            _logger.LogWarning(
                "User does not own project - UserStoryId: {UserStoryId}, ProjectId: {ProjectId}, UserId: {UserId}",
                userStoryId, userStory.ProjectId, userId);
            throw new UnauthorizedAccessException($"User {userId} does not have access to project {userStory.ProjectId}");
        }

        return userStory;
    }

    /// <summary>
    /// Generates Gherkin scenarios using AI and saves them to the database.
    /// </summary>
    private async Task<int> GenerateGherkinScenariosAsync(
        UserStory userStory,
        IAIGenerationService aiService,
        UserSettings settings)
    {
        _logger.LogInformation("Generating Gherkin scenarios for UserStoryId: {UserStoryId}", userStory.Id);

        var gherkinContent = await aiService.GenerateGherkinScenariosAsync(
            userStory.Title,
            userStory.UserStoryText,
            userStory.Description,
            userStory.AcceptanceCriteria,
            settings.MaxScenariosPerGeneration,
            settings.PreferredLanguage);

        var gherkinScenario = new GherkinScenario
        {
            UserStoryId = userStory.Id,
            Title = $"Test Scenarios for: {userStory.Title}",
            Content = gherkinContent
        };

        await _gherkinService.CreateScenarioAsync(gherkinScenario);

        _logger.LogInformation(
            "Gherkin scenario created - ScenarioId: {ScenarioId}, UserStoryId: {UserStoryId}, ContentLength: {Length} chars",
            gherkinScenario.Id, userStory.Id, gherkinContent.Length);

        return gherkinScenario.Id;
    }

    /// <summary>
    /// Fetches HTML context from the target URL if provided.
    /// Returns (htmlContext, screenshot) tuple. Both can be null if fetch fails or URL not provided.
    /// </summary>
    private async Task<(string? htmlContext, byte[]? screenshot)> FetchHtmlContextAsync(string? targetUrl, UserSettings settings)
    {
        if (!settings.WebExtractionEnabledForCypressGeneration)
        {
            _logger.LogInformation("Web extraction is disabled in user settings, skipping HTML context fetch");
            return (null, null);
        }

        if (string.IsNullOrWhiteSpace(targetUrl))
        {
            _logger.LogInformation("No target URL provided, skipping HTML context fetch");
            return (null, null);
        }

        try
        {
            _logger.LogInformation("Fetching HTML context from URL: {Url}", targetUrl);

            var options = new HtmlFetchOptions
            {
                CaptureScreenshot = settings.VisionApiEnabled,
                IncludePageMetadata = settings.IncludeWebPageMetadataInExtraction,
                IncludeUiElementMap = settings.IncludeUiElementMapInExtraction,
                IncludeAccessibilityTree = settings.IncludeAccessibilityTreeInExtraction,
                IncludeSimplifiedHtml = settings.IncludeSimplifiedHtmlInExtraction
            };

            var contextResult = await _htmlContextService.FetchAndSimplifyHtmlAsync(targetUrl, options);

            _logger.LogInformation(
                "HTML context fetched successfully - Url: {Url}, ContentLength: {Length} chars, ScreenshotSize: {ScreenshotSize} bytes",
                targetUrl, contextResult.HtmlContent?.Length ?? 0, contextResult.Screenshot?.Length ?? 0);

            return (contextResult.HtmlContent, contextResult.Screenshot);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to fetch HTML context from URL: {Url}. Continuing without HTML context.",
                targetUrl);
            return (null, null);
        }
    }

    /// <summary>
    /// Generates Cypress script using AI and saves it to the database.
    /// </summary>
    private async Task<int> GenerateCypressScriptAsync(
        UserStory userStory,
        int gherkinScenarioId,
        IAIGenerationService aiService,
        UserSettings settings,
        string? targetUrl,
        string? htmlContext,
        byte[]? screenshot)
    {
        _logger.LogInformation(
            "Generating Cypress script for UserStoryId: {UserStoryId}, GherkinScenarioId: {GherkinId}",
            userStory.Id, gherkinScenarioId);

        // Get the Gherkin content
        var gherkinScenario = await _gherkinService.GetScenarioByIdAsync(gherkinScenarioId);
        var gherkinContent = gherkinScenario?.Content ?? string.Empty;

        var hasExtractionContext =
            settings.WebExtractionEnabledForCypressGeneration &&
            !string.IsNullOrWhiteSpace(targetUrl);

        // Generate Cypress script (with optional HTML context and screenshot for multimodal processing)
        var cypressContent = await aiService.GenerateCypressScriptAsync(
            gherkinContent,
            targetUrl ?? string.Empty,
            userStory.Title,
            userStory.UserStoryText,
            userStory.Description,
            userStory.AcceptanceCriteria,
            htmlContext,
            settings.PreferredCypressLanguage,
                screenshot,
            hasExtractionContext && settings.IncludeWebPageMetadataInExtraction,
            hasExtractionContext && settings.IncludeUiElementMapInExtraction,
            hasExtractionContext && settings.IncludeAccessibilityTreeInExtraction);

        // Generate file name
        var fileName = _fileNameService.GenerateCypressFileName(
            settings.DefaultCypressFileNamePattern,
            userStory.Title,
            userStory.Project?.Name ?? "project",
            settings.PreferredCypressLanguage);

        // Save to database
        var cypressScript = new CypressScript
        {
            UserStoryId = userStory.Id,
            FileName = fileName,
            Content = cypressContent,
            TargetUrl = targetUrl
        };

        await _cypressService.CreateScriptAsync(cypressScript);

        _logger.LogInformation(
            "Cypress script created - ScriptId: {ScriptId}, UserStoryId: {UserStoryId}, FileName: {FileName}, ContentLength: {Length} chars",
            cypressScript.Id, userStory.Id, fileName, cypressContent.Length);

        return cypressScript.Id;
    }
}
