using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SynTA.Models.Domain;
using SynTA.Services.AI;
using SynTA.Services.AI.Prompts;
using Xunit;

namespace SynTA.Tests.Services
{
    public class AIServiceTests
    {
        [Fact]
        public void AIServiceFactory_CurrentProvider_ParsesGeminiFromConfig()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "AI:Provider", "Gemini" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var services = new ServiceCollection();
            var provider = services.BuildServiceProvider();
            var logger = new Mock<ILogger<AIServiceFactory>>().Object;

            var factory = new AIServiceFactory(provider, configuration, logger);

            // Act
            var current = factory.CurrentProvider;

            // Assert
            Assert.Equal(AIProviderType.Gemini, current);
        }

        [Fact]
        public void AIServiceFactory_GetAvailableProviders_ReturnsConfiguredProviders()
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
            var provider = services.BuildServiceProvider();
            var logger = new Mock<ILogger<AIServiceFactory>>().Object;

            var factory = new AIServiceFactory(provider, configuration, logger);

            // Act
            var available = factory.GetAvailableProviders().ToList();

            // Assert
            Assert.Contains(AIProviderType.OpenAI, available);
            Assert.Contains(AIProviderType.Gemini, available);
        }

        [Fact]
        public void AIServiceFactory_CurrentProvider_ParsesOpenRouterFromConfig()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "AI:Provider", "OpenRouter" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var services = new ServiceCollection();
            var provider = services.BuildServiceProvider();
            var logger = new Mock<ILogger<AIServiceFactory>>().Object;

            var factory = new AIServiceFactory(provider, configuration, logger);

            // Act
            var current = factory.CurrentProvider;

            // Assert
            Assert.Equal(AIProviderType.OpenRouter, current);
        }

        [Fact]
        public void AIServiceFactory_GetAvailableProviders_IncludesOpenRouterWhenConfigured()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "OpenAI:ApiKey", "openai-key" },
                { "Gemini:ApiKey", "gemini-key" },
                { "OpenRouter:ApiKey", "openrouter-key" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var services = new ServiceCollection();
            var provider = services.BuildServiceProvider();
            var logger = new Mock<ILogger<AIServiceFactory>>().Object;

            var factory = new AIServiceFactory(provider, configuration, logger);

            // Act
            var available = factory.GetAvailableProviders().ToList();

            // Assert
            Assert.Contains(AIProviderType.OpenRouter, available);
        }

        [Fact]
        public void PromptService_BuildCypressPrompt_IncludesLanguageAndExtension()
        {
            // Arrange
            var promptService = new PromptService();
            var gherkin = "Scenario: Login...\n  Given ...";
            var targetUrl = "https://example.com";
            var title = "Login Story";
            var userStoryText = "As a user, I want to login";

            // Act
            var prompt = promptService.BuildCypressPrompt(gherkin, targetUrl, title, userStoryText, scriptLanguage: CypressScriptLanguage.JavaScript);

            // Assert
            Assert.Contains("JavaScript", prompt);
            Assert.Contains(".cy.js", prompt);
            Assert.Contains(title, prompt);
            Assert.Contains(userStoryText, prompt);
        }

        [Fact]
        public void PromptService_BuildGherkinPrompt_IncludesTitleAndUserStory()
        {
            // Arrange
            var promptService = new PromptService();
            var title = "Payment Story";
            var userStory = "As a user, I want to pay";

            // Act
            var prompt = promptService.BuildGherkinPrompt(title, userStory, description: "Optional", acceptanceCriteria: "AC1, AC2");

            // Assert
            Assert.Contains("User Story:", prompt);
            Assert.Contains($"Title: {title}", prompt);
            Assert.Contains($"User Story: {userStory}", prompt);
        }
    }
}
