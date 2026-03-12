using System.ComponentModel.DataAnnotations;
using SynTA.Models.Domain;
using SynTA.Services.AI;

namespace SynTA.Areas.User.Models
{
    /// <summary>
    /// ViewModel for displaying and editing user settings.
    /// </summary>
    public class SettingsViewModel
    {
        /// <summary>
        /// The settings ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The user's preferred AI provider.
        /// </summary>
        [Display(Name = "Preferred AI Provider")]
        public AIProviderType PreferredAIProvider { get; set; } = AIProviderType.OpenAI;

        /// <summary>
        /// The user's preferred AI model tier.
        /// </summary>
        [Display(Name = "Model Performance Tier")]
        public AIModelTier PreferredModelTier { get; set; } = AIModelTier.Fast;

        /// <summary>
        /// Custom OpenRouter model identifier. Used only when provider is OpenRouter.
        /// </summary>
        [Display(Name = "OpenRouter Model")]
        [StringLength(200, ErrorMessage = "OpenRouter model cannot exceed 200 characters.")]
        public string? OpenRouterModelName { get; set; }

        /// <summary>
        /// Default file name pattern for exported Cypress scripts.
        /// Note: The file extension is automatically applied based on PreferredCypressLanguage.
        /// </summary>
        [Display(Name = "Cypress File Name Pattern")]
        [StringLength(200, ErrorMessage = "File name pattern cannot exceed 200 characters.")]
        public string DefaultCypressFileNamePattern { get; set; } = "{UserStory}";

        /// <summary>
        /// The user's preferred language for AI-generated content.
        /// </summary>
        [Display(Name = "Preferred Language")]
        [StringLength(10, ErrorMessage = "Language code cannot exceed 10 characters.")]
        public string PreferredLanguage { get; set; } = "en";

        /// <summary>
        /// Maximum number of scenarios to generate per user story.
        /// Set to 0 to let the AI automatically determine the optimal number.
        /// </summary>
        [Display(Name = "Max Scenarios Per Generation")]
        [Range(0, 50, ErrorMessage = "Must be between 0 and 50 scenarios. Use 0 for automatic.")]
        public int MaxScenariosPerGeneration { get; set; } = 10;

        /// <summary>
        /// The user's preferred language for Cypress scripts (TypeScript or JavaScript).
        /// </summary>
        [Display(Name = "Cypress Script Language")]
        public CypressScriptLanguage PreferredCypressLanguage { get; set; } = CypressScriptLanguage.TypeScript;

        /// <summary>
        /// Whether to enable the Vision API (screenshots) for AI processing.
        /// When enabled, screenshots of the target URL will be sent to the AI for multimodal processing.
        /// </summary>
        [Display(Name = "Enable Vision API")]
        public bool VisionApiEnabled { get; set; } = true;

        /// <summary>
        /// Whether to fetch website extraction inputs for Cypress generation.
        /// </summary>
        [Display(Name = "Enable Web Extraction for Cypress")]
        public bool WebExtractionEnabledForCypressGeneration { get; set; } = true;

        /// <summary>
        /// Whether to include website metadata in extraction context.
        /// </summary>
        [Display(Name = "Include Website Metadata")]
        public bool IncludeWebPageMetadataInExtraction { get; set; } = true;

        /// <summary>
        /// Whether to include UI element mapping in extraction context.
        /// </summary>
        [Display(Name = "Include UI Element Map")]
        public bool IncludeUiElementMapInExtraction { get; set; } = true;

        /// <summary>
        /// Whether to include accessibility tree in extraction context.
        /// </summary>
        [Display(Name = "Include Accessibility Tree")]
        public bool IncludeAccessibilityTreeInExtraction { get; set; } = true;

        /// <summary>
        /// Whether to include simplified HTML in extraction context.
        /// </summary>
        [Display(Name = "Include Simplified HTML")]
        public bool IncludeSimplifiedHtmlInExtraction { get; set; } = true;

        /// <summary>
        /// List of available AI providers for display in dropdown.
        /// </summary>
        public IEnumerable<AIProviderType> AvailableProviders { get; set; } = new List<AIProviderType>();

        /// <summary>
        /// Success message to display after saving settings.
        /// </summary>
        public string? SuccessMessage { get; set; }

        /// <summary>
        /// Error message to display if saving fails.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// The active tab to display (for tabbed UI).
        /// </summary>
        public string ActiveTab { get; set; } = "ai";
    }
}
