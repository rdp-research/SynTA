using SynTA.Constants;
using SynTA.Models.Domain;

namespace SynTA.Services.AI
{
    /// <summary>
    /// Supported AI provider types for the application.
    /// </summary>
    public enum AIProviderType
    {
        /// <summary>
        /// OpenAI (GPT models)
        /// </summary>
        OpenAI,

        /// <summary>
        /// Google Gemini
        /// </summary>
        Gemini,

        /// <summary>
        /// OpenRouter (OpenAI-compatible API gateway)
        /// </summary>
        OpenRouter
    }

    /// <summary>
    /// Factory for creating AI generation services based on configuration.
    /// This factory supports the Strategy pattern, allowing the application to
    /// switch between different AI providers at runtime based on configuration.
    /// </summary>
    public interface IAIServiceFactory
    {
        /// <summary>
        /// Gets the current AI provider type from configuration.
        /// </summary>
        AIProviderType CurrentProvider { get; }

        /// <summary>
        /// Creates an AI generation service based on the current configuration.
        /// </summary>
        /// <param name="modelTier">The model tier to use (defaults to Fast)</param>
        /// <returns>An implementation of IAIGenerationService</returns>
        IAIGenerationService CreateService(AIModelTier modelTier = AIModelTier.Fast);

        /// <summary>
        /// Creates an AI generation service for a specific provider.
        /// </summary>
        /// <param name="provider">The AI provider to use</param>
        /// <param name="modelTier">The model tier to use (defaults to Fast)</param>
        /// <returns>An implementation of IAIGenerationService</returns>
        IAIGenerationService CreateService(AIProviderType provider, AIModelTier modelTier = AIModelTier.Fast);

        /// <summary>
        /// Gets a list of all available AI providers.
        /// </summary>
        /// <returns>List of available provider types</returns>
        IEnumerable<AIProviderType> GetAvailableProviders();
    }

    /// <summary>
    /// Default implementation of the AI service factory.
    /// Uses keyed services to resolve the appropriate AI provider.
    /// </summary>
    public class AIServiceFactory : IAIServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIServiceFactory> _logger;

        public AIServiceFactory(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<AIServiceFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        public AIProviderType CurrentProvider
        {
            get
            {
                var providerName = _configuration["AI:Provider"] ?? AIProviders.OpenAI;
                return ParseProvider(providerName);
            }
        }

        public IAIGenerationService CreateService(AIModelTier modelTier = AIModelTier.Fast)
        {
            return CreateService(CurrentProvider, modelTier);
        }

        public IAIGenerationService CreateService(AIProviderType provider, AIModelTier modelTier = AIModelTier.Fast)
        {
            _logger.LogDebug("Creating AI service - Provider: {Provider}, ModelTier: {ModelTier}", provider, modelTier);

            try
            {
                var service = provider switch
                {
                    AIProviderType.OpenAI => _serviceProvider.GetRequiredKeyedService<IAIGenerationService>(AIProviders.OpenAI),
                    AIProviderType.Gemini => _serviceProvider.GetRequiredKeyedService<IAIGenerationService>(AIProviders.Gemini),
                    AIProviderType.OpenRouter => _serviceProvider.GetRequiredKeyedService<IAIGenerationService>(AIProviders.OpenRouter),
                    _ => throw new ArgumentException($"Unsupported AI provider: {provider}")
                };

                // Set the model tier on the service
                service.ModelTier = modelTier;

                _logger.LogInformation("AI service instantiated - Provider: {ProviderName}, ModelTier: {ModelTier}", service.ProviderName, modelTier);
                return service;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create AI service - Provider: {Provider}, ModelTier: {ModelTier}, Error: {ErrorMessage}",
                    provider, modelTier, ex.Message);
                throw;
            }
        }

        public IEnumerable<AIProviderType> GetAvailableProviders()
        {
            var providers = new List<AIProviderType>();

            // Check if OpenAI is configured
            if (!string.IsNullOrEmpty(_configuration[$"{AIProviders.OpenAI}:ApiKey"]))
            {
                providers.Add(AIProviderType.OpenAI);
            }

            // Check if Gemini is configured
            if (!string.IsNullOrEmpty(_configuration[$"{AIProviders.Gemini}:ApiKey"]))
            {
                providers.Add(AIProviderType.Gemini);
            }

            // Check if OpenRouter is configured
            if (!string.IsNullOrEmpty(_configuration[$"{AIProviders.OpenRouter}:ApiKey"]))
            {
                providers.Add(AIProviderType.OpenRouter);
            }

            _logger.LogDebug("Available AI providers: {Providers}", string.Join(", ", providers));
            return providers;
        }

        private static AIProviderType ParseProvider(string providerName)
        {
            return providerName.ToLowerInvariant() switch
            {
                var p when p.Equals(AIProviders.OpenAI, StringComparison.OrdinalIgnoreCase) => AIProviderType.OpenAI,
                var p when p.Equals(AIProviders.Gemini, StringComparison.OrdinalIgnoreCase) => AIProviderType.Gemini,
                var p when p.Equals(AIProviders.OpenRouter, StringComparison.OrdinalIgnoreCase) => AIProviderType.OpenRouter,
                "google" => AIProviderType.Gemini,
                "chatgpt" => AIProviderType.OpenAI,
                _ => AIProviderType.OpenAI // Default to OpenAI
            };
        }
    }
}
