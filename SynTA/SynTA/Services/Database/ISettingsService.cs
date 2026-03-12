using SynTA.Models.Domain;
using SynTA.Services.AI;

namespace SynTA.Services.Database
{
    /// <summary>
    /// Interface for managing user settings.
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// Gets the settings for a specific user. Creates default settings if none exist.
        /// </summary>
        /// <param name="userId">The user's ID</param>
        /// <returns>The user's settings</returns>
        Task<UserSettings> GetSettingsAsync(string userId);

        /// <summary>
        /// Updates the settings for a specific user.
        /// </summary>
        /// <param name="settings">The updated settings</param>
        /// <returns>The updated settings</returns>
        Task<UserSettings> UpdateSettingsAsync(UserSettings settings);

        /// <summary>
        /// Gets the preferred AI provider for a specific user.
        /// </summary>
        /// <param name="userId">The user's ID</param>
        /// <returns>The preferred AI provider type</returns>
        Task<AIProviderType> GetPreferredAIProviderAsync(string userId);

        /// <summary>
        /// Updates the preferred AI provider for a specific user.
        /// </summary>
        /// <param name="userId">The user's ID</param>
        /// <param name="provider">The new preferred provider</param>
        /// <returns>Task</returns>
        Task UpdatePreferredAIProviderAsync(string userId, AIProviderType provider);

        /// <summary>
        /// Resets user settings to default values.
        /// </summary>
        /// <param name="userId">The user's ID</param>
        /// <returns>The reset settings</returns>
        Task<UserSettings> ResetToDefaultsAsync(string userId);

        /// <summary>
        /// Checks if settings exist for a user.
        /// </summary>
        /// <param name="userId">The user's ID</param>
        /// <returns>True if settings exist</returns>
        Task<bool> SettingsExistAsync(string userId);
    }
}
