using System.ClientModel;
using OpenAI;
using OpenAI.Chat;
using SynTA.Models.Domain;
using SynTA.Services.AI.Prompts;
using SynTA.Services.AI.TextProcessing;

namespace SynTA.Services.AI;

/// <summary>
/// OpenAI implementation of the AI generation service.
/// Uses OpenAI's GPT models for generating Gherkin scenarios and Cypress scripts.
/// </summary>
public class OpenAIService : BaseAIService
{
    private readonly OpenAIClient _openAIClient;

    // OpenAI image size limit (20MB as per documentation)
    private const int MaxImageSizeBytes = 20 * 1024 * 1024;

    public override string ProviderName => "OpenAI";

    /// <summary>
    /// Gets the model name based on the current model tier.
    /// </summary>
    protected override string CurrentModelName => ModelTier switch
    {
        AIModelTier.UltraFast => "gpt-5-nano",
        AIModelTier.Fast => "gpt-5-mini",
        AIModelTier.Smart => "gpt-5.2",
        _ => "gpt-5-mini"
    };

    public OpenAIService(
        IConfiguration configuration,
        ILogger<OpenAIService> logger,
        IPromptService promptService)
        : base(logger, promptService, configuration.GetValue<float>("OpenAI:Temperature", 0.3f))
    {
        var apiKey = configuration["OpenAI:ApiKey"] ??
            throw new InvalidOperationException("OpenAI API key not configured");

        // Create OpenAI client options with extended timeout for large context windows
        var clientOptions = new OpenAIClientOptions
        {
            NetworkTimeout = TimeSpan.FromMinutes(5)
        };

        _openAIClient = new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions);
    }


    protected override async Task<string> GenerateGherkinContentAsync(string prompt, string language)
    {
        return await GenerateCompletionAsync(prompt);
    }

    protected override async Task<string> GenerateCypressContentAsync(string prompt, byte[]? screenshot)
    {
        return await GenerateCompletionAsync(prompt, screenshot);
    }

    public override async Task<bool> TestConnectionAsync()
    {
        Logger.LogInformation("Testing OpenAI API connection - Model: {Model}", CurrentModelName);
        try
        {
            var chatClient = _openAIClient.GetChatClient(CurrentModelName);
            var messages = new List<ChatMessage>
            {
                new UserChatMessage("Hello, respond with 'OK' if you can read this.")
            };

            var chatOptions = new ChatCompletionOptions
            {
                Temperature = 0f,
                MaxOutputTokenCount = 10
            };

            var completion = await chatClient.CompleteChatAsync(messages, chatOptions);
            var success = !string.IsNullOrEmpty(completion.Value.Content[0].Text);

            if (success)
            {
                Logger.LogInformation("OpenAI API connection test successful - Model: {Model}", CurrentModelName);
            }
            else
            {
                Logger.LogWarning("OpenAI API connection test returned empty response - Model: {Model}", CurrentModelName);
            }

            return success;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "OpenAI API connection test failed - Model: {Model}, Error: {ErrorMessage}", CurrentModelName, ex.Message);
            return false;
        }
    }

    private async Task<string> GenerateCompletionAsync(string userPrompt, byte[]? screenshot = null)
    {
        var chatClient = _openAIClient.GetChatClient(CurrentModelName);
        Logger.LogInformation("Using OpenAI model: {ModelName} (Tier: {Tier}, Temperature: {Temperature}, Multimodal: {IsMultimodal})", CurrentModelName, ModelTier, Temperature, screenshot != null);

        // Build message content parts
        var contentParts = new List<ChatMessageContentPart>
        {
            ChatMessageContentPart.CreateTextPart(userPrompt)
        };

        // Add image if screenshot is provided
        if (screenshot != null)
        {
            contentParts.Add(ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(screenshot), "image/jpeg"));
            Logger.LogDebug("Sending multimodal request with screenshot - Size: {Size} bytes", screenshot.Length);
        }

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                "You are a professional, conservative test automation engineer and Cypress code generator. " +
                "Convert Gherkin scenarios into production-ready Cypress test files. " +
                "Strictly follow the instructions included in the prompt. " +
                "Follow professional best practices: prioritize test stability, be conservative in assertions, and avoid inventing selectors or assumptions not present in the prompt."),
            new UserChatMessage(contentParts)
        };

        var chatOptions = new ChatCompletionOptions
        {
            Temperature = Temperature,
            MaxOutputTokenCount = 65536
        };

        ClientResult<ChatCompletion> completionResult;
        try
        {
            completionResult = await chatClient.CompleteChatAsync(messages, chatOptions);
        }
        catch (ClientResultException ex) when (ex.Message.Contains("temperature") && ex.Message.Contains("unsupported"))
        {
            Logger.LogWarning("Model {ModelName} does not support custom temperature value {Temperature}. Retrying with default temperature.", CurrentModelName, Temperature);
            
            // Retry without temperature (uses default)
            chatOptions = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 65536
                // Temperature omitted - will use model default (1)
            };
            
            Logger.LogInformation("Retrying with default temperature - Model: {ModelName}", CurrentModelName);
            completionResult = await chatClient.CompleteChatAsync(messages, chatOptions);
        }

        var completion = completionResult.Value;

        // Validate that the response contains content
        if (completion?.Content == null || completion.Content.Count == 0)
        {
            Logger.LogError("OpenAI returned an empty response - no content in completion. FinishReason: {FinishReason}",
                completion?.FinishReason);
            throw new InvalidOperationException("OpenAI returned an empty response");
        }

        // Log detailed information about the response for debugging
        Logger.LogDebug("OpenAI response - FinishReason: {FinishReason}, ContentCount: {Count}",
            completion.FinishReason, completion.Content.Count);

        // Try to extract text from all content parts
        var textParts = new List<string>();
        foreach (var contentPart in completion.Content)
        {
            if (!string.IsNullOrEmpty(contentPart.Text))
            {
                textParts.Add(contentPart.Text);
            }
            else
            {
                Logger.LogDebug("Content part has no Text property. Kind: {Kind}", contentPart.Kind);
            }
        }

        var text = string.Join("\n", textParts);

        if (string.IsNullOrWhiteSpace(text))
        {
            // Check if there was a refusal
            var refusal = completion.Content.FirstOrDefault()?.Refusal;
            if (!string.IsNullOrEmpty(refusal))
            {
                Logger.LogError("OpenAI refused the request: {Refusal}", refusal);
                throw new InvalidOperationException($"OpenAI refused the request: {refusal}");
            }

            // Check if the response was cut off due to token limits
            var finishReason = completion.FinishReason;
            if (finishReason.ToString() == "Length")
            {
                Logger.LogError("OpenAI response was truncated due to token limits (FinishReason: Length). Input may be too large.");
                throw new InvalidOperationException("The input is too large for the AI to process. Try using a shorter Gherkin scenario or disable HTML context fetching.");
            }

            Logger.LogError("OpenAI returned an empty or whitespace-only text response. FinishReason: {FinishReason}",
                finishReason);
            throw new InvalidOperationException("OpenAI returned an empty text response");
        }

        return text;
    }
}
