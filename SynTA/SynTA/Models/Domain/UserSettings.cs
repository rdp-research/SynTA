using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynTA.Services.AI;

namespace SynTA.Models.Domain
{
    /// <summary>
    /// Theme preference options for the user interface.
    /// </summary>
    public enum ThemePreference
    {
        /// <summary>
        /// Use the system's default theme setting
        /// </summary>
        System,

        /// <summary>
        /// Light theme
        /// </summary>
        Light,

        /// <summary>
        /// Dark theme
        /// </summary>
        Dark
    }

    /// <summary>
    /// AI model tier preference options balancing speed, cost, and intelligence.
    /// </summary>
    public enum AIModelTier
    {
        /// <summary>
        /// Ultra Fast: Fastest, cheapest, but least intelligent models
        /// </summary>
        UltraFast,

        /// <summary>
        /// Fast: Good balance of speed, cost, and intelligence
        /// </summary>
        Fast,

        /// <summary>
        /// Smart: Most intelligent, but slower and more expensive
        /// </summary>
        Smart
    }

    /// <summary>
    /// Cypress script language preference options.
    /// </summary>
    public enum CypressScriptLanguage
    {
        /// <summary>
        /// TypeScript - Modern, type-safe Cypress scripts (.cy.ts)
        /// </summary>
        TypeScript,

        /// <summary>
        /// JavaScript - Classic Cypress scripts (.cy.js)
        /// </summary>
        JavaScript
    }

    /// <summary>
    /// User settings and preferences for the SynTA application.
    /// Each user has one settings record that stores their preferred configurations.
    /// </summary>
    public class UserSettings
    {
        /// <summary>
        /// Primary key for the user settings.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the ApplicationUser.
        /// </summary>
        [Required]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property to the user who owns these settings.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }

        /// <summary>
        /// The user's preferred AI provider for generating Gherkin and Cypress scripts.
        /// </summary>
        public AIProviderType PreferredAIProvider { get; set; } = AIProviderType.OpenAI;

        /// <summary>
        /// The user's preferred AI model tier (affects speed, cost, and intelligence).
        /// </summary>
        public AIModelTier PreferredModelTier { get; set; } = AIModelTier.Fast;

        /// <summary>
        /// Optional custom OpenRouter model identifier (e.g. "openai/gpt-5-mini").
        /// Used only when PreferredAIProvider is OpenRouter.
        /// </summary>
        [MaxLength(200)]
        public string? OpenRouterModelName { get; set; }

        /// <summary>
        /// The user's preferred UI theme.
        /// </summary>
        public ThemePreference ThemePreference { get; set; } = ThemePreference.System;

        /// <summary>
        /// Whether to show detailed AI generation progress.
        /// </summary>
        public bool ShowGenerationProgress { get; set; } = true;

        /// <summary>
        /// Default file name pattern for exported Cypress scripts.
        /// Supports placeholders: {UserStory}, {Date}, {Project}
        /// Note: The file extension (.cy.ts or .cy.js) is automatically applied based on PreferredCypressLanguage.
        /// </summary>
        [MaxLength(200)]
        public string DefaultCypressFileNamePattern { get; set; } = "{UserStory}";

        /// <summary>
        /// The user's preferred language for AI-generated content.
        /// </summary>
        [MaxLength(10)]
        public string PreferredLanguage { get; set; } = "en";

        /// <summary>
        /// Maximum number of scenarios to generate per user story.
        /// Set to 0 to let the AI automatically determine the optimal number.
        /// </summary>
        [Range(0, 50)]
        public int MaxScenariosPerGeneration { get; set; } = 10;

        /// <summary>
        /// The user's preferred language for Cypress scripts (TypeScript or JavaScript).
        /// </summary>
        public CypressScriptLanguage PreferredCypressLanguage { get; set; } = CypressScriptLanguage.TypeScript;

        /// <summary>
        /// Whether to enable the Vision API (screenshots) for AI processing.
        /// When enabled, screenshots of the target URL will be sent to the AI for multimodal processing.
        /// </summary>
        public bool VisionApiEnabled { get; set; } = true;

        /// <summary>
        /// Whether to fetch website extraction context (HTML-derived inputs) during Cypress generation.
        /// </summary>
        public bool WebExtractionEnabledForCypressGeneration { get; set; } = true;

        /// <summary>
        /// Whether to include website page metadata (title, H1, description, language) in extracted context.
        /// </summary>
        public bool IncludeWebPageMetadataInExtraction { get; set; } = true;

        /// <summary>
        /// Whether to include the UI element mapping section in extracted context.
        /// </summary>
        public bool IncludeUiElementMapInExtraction { get; set; } = true;

        /// <summary>
        /// Whether to include accessibility tree data in extracted context.
        /// </summary>
        public bool IncludeAccessibilityTreeInExtraction { get; set; } = true;

        /// <summary>
        /// Whether to include simplified HTML content in extracted context.
        /// </summary>
        public bool IncludeSimplifiedHtmlInExtraction { get; set; } = true;

        /// <summary>
        /// Timestamp when these settings were created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when these settings were last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
