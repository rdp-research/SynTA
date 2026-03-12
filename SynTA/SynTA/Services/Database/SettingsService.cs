using Microsoft.EntityFrameworkCore;
using SynTA.Data;
using SynTA.Models.Domain;
using SynTA.Services.AI;

namespace SynTA.Services.Database
{
    /// <summary>
    /// Service for managing user settings and preferences.
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SettingsService> _logger;

        public SettingsService(ApplicationDbContext context, ILogger<SettingsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<UserSettings> GetSettingsAsync(string userId)
        {
            try
            {
                var settings = await _context.UserSettings
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (settings == null)
                {
                    _logger.LogInformation("Creating default settings for user {UserId}", userId);
                    settings = await CreateDefaultSettingsAsync(userId);
                }

                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving settings for user {UserId}", userId);
                throw;
            }
        }

        public async Task<UserSettings> UpdateSettingsAsync(UserSettings settings)
        {
            try
            {
                var existingSettings = await _context.UserSettings
                    .FirstOrDefaultAsync(s => s.UserId == settings.UserId);

                if (existingSettings == null)
                {
                    _logger.LogInformation("Settings not found for user {UserId}, creating new settings", settings.UserId);
                    settings.CreatedAt = DateTime.UtcNow;
                    settings.UpdatedAt = DateTime.UtcNow;
                    _context.UserSettings.Add(settings);
                }
                else
                {
                    // Update existing settings
                    existingSettings.PreferredAIProvider = settings.PreferredAIProvider;
                    existingSettings.PreferredModelTier = settings.PreferredModelTier;
                    existingSettings.OpenRouterModelName = settings.OpenRouterModelName;
                    existingSettings.ThemePreference = settings.ThemePreference;
                    existingSettings.ShowGenerationProgress = settings.ShowGenerationProgress;
                    existingSettings.DefaultCypressFileNamePattern = settings.DefaultCypressFileNamePattern;
                    existingSettings.PreferredLanguage = settings.PreferredLanguage;
                    existingSettings.MaxScenariosPerGeneration = settings.MaxScenariosPerGeneration;
                    existingSettings.PreferredCypressLanguage = settings.PreferredCypressLanguage;
                    existingSettings.VisionApiEnabled = settings.VisionApiEnabled;
                    existingSettings.WebExtractionEnabledForCypressGeneration = settings.WebExtractionEnabledForCypressGeneration;
                    existingSettings.IncludeWebPageMetadataInExtraction = settings.IncludeWebPageMetadataInExtraction;
                    existingSettings.IncludeUiElementMapInExtraction = settings.IncludeUiElementMapInExtraction;
                    existingSettings.IncludeAccessibilityTreeInExtraction = settings.IncludeAccessibilityTreeInExtraction;
                    existingSettings.IncludeSimplifiedHtmlInExtraction = settings.IncludeSimplifiedHtmlInExtraction;
                    existingSettings.UpdatedAt = DateTime.UtcNow;;

                    _context.UserSettings.Update(existingSettings);
                    settings = existingSettings;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated settings for user {UserId}", settings.UserId);
                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating settings for user {UserId}", settings.UserId);
                throw;
            }
        }

        public async Task<AIProviderType> GetPreferredAIProviderAsync(string userId)
        {
            try
            {
                var settings = await GetSettingsAsync(userId);
                return settings.PreferredAIProvider;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting preferred AI provider for user {UserId}", userId);
                // Return default if there's an error
                return AIProviderType.OpenAI;
            }
        }

        public async Task UpdatePreferredAIProviderAsync(string userId, AIProviderType provider)
        {
            try
            {
                var settings = await GetSettingsAsync(userId);
                settings.PreferredAIProvider = provider;
                settings.UpdatedAt = DateTime.UtcNow;
                
                _context.UserSettings.Update(settings);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Updated preferred AI provider to {Provider} for user {UserId}", provider, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating preferred AI provider for user {UserId}", userId);
                throw;
            }
        }

        public async Task<UserSettings> ResetToDefaultsAsync(string userId)
        {
            try
            {
                var existingSettings = await _context.UserSettings
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (existingSettings != null)
                {
                    _context.UserSettings.Remove(existingSettings);
                    await _context.SaveChangesAsync();
                }

                var defaultSettings = await CreateDefaultSettingsAsync(userId);
                _logger.LogInformation("Reset settings to defaults for user {UserId}", userId);
                return defaultSettings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting settings for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> SettingsExistAsync(string userId)
        {
            try
            {
                return await _context.UserSettings.AnyAsync(s => s.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking settings existence for user {UserId}", userId);
                throw;
            }
        }

        private async Task<UserSettings> CreateDefaultSettingsAsync(string userId)
        {
            var settings = new UserSettings
            {
                UserId = userId,
                PreferredAIProvider = AIProviderType.OpenAI,
                PreferredModelTier = AIModelTier.Fast,
                OpenRouterModelName = null,
                ThemePreference = ThemePreference.System,
                ShowGenerationProgress = true,
                DefaultCypressFileNamePattern = "{UserStory}",
                PreferredLanguage = "en",
                MaxScenariosPerGeneration = 10,
                PreferredCypressLanguage = CypressScriptLanguage.TypeScript,
                VisionApiEnabled = true,
                WebExtractionEnabledForCypressGeneration = true,
                IncludeWebPageMetadataInExtraction = true,
                IncludeUiElementMapInExtraction = true,
                IncludeAccessibilityTreeInExtraction = true,
                IncludeSimplifiedHtmlInExtraction = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserSettings.Add(settings);
            await _context.SaveChangesAsync();

            return settings;
        }
    }
}
