using Google.GenAI;
using Google.GenAI.Types;
using SynTA.Models.Domain;
using SynTA.Services.AI.Prompts;
using SynTA.Services.AI.TextProcessing;

namespace SynTA.Services.AI;

/// <summary>
/// Google Gemini implementation of the AI generation service.
/// Uses Google's Gemini models for generating Gherkin scenarios and Cypress scripts.
/// </summary>
public class GeminiService : BaseAIService
{
    private readonly Client _client;
    private readonly HttpClient _httpClient;

    // Gemini image size limit (20MB for Gemini 1.5 Pro and later models)
    private const int MaxImageSizeBytes = 20 * 1024 * 1024;

    public override string ProviderName => "Gemini";

    /// <summary>
    /// Gets the model name based on the current model tier.
    /// </summary>
    protected override string CurrentModelName => ModelTier switch
    {
        AIModelTier.UltraFast => "gemini-2.5-flash-lite",
        AIModelTier.Fast => "gemini-2.5-flash",
        AIModelTier.Smart => "gemini-3-pro-preview",
        _ => "gemini-2.5-flash"
    };

    public GeminiService(
        IConfiguration configuration,
        ILogger<GeminiService> logger,
        IPromptService promptService)
        : base(logger, promptService, configuration.GetValue<float>("Gemini:Temperature", 0.3f))
    {
        var apiKey = configuration["Gemini:ApiKey"] ??
            throw new InvalidOperationException("Gemini API key not configured");

        // Create HttpClient with extended timeout for large context windows
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };

        // Note: Google.GenAI Client may not directly accept HttpClient in constructor
        // The timeout will be handled via CancellationToken in API calls if needed
        _client = new Client(apiKey: apiKey);
    }


    protected override async Task<string> GenerateGherkinContentAsync(string prompt, string language)
    {
        return await GenerateContentAsync(prompt, GetGherkinSystemInstruction(language));
    }

    protected override async Task<string> GenerateCypressContentAsync(string prompt, byte[]? screenshot)
    {
        return await GenerateContentAsync(prompt, GetCypressSystemInstruction(), screenshot);
    }

    public override async Task<bool> TestConnectionAsync()
    {
        Logger.LogInformation("Testing Gemini API connection - Model: {Model}", CurrentModelName);
        try
        {
            var config = new GenerateContentConfig
            {
                Temperature = 0f,
                MaxOutputTokens = 10
            };

            var response = await _client.Models.GenerateContentAsync(
                model: CurrentModelName,
                contents: "Hello, respond with 'OK' if you can read this.",
                config: config);

            var text = response.Candidates?[0]?.Content?.Parts?[0]?.Text;
            var success = !string.IsNullOrEmpty(text);

            if (success)
            {
                Logger.LogInformation("Gemini API connection test successful - Model: {Model}", CurrentModelName);
            }
            else
            {
                Logger.LogWarning("Gemini API connection test returned empty response - Model: {Model}", CurrentModelName);
            }

            return success;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Gemini API connection test failed - Model: {Model}, Error: {ErrorMessage}", CurrentModelName, ex.Message);
            return false;
        }
    }

    private Content GetGherkinSystemInstruction(string language)
    {
        var languageName = PromptService.GetLanguageName(language);
        return new Content
        {
            Parts = new List<Part>
            {
                new Part
                {
                    Text = $"You are an expert test automation engineer specializing in Behavior-Driven Development (BDD) and Gherkin syntax. " +
                           $"Your task is to generate comprehensive, well-structured Gherkin test scenarios that cover all aspects of the given user story. " +
                           $"Always provide accurate, production-ready test scenarios. " +
                           $"Generate all scenario text in {languageName}."
                }
            }
        };
    }

    private Content GetCypressSystemInstruction()
    {
        return new Content
        {
            Parts = new List<Part>
            {
                new Part
                {
                    Text = "You are a professional, conservative test automation engineer and Cypress code generator. " +
                           "Convert Gherkin scenarios into production-ready Cypress test files. " +
                           "Strictly follow the instructions included in the prompt. " +
                           "Follow professional best practices: prioritize test stability, be conservative in assertions, and avoid inventing selectors or assumptions not present in the prompt."
                }
            }
        };
    }

    private async Task<string> GenerateContentAsync(string userPrompt, Content systemInstruction, byte[]? screenshot = null)
    {
        Logger.LogInformation("Using Gemini model: {ModelName} (Tier: {Tier}, Temperature: {Temperature}, Multimodal: {IsMultimodal})", CurrentModelName, ModelTier, Temperature, screenshot != null);

        var config = new GenerateContentConfig
        {
            SystemInstruction = systemInstruction,
            Temperature = Temperature,
            MaxOutputTokens = 65536
        };

        // Build multimodal content if screenshot is provided
        List<Part> contentParts;
        if (screenshot != null)
        {
            contentParts = new List<Part>
            {
                new Part { Text = userPrompt },
                new Part
                {
                    InlineData = new Google.GenAI.Types.Blob
                    {
                        MimeType = "image/jpeg",
                        Data = screenshot
                    }
                }
            };
            Logger.LogDebug("Sending multimodal request with screenshot - Size: {Size} bytes", screenshot.Length);
        }
        else
        {
            contentParts = new List<Part> { new Part { Text = userPrompt } };
        }

        var content = new Content { Parts = contentParts };

        GenerateContentResponse response;
        try
        {
            response = await _client.Models.GenerateContentAsync(
                model: CurrentModelName,
                contents: content,
                config: config);
        }
        catch (Exception ex) when (ex.Message.Contains("temperature") || ex.Message.Contains("Temperature"))
        {
            Logger.LogWarning("Model {ModelName} does not support custom temperature value {Temperature}. Retrying with default temperature.", CurrentModelName, Temperature);
            
            // Retry without temperature (uses default)
            config = new GenerateContentConfig
            {
                SystemInstruction = systemInstruction,
                MaxOutputTokens = 65536
                // Temperature omitted - will use model default
            };
            
            Logger.LogInformation("Retrying with default temperature - Model: {ModelName}", CurrentModelName);
            response = await _client.Models.GenerateContentAsync(
                model: CurrentModelName,
                contents: content,
                config: config);
        }

        // Check if the response was blocked
        if (response.Candidates == null || response.Candidates.Count == 0)
        {
            // Check for prompt feedback (blocked due to safety)
            var blockReason = response.PromptFeedback?.BlockReason;
            if (blockReason != null)
            {
                Logger.LogError("Gemini blocked the request. BlockReason: {BlockReason}", blockReason);
                throw new InvalidOperationException($"Gemini blocked the request due to: {blockReason}");
            }

            Logger.LogError("Gemini returned no candidates");
            throw new InvalidOperationException("Gemini returned an empty response with no candidates");
        }

        var candidate = response.Candidates[0];

        // Check finish reason for potential issues
        var finishReason = candidate.FinishReason;
        if (finishReason == Google.GenAI.Types.FinishReason.SAFETY)
        {
            Logger.LogError("Gemini response was blocked due to safety filters");
            throw new InvalidOperationException("The content was blocked by Gemini's safety filters. Please modify your input.");
        }
        else if (finishReason == Google.GenAI.Types.FinishReason.MAX_TOKENS)
        {
            Logger.LogWarning("Gemini response was truncated due to max tokens limit");
        }

        // Collect text from all parts (Gemini may return content in multiple parts)
        var parts = candidate.Content?.Parts;
        if (parts == null || parts.Count == 0)
        {
            // Special handling for MAX_TOKENS with no content - this happens when input is too large
            if (finishReason == Google.GenAI.Types.FinishReason.MAX_TOKENS)
            {
                Logger.LogError("Gemini hit token limit with no output. The input prompt may be too large.");
                throw new InvalidOperationException("The input is too large for Gemini to process. Try disabling HTML context fetching or using a shorter Gherkin scenario.");
            }

            Logger.LogError("Gemini candidate has no content parts. FinishReason: {FinishReason}", finishReason);
            throw new InvalidOperationException("Gemini returned a response with no content");
        }

        var textParts = new List<string>();
        foreach (var part in parts)
        {
            if (!string.IsNullOrEmpty(part.Text))
            {
                textParts.Add(part.Text);
            }
        }

        var text = string.Join("", textParts);

        if (string.IsNullOrEmpty(text))
        {
            Logger.LogError("Gemini returned empty text content. FinishReason: {FinishReason}", finishReason);
            throw new InvalidOperationException("Gemini returned an empty response");
        }

        Logger.LogDebug("Gemini response - FinishReason: {FinishReason}, PartsCount: {Count}, TextLength: {Length}",
            finishReason, parts.Count, text.Length);

        return text;
    }
}
