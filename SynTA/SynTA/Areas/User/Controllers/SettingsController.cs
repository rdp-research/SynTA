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
    public class SettingsController : Controller
    {
        private readonly ISettingsService _settingsService;
        private readonly IAIServiceFactory _aiServiceFactory;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(
            ISettingsService settingsService,
            IAIServiceFactory aiServiceFactory,
            UserManager<ApplicationUser> userManager,
            ILogger<SettingsController> logger)
        {
            _settingsService = settingsService;
            _aiServiceFactory = aiServiceFactory;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: User/Settings
        public async Task<IActionResult> Index(string? tab = null)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            try
            {
                var settings = await _settingsService.GetSettingsAsync(userId);
                var viewModel = MapToViewModel(settings);
                viewModel.AvailableProviders = _aiServiceFactory.GetAvailableProviders();
                viewModel.ActiveTab = tab ?? "ai";

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving settings for user {UserId}", userId);
                TempData["ErrorMessage"] = "Failed to load settings. Please try again.";
                return RedirectToAction("Index", "Project");
            }
        }

        // POST: User/Settings/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(SettingsViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            if (!ModelState.IsValid)
            {
                model.AvailableProviders = _aiServiceFactory.GetAvailableProviders();
                return View("Index", model);
            }

            if (model.PreferredAIProvider == AIProviderType.OpenRouter && string.IsNullOrWhiteSpace(model.OpenRouterModelName))
            {
                ModelState.AddModelError(nameof(SettingsViewModel.OpenRouterModelName), "OpenRouter model is required when OpenRouter provider is selected.");
                model.AvailableProviders = _aiServiceFactory.GetAvailableProviders();
                return View("Index", model);
            }

            try
            {
                var settings = await _settingsService.GetSettingsAsync(userId);
                
                // Update settings from view model
                settings.PreferredAIProvider = model.PreferredAIProvider;
                settings.PreferredModelTier = model.PreferredAIProvider == AIProviderType.OpenRouter
                    ? settings.PreferredModelTier
                    : model.PreferredModelTier;
                settings.OpenRouterModelName = string.IsNullOrWhiteSpace(model.OpenRouterModelName)
                    ? settings.OpenRouterModelName
                    : model.OpenRouterModelName.Trim();
                settings.DefaultCypressFileNamePattern = model.DefaultCypressFileNamePattern;
                settings.PreferredLanguage = model.PreferredLanguage;
                settings.MaxScenariosPerGeneration = model.MaxScenariosPerGeneration;
                settings.PreferredCypressLanguage = model.PreferredCypressLanguage;
                settings.VisionApiEnabled = model.VisionApiEnabled;
                settings.WebExtractionEnabledForCypressGeneration = model.WebExtractionEnabledForCypressGeneration;

                if (Request.Form.ContainsKey(nameof(SettingsViewModel.IncludeWebPageMetadataInExtraction)))
                {
                    settings.IncludeWebPageMetadataInExtraction = model.IncludeWebPageMetadataInExtraction;
                }

                if (Request.Form.ContainsKey(nameof(SettingsViewModel.IncludeUiElementMapInExtraction)))
                {
                    settings.IncludeUiElementMapInExtraction = model.IncludeUiElementMapInExtraction;
                }

                if (Request.Form.ContainsKey(nameof(SettingsViewModel.IncludeAccessibilityTreeInExtraction)))
                {
                    settings.IncludeAccessibilityTreeInExtraction = model.IncludeAccessibilityTreeInExtraction;
                }

                if (Request.Form.ContainsKey(nameof(SettingsViewModel.IncludeSimplifiedHtmlInExtraction)))
                {
                    settings.IncludeSimplifiedHtmlInExtraction = model.IncludeSimplifiedHtmlInExtraction;
                }

                await _settingsService.UpdateSettingsAsync(settings);

                _logger.LogInformation("Settings updated for user {UserId}", userId);
                TempData["SuccessMessage"] = "Settings saved successfully!";
                
                return RedirectToAction(nameof(Index), new { tab = model.ActiveTab });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating settings for user {UserId}", userId);
                model.AvailableProviders = _aiServiceFactory.GetAvailableProviders();
                model.ErrorMessage = "Failed to save settings. Please try again.";
                return View("Index", model);
            }
        }

        // POST: User/Settings/UpdateAIProvider
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAIProvider(AIProviderType provider)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            try
            {
                await _settingsService.UpdatePreferredAIProviderAsync(userId, provider);
                _logger.LogInformation("AI provider updated to {Provider} for user {UserId}", provider, userId);
                return Json(new { success = true, message = $"AI provider changed to {provider}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating AI provider for user {UserId}", userId);
                return Json(new { success = false, message = "Failed to update AI provider" });
            }
        }

        // POST: User/Settings/Reset
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reset()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            try
            {
                await _settingsService.ResetToDefaultsAsync(userId);
                _logger.LogInformation("Settings reset to defaults for user {UserId}", userId);
                TempData["SuccessMessage"] = "Settings have been reset to defaults.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting settings for user {UserId}", userId);
                TempData["ErrorMessage"] = "Failed to reset settings. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        private static SettingsViewModel MapToViewModel(UserSettings settings)
        {
            return new SettingsViewModel
            {
                Id = settings.Id,
                PreferredAIProvider = settings.PreferredAIProvider,
                PreferredModelTier = settings.PreferredModelTier,
                OpenRouterModelName = settings.OpenRouterModelName,
                DefaultCypressFileNamePattern = settings.DefaultCypressFileNamePattern,
                PreferredLanguage = settings.PreferredLanguage,
                MaxScenariosPerGeneration = settings.MaxScenariosPerGeneration,
                PreferredCypressLanguage = settings.PreferredCypressLanguage,
                VisionApiEnabled = settings.VisionApiEnabled,
                WebExtractionEnabledForCypressGeneration = settings.WebExtractionEnabledForCypressGeneration,
                IncludeWebPageMetadataInExtraction = settings.IncludeWebPageMetadataInExtraction,
                IncludeUiElementMapInExtraction = settings.IncludeUiElementMapInExtraction,
                IncludeAccessibilityTreeInExtraction = settings.IncludeAccessibilityTreeInExtraction,
                IncludeSimplifiedHtmlInExtraction = settings.IncludeSimplifiedHtmlInExtraction
            };
        }
    }
}
