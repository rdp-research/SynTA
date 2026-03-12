using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SynTA.Areas.User.Models;
using SynTA.Models.Domain;
using SynTA.Services.AI;
using SynTA.Services.Database;

namespace SynTA.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class GherkinController : Controller
    {
        private readonly IGherkinScenarioService _gherkinService;
        private readonly IUserStoryService _userStoryService;
        private readonly IProjectService _projectService;
        private readonly IAIServiceFactory _aiServiceFactory;
        private readonly ISettingsService _settingsService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<GherkinController> _logger;

        public GherkinController(
            IGherkinScenarioService gherkinService,
            IUserStoryService userStoryService,
            IProjectService projectService,
            IAIServiceFactory aiServiceFactory,
            ISettingsService settingsService,
            UserManager<ApplicationUser> userManager,
            ILogger<GherkinController> logger)
        {
            _gherkinService = gherkinService;
            _userStoryService = userStoryService;
            _projectService = projectService;
            _aiServiceFactory = aiServiceFactory;
            _settingsService = settingsService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: User/Gherkin/Index?userStoryId=5
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

            var scenarios = await _gherkinService.GetScenariosByUserStoryIdAsync(userStoryId);

            var viewModels = scenarios.Select(s => new GherkinEditorViewModel
            {
                Id = s.Id,
                UserStoryId = s.UserStoryId,
                UserStoryTitle = userStory.Title,
                ProjectName = userStory.Project?.Name ?? "",
                Title = s.Title,
                Content = s.Content,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).ToList();

            return View(viewModels);
        }

        // GET: User/Gherkin/Generate?userStoryId=5
        public async Task<IActionResult> Generate(int userStoryId)
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

            var viewModel = new GherkinGenerateViewModel
            {
                UserStoryId = userStory.Id,
                UserStoryTitle = userStory.Title,
                UserStoryText = userStory.UserStoryText,
                UserStoryDescription = userStory.Description,
                AcceptanceCriteria = userStory.AcceptanceCriteria,
                ProjectName = userStory.Project?.Name ?? "",
                IsGenerating = false
            };

            return View(viewModel);
        }

        // POST: User/Gherkin/Generate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(int userStoryId, bool confirm = false)
        {
            var operationId = Guid.NewGuid().ToString("N")[..8];
            
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("[{OperationId}] Gherkin generation aborted - User not authenticated", operationId);
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var userStory = await _userStoryService.GetUserStoryByIdAsync(userStoryId);
            if (userStory == null)
            {
                _logger.LogWarning("[{OperationId}] Gherkin generation aborted - User story not found: {UserStoryId}",
                    operationId, userStoryId);
                return NotFound();
            }

            // Verify user owns the project
            if (!await _projectService.UserOwnsProjectAsync(userStory.ProjectId, userId))
            {
                _logger.LogWarning("[{OperationId}] Gherkin generation forbidden - User {UserId} does not own project {ProjectId}",
                    operationId, userId, userStory.ProjectId);
                return Forbid();
            }

            try
            {
                // Get user settings for generation parameters
                var settings = await _settingsService.GetSettingsAsync(userId);

                // Get user's preferred AI provider and create the appropriate service with model tier
                var aiService = _aiServiceFactory.CreateService(settings.PreferredAIProvider, settings.PreferredModelTier);
                
                // Set custom model name for OpenRouter
                if (settings.PreferredAIProvider == AIProviderType.OpenRouter)
                {
                    aiService.CustomModelName = settings.OpenRouterModelName;
                    _logger.LogInformation("[{OperationId}] OpenRouter custom model from settings: '{ModelName}'",
                        operationId, settings.OpenRouterModelName ?? "(null)");
                }
                
                _logger.LogInformation(
                    "[{OperationId}] Starting Gherkin generation - Provider: {Provider}, ModelTier: {ModelTier}, UserStoryId: {UserStoryId}, UserStory: '{Title}', UserId: {UserId}",
                    operationId, settings.PreferredAIProvider, settings.PreferredModelTier, userStoryId, userStory.Title, userId);

                // Call AI service to generate Gherkin scenarios with user's preferred settings
                var gherkinContent = await aiService.GenerateGherkinScenariosAsync(
                    userStory.Title,
                    userStory.UserStoryText,
                    userStory.Description,
                    userStory.AcceptanceCriteria,
                    settings.MaxScenariosPerGeneration,
                    settings.PreferredLanguage);

                // Create a new Gherkin scenario
                var scenario = new GherkinScenario
                {
                    UserStoryId = userStoryId,
                    Title = $"Test Scenarios for: {userStory.Title}",
                    Content = gherkinContent
                };

                await _gherkinService.CreateScenarioAsync(scenario);

                _logger.LogInformation(
                    "[{OperationId}] Gherkin scenarios generated and saved successfully - ScenarioId: {ScenarioId}, UserStoryId: {UserStoryId}, ContentLength: {Length} chars, UserId: {UserId}",
                    operationId, scenario.Id, userStoryId, gherkinContent.Length, userId);

                TempData["SuccessMessage"] = "Gherkin scenarios generated successfully!";
                return RedirectToAction(nameof(Index), new { userStoryId = userStoryId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[{OperationId}] Gherkin generation failed - UserStoryId: {UserStoryId}, UserStory: '{Title}', UserId: {UserId}, Error: {ErrorMessage}",
                    operationId, userStoryId, userStory.Title, userId, ex.Message);
                TempData["ErrorMessage"] = "An error occurred while generating Gherkin scenarios. Please try again.";
                
                var viewModel = new GherkinGenerateViewModel
                {
                    UserStoryId = userStory.Id,
                    UserStoryTitle = userStory.Title,
                    UserStoryText = userStory.UserStoryText,
                    UserStoryDescription = userStory.Description,
                    AcceptanceCriteria = userStory.AcceptanceCriteria,
                    ProjectName = userStory.Project?.Name ?? "",
                    IsGenerating = false
                };
                
                return View(viewModel);
            }
        }

        // GET: User/Gherkin/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var scenario = await _gherkinService.GetScenarioByIdAsync(id);
            if (scenario == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // Verify user owns the project
            if (scenario.UserStory?.ProjectId != null &&
                !await _projectService.UserOwnsProjectAsync(scenario.UserStory.ProjectId, userId))
            {
                return Forbid();
            }

            var viewModel = new GherkinEditorViewModel
            {
                Id = scenario.Id,
                UserStoryId = scenario.UserStoryId,
                UserStoryTitle = scenario.UserStory?.Title ?? "",
                ProjectName = scenario.UserStory?.Project?.Name ?? "",
                Title = scenario.Title,
                Content = scenario.Content,
                CreatedAt = scenario.CreatedAt,
                UpdatedAt = scenario.UpdatedAt
            };

            return View(viewModel);
        }

        // POST: User/Gherkin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, GherkinEditorViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            try
            {
                var scenario = await _gherkinService.GetScenarioByIdAsync(id);
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

                scenario.Title = model.Title;
                scenario.Content = model.Content;

                await _gherkinService.UpdateScenarioAsync(scenario);

                TempData["SuccessMessage"] = "Gherkin scenario updated successfully!";
                return RedirectToAction(nameof(Index), new { userStoryId = scenario.UserStoryId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Gherkin scenario {ScenarioId}", id);
                ModelState.AddModelError("", "An error occurred while updating the scenario. Please try again.");
                return View(model);
            }
        }

        // POST: User/Gherkin/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var scenario = await _gherkinService.GetScenarioByIdAsync(id);
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

            var userStoryId = scenario.UserStoryId;

            try
            {
                await _gherkinService.DeleteScenarioAsync(id);
                TempData["SuccessMessage"] = "Gherkin scenario deleted successfully!";
                return RedirectToAction(nameof(Index), new { userStoryId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Gherkin scenario {ScenarioId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the scenario. Please try again.";
                return RedirectToAction(nameof(Index), new { userStoryId });
            }
        }

        // POST: User/Gherkin/DeleteAll?userStoryId=5
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
                var deletedCount = await _gherkinService.DeleteAllByUserStoryIdAsync(userStoryId);
                TempData["SuccessMessage"] = $"Successfully deleted {deletedCount} Gherkin scenario(s)!";
                return RedirectToAction(nameof(Index), new { userStoryId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all Gherkin scenarios for user story {UserStoryId}", userStoryId);
                TempData["ErrorMessage"] = "An error occurred while deleting the scenarios. Please try again.";
                return RedirectToAction(nameof(Index), new { userStoryId });
            }
        }
    }
}
