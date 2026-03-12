using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SynTA.Areas.User.Models;
using SynTA.Models.Domain;
using SynTA.Models.Testing;
using SynTA.Services.AI;
using SynTA.Services.Database;
using SynTA.Services.Export;
using SynTA.Services.Testing;
using SynTA.Services.Utilities;

namespace SynTA.Areas.User.Controllers;

[Area("User")]
[Authorize]
public class CypressController : Controller
{
    private readonly ICypressScriptService _cypressService;
    private readonly IGherkinScenarioService _gherkinService;
    private readonly IUserStoryService _userStoryService;
    private readonly IProjectService _projectService;
    private readonly IAIServiceFactory _aiServiceFactory;
    private readonly ISettingsService _settingsService;
    private readonly IHtmlContextService _htmlContextService;
    private readonly IFileExportService _fileExportService;
    private readonly IFileNameService _fileNameService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<CypressController> _logger;
    private readonly ICypressRunnerService _cypressRunnerService;

    public CypressController(
        ICypressScriptService cypressService,
        IGherkinScenarioService gherkinService,
        IUserStoryService userStoryService,
        IProjectService projectService,
        IAIServiceFactory aiServiceFactory,
        ISettingsService settingsService,
        IHtmlContextService htmlContextService,
        IFileExportService fileExportService,
        IFileNameService fileNameService,
        UserManager<ApplicationUser> userManager,
        ILogger<CypressController> logger,
        ICypressRunnerService cypressRunnerService)
    {
        _cypressService = cypressService;
        _gherkinService = gherkinService;
        _userStoryService = userStoryService;
        _projectService = projectService;
        _aiServiceFactory = aiServiceFactory;
        _settingsService = settingsService;
        _htmlContextService = htmlContextService;
        _fileExportService = fileExportService;
        _fileNameService = fileNameService;
        _userManager = userManager;
        _logger = logger;
        _cypressRunnerService = cypressRunnerService;
    }

    // GET: User/Cypress/Index?userStoryId=5
    public async Task<IActionResult> Index(int userStoryId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account", new { area = "" });
        }

        var userStory = await _userStoryService.GetUserStoryByIdAsync(userStoryId);
        if (userStory == null)
        {
            return NotFound();
        }

        // Verify user owns the project
        if (!await _projectService.UserOwnsProjectAsync(userStory.ProjectId, userId))
        {
            return Forbid();
        }

        ViewBag.UserStoryId = userStoryId;
        ViewBag.UserStoryTitle = userStory.Title;
        ViewBag.ProjectName = userStory.Project?.Name;

        var scripts = await _cypressService.GetScriptsByUserStoryIdAsync(userStoryId);

        var viewModels = scripts.Select(s => new CypressReviewViewModel
        {
            Id = s.Id,
            UserStoryId = s.UserStoryId,
            UserStoryTitle = userStory.Title,
            ProjectName = userStory.Project?.Name ?? "",
            FileName = s.FileName,
            Content = s.Content,
            TargetUrl = s.TargetUrl,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        }).ToList();

        return View(viewModels);
    }

    // GET: User/Cypress/Configure?gherkinScenarioId=5
    public async Task<IActionResult> Configure(int gherkinScenarioId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account", new { area = "" });
        }

        var scenario = await _gherkinService.GetScenarioByIdAsync(gherkinScenarioId);
        if (scenario == null)
        {
            return NotFound();
        }

        // Verify user owns the project
        if (scenario.UserStory?.ProjectId != null &&
            !await _projectService.UserOwnsProjectAsync(scenario.UserStory.ProjectId, userId))
        {
            return Forbid();
        }

        var settings = await _settingsService.GetSettingsAsync(userId);

        var viewModel = new CypressConfigureViewModel
        {
            GherkinScenarioId = scenario.Id,
            GherkinTitle = scenario.Title,
            GherkinContent = scenario.Content,
            UserStoryId = scenario.UserStoryId,
            UserStoryTitle = scenario.UserStory?.Title ?? "",
            ProjectName = scenario.UserStory?.Project?.Name ?? "",
            FetchHtmlContext = settings.WebExtractionEnabledForCypressGeneration
        };

        return View(viewModel);
    }

    // POST: User/Cypress/Generate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(CypressConfigureViewModel model)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("[{OperationId}] Cypress generation aborted - Invalid model state for GherkinScenarioId: {ScenarioId}",
                operationId, model.GherkinScenarioId);
            return View("Configure", model);
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("[{OperationId}] Cypress generation aborted - User not authenticated", operationId);
            return RedirectToAction("Login", "Account", new { area = "" });
        }

        var scenario = await _gherkinService.GetScenarioByIdAsync(model.GherkinScenarioId);
        if (scenario == null)
        {
            _logger.LogWarning("[{OperationId}] Cypress generation aborted - Gherkin scenario not found: {ScenarioId}",
                operationId, model.GherkinScenarioId);
            return NotFound();
        }

        // Verify user owns the project
        if (scenario.UserStory?.ProjectId != null &&
            !await _projectService.UserOwnsProjectAsync(scenario.UserStory.ProjectId, userId))
        {
            return Forbid();
        }

        // Get user settings for AI provider and model tier
        var settings = await _settingsService.GetSettingsAsync(userId);

        try
        {
            string? htmlContext = null;
            byte[]? screenshot = null;

            var shouldFetchHtmlContext = model.FetchHtmlContext && settings.WebExtractionEnabledForCypressGeneration;
            var hasPageMetadata = shouldFetchHtmlContext && settings.IncludeWebPageMetadataInExtraction;
            var hasUiElementMap = shouldFetchHtmlContext && settings.IncludeUiElementMapInExtraction;
            var hasAccessibilityTree = shouldFetchHtmlContext && settings.IncludeAccessibilityTreeInExtraction;

            // Fetch HTML context if requested
            if (shouldFetchHtmlContext)
            {
                try
                {
                    _logger.LogInformation("[{OperationId}] Fetching HTML context for Cypress generation - TargetUrl: {Url}",
                        operationId, model.TargetUrl);

                    var fetchOptions = new HtmlFetchOptions
                    {
                        CaptureScreenshot = settings.VisionApiEnabled,
                        IncludePageMetadata = settings.IncludeWebPageMetadataInExtraction,
                        IncludeUiElementMap = settings.IncludeUiElementMapInExtraction,
                        IncludeAccessibilityTree = settings.IncludeAccessibilityTreeInExtraction,
                        IncludeSimplifiedHtml = settings.IncludeSimplifiedHtmlInExtraction
                    };

                    var contextResult = await _htmlContextService.FetchAndSimplifyHtmlAsync(model.TargetUrl, fetchOptions);
                    htmlContext = contextResult.HtmlContent;
                    screenshot = contextResult.Screenshot;
                    _logger.LogInformation("[{OperationId}] HTML context retrieved successfully - Length: {Length} chars, ScreenshotCaptured: {HasScreenshot}, ScreenshotSize: {ScreenshotSize} bytes, VisionApiEnabled: {VisionEnabled}",
                        operationId, htmlContext?.Length ?? 0, screenshot != null, screenshot?.Length ?? 0, settings.VisionApiEnabled);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[{OperationId}] HTML fetch failed - aborting Cypress generation - TargetUrl: {Url}, Error: {ErrorMessage}",
                        operationId, model.TargetUrl, ex.Message);
                    ModelState.AddModelError(string.Empty, "Failed to fetch HTML context from the target URL. Script generation was aborted to prevent generating inaccurate selectors. Please ensure the URL is accessible and try again, or uncheck 'Fetch HTML Context' to proceed without it.");
                    return View("Configure", model);
                }
            }

            // Create AI service with user's preferred provider and model tier
            var aiService = _aiServiceFactory.CreateService(settings.PreferredAIProvider, settings.PreferredModelTier);
            if (settings.PreferredAIProvider == AIProviderType.OpenRouter)
            {
                aiService.CustomModelName = settings.OpenRouterModelName;
            }
            _logger.LogInformation(
                "[{OperationId}] Starting Cypress script generation - Provider: {Provider}, ModelTier: {ModelTier}, TargetUrl: {Url}, UserId: {UserId}",
                operationId, settings.PreferredAIProvider, settings.PreferredModelTier, model.TargetUrl, userId);

            // Get User Story full context (title, user story text, description, acceptance criteria)
            var userStoryTitle = scenario.UserStory?.Title ?? "Unknown User Story";
            var userStoryText = scenario.UserStory?.UserStoryText ?? "";
            var userStoryDescription = scenario.UserStory?.Description;
            var acceptanceCriteria = scenario.UserStory?.AcceptanceCriteria;

            // Generate Cypress script using AI with full context (including screenshot for multimodal processing)
            var cypressContent = await aiService.GenerateCypressScriptAsync(
                scenario.Content,
                model.TargetUrl,
                userStoryTitle,
                userStoryText,
                userStoryDescription,
                acceptanceCriteria,
                htmlContext,
                settings.PreferredCypressLanguage,
                screenshot,
                hasPageMetadata,
                hasUiElementMap,
                hasAccessibilityTree);

            // Validate that we got actual content from the AI
            if (string.IsNullOrWhiteSpace(cypressContent))
            {
                _logger.LogError("[{OperationId}] AI returned empty content for Gherkin scenario {ScenarioId}", operationId, model.GherkinScenarioId);
                TempData["ErrorMessage"] = "The AI service returned an empty response. Please try again.";
                return View("Configure", model);
            }

            // Create file name from user story title using the centralized service
            var fileName = _fileNameService.GenerateCypressFileName(
                settings.DefaultCypressFileNamePattern,
                scenario.UserStory?.Title ?? "test",
                scenario.UserStory?.Project?.Name ?? "project",
                settings.PreferredCypressLanguage);

            // Save to database
            var cypressScript = new CypressScript
            {
                UserStoryId = scenario.UserStoryId,
                FileName = fileName,
                Content = cypressContent,
                TargetUrl = model.TargetUrl
            };

            await _cypressService.CreateScriptAsync(cypressScript);

            _logger.LogInformation(
                "[{OperationId}] Cypress script generated and saved successfully - ScriptId: {ScriptId}, FileName: {FileName}, ContentLength: {Length} chars, UserId: {UserId}",
                operationId, cypressScript.Id, fileName, cypressContent.Length, userId);

            TempData["SuccessMessage"] = "Cypress script generated successfully!";
            return RedirectToAction(nameof(Index), new { userStoryId = scenario.UserStoryId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[{OperationId}] Cypress generation failed - GherkinScenarioId: {ScenarioId}, TargetUrl: {Url}, UserId: {UserId}, Error: {ErrorMessage}",
                operationId, model.GherkinScenarioId, model.TargetUrl, userId, ex.Message);
            TempData["ErrorMessage"] = "An error occurred while generating the Cypress script. Please try again.";
            return View("Configure", model);
        }
    }

    // GET: User/Cypress/ReviewAndExport/5
    public async Task<IActionResult> ReviewAndExport(int id)
    {
        var script = await _cypressService.GetScriptByIdAsync(id);
        if (script == null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account", new { area = "" });
        }

        // Verify user owns the project
        if (script.UserStory?.ProjectId != null &&
            !await _projectService.UserOwnsProjectAsync(script.UserStory.ProjectId, userId))
        {
            return Forbid();
        }

        var viewModel = new CypressReviewViewModel
        {
            Id = script.Id,
            UserStoryId = script.UserStoryId,
            UserStoryTitle = script.UserStory?.Title ?? "",
            ProjectName = script.UserStory?.Project?.Name ?? "",
            FileName = script.FileName,
            Content = script.Content,
            TargetUrl = script.TargetUrl,
            CreatedAt = script.CreatedAt,
            UpdatedAt = script.UpdatedAt
        };

        return View(viewModel);
    }

    // POST: User/Cypress/RunTest/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunTest(int id)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];
        
        var script = await _cypressService.GetScriptByIdAsync(id);
        if (script == null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account", new { area = "" });
        }

        // Verify user owns the project
        if (script.UserStory?.ProjectId != null &&
            !await _projectService.UserOwnsProjectAsync(script.UserStory.ProjectId, userId))
        {
            return Forbid();
        }

        _logger.LogInformation(
            "[{OperationId}] Running Cypress test - ScriptId: {ScriptId}, TargetUrl: {Url}",
            operationId, id, script.TargetUrl);

        try
        {
            var runId = await _cypressRunnerService.StartTestRunContentAsync(
                script.Content,
                script.TargetUrl,
                script.FileName);

            _logger.LogInformation(
                "[{OperationId}] Cypress test started asynchronously - RunId: {RunId}",
                operationId, runId);

            // Redirect to launching page which will poll for process start
            return RedirectToAction(nameof(Launching), new { runId, scriptId = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[{OperationId}] Cypress test run failed to start - ScriptId: {ScriptId}, Error: {Error}",
                operationId, id, ex.Message);
            TempData["ErrorMessage"] = $"Failed to start test: {ex.Message}";
            return RedirectToAction(nameof(ReviewAndExport), new { id });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Launching(Guid runId, int scriptId)
    {
        var script = await _cypressService.GetScriptByIdAsync(scriptId);
        if (script == null) return NotFound();

        ViewBag.RunId = runId;
        ViewBag.ScriptId = scriptId;
        ViewBag.FileName = script.FileName;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetRunStatus(Guid runId)
    {
        var status = await _cypressRunnerService.GetRunStatusAsync(runId);
        if (status == null) return NotFound();
        return Json(status);
    }

    // POST: User/Cypress/Download/5
    [HttpPost]
    public async Task<IActionResult> Download(int id)
    {
        var script = await _cypressService.GetScriptByIdAsync(id);
        if (script == null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account", new { area = "" });
        }

        // Verify user owns the project
        if (script.UserStory?.ProjectId != null &&
            !await _projectService.UserOwnsProjectAsync(script.UserStory.ProjectId, userId))
        {
            return Forbid();
        }

        var (fileContent, contentType, fileName) = _fileExportService.CreateCypressFile(
            script.Content,
            script.FileName);

        return File(fileContent, contentType, fileName);
    }

    // POST: User/Cypress/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account", new { area = "" });
        }

        var script = await _cypressService.GetScriptByIdAsync(id);
        if (script == null)
        {
            return NotFound();
        }

        // Verify user owns the project
        if (script.UserStory?.ProjectId != null &&
            !await _projectService.UserOwnsProjectAsync(script.UserStory.ProjectId, userId))
        {
            return Forbid();
        }

        var userStoryId = script.UserStoryId;

        try
        {
            await _cypressService.DeleteScriptAsync(id);
            TempData["SuccessMessage"] = "Cypress script deleted successfully!";
            return RedirectToAction(nameof(Index), new { userStoryId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Cypress script {ScriptId}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the script. Please try again.";
            return RedirectToAction(nameof(Index), new { userStoryId });
        }
    }

    // POST: User/Cypress/DeleteAll?userStoryId=5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAll(int userStoryId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account", new { area = "" });
        }

        var userStory = await _userStoryService.GetUserStoryByIdAsync(userStoryId);
        if (userStory == null)
        {
            return NotFound();
        }

        // Verify user owns the project
        if (!await _projectService.UserOwnsProjectAsync(userStory.ProjectId, userId))
        {
            return Forbid();
        }

        try
        {
            var deletedCount = await _cypressService.DeleteAllByUserStoryIdAsync(userStoryId);
            TempData["SuccessMessage"] = $"Successfully deleted {deletedCount} Cypress script(s)!";
            return RedirectToAction(nameof(Index), new { userStoryId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting all Cypress scripts for user story {UserStoryId}", userStoryId);
            TempData["ErrorMessage"] = "An error occurred while deleting the scripts. Please try again.";
            return RedirectToAction(nameof(Index), new { userStoryId });
        }
    }
}
