using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SynTA.Services.AI;
using SynTA.Services.AI.Prompts;
using Xunit;

namespace SynTA.Tests.Services
{
    public class AIServiceFactoryIntegrationTests
    {
        [Fact]
        public void CreateService_ReturnsGeminiService_WhenGeminiConfiguredAndRegistered()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "OpenAI:ApiKey", "openai-key" },
                { "Gemini:ApiKey", "gemini-key" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var services = new ServiceCollection();

            // Required dependencies for the AI services
            services.AddSingleton<IConfiguration>(configuration);
            services.AddScoped<IPromptService, PromptService>();
            services.AddLogging();

            // Register keyed services exactly as in Program.cs
            services.AddKeyedScoped<IAIGenerationService, OpenAIService>("OpenAI");
            services.AddKeyedScoped<IAIGenerationService, GeminiService>("Gemini");

            var provider = services.BuildServiceProvider();

            var logger = new Mock<ILogger<AIServiceFactory>>().Object;
            var factory = new AIServiceFactory(provider, configuration, logger);

            // Act
            var service = factory.CreateService(AIProviderType.Gemini);

            // Assert
            Assert.NotNull(service);
            Assert.Equal("Gemini", service.ProviderName);
        }
    }
}
