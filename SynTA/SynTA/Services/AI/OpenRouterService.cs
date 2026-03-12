using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net;
using SynTA.Models.Domain;
using SynTA.Services.AI.Prompts;

namespace SynTA.Services.AI;

/// <summary>
/// OpenRouter implementation of the AI generation service.
/// Uses OpenRouter's OpenAI-compatible chat completions endpoint.
/// </summary>
public class OpenRouterService : BaseAIService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _endpoint;
    private const int MaxTransientAttempts = 2;

    public override string ProviderName => "OpenRouter";

    protected override string CurrentModelName =>
        !string.IsNullOrWhiteSpace(CustomModelName)
            ? CustomModelName.Trim()
            : ModelTier switch
            {
                AIModelTier.UltraFast => "google/gemini-2.5-flash-lite",
                AIModelTier.Fast => "openai/gpt-5-mini",
                AIModelTier.Smart => "anthropic/claude-3.7-sonnet",
                _ => "openai/gpt-5-mini"
            };

    public OpenRouterService(
        IConfiguration configuration,
        ILogger<OpenRouterService> logger,
        IPromptService promptService)
        : base(logger, promptService, configuration.GetValue<float>("OpenRouter:Temperature", 0.3f))
    {
        _apiKey = configuration["OpenRouter:ApiKey"] ??
            throw new InvalidOperationException("OpenRouter API key not configured");

        _endpoint = configuration["OpenRouter:Endpoint"] ?? "https://openrouter.ai/api/v1/chat/completions";

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        var referer = configuration["OpenRouter:HttpReferer"];
        if (!string.IsNullOrWhiteSpace(referer))
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("HTTP-Referer", referer);
        }

        var title = configuration["OpenRouter:XTitle"];
        if (!string.IsNullOrWhiteSpace(title))
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Title", title);
        }
    }

    protected override Task<string> GenerateGherkinContentAsync(string prompt, string language)
    {
        return GenerateCompletionAsync(prompt);
    }

    protected override Task<string> GenerateCypressContentAsync(string prompt, byte[]? screenshot)
    {
        return GenerateCompletionAsync(prompt, screenshot);
    }

    public override async Task<bool> TestConnectionAsync()
    {
        Logger.LogInformation("Testing OpenRouter API connection - Model: {Model}", CurrentModelName);
        try
        {
            var response = await GenerateCompletionAsync("Respond with exactly 'OK'.");
            var success = !string.IsNullOrWhiteSpace(response);

            if (success)
            {
                Logger.LogInformation("OpenRouter API connection test successful - Model: {Model}", CurrentModelName);
            }
            else
            {
                Logger.LogWarning("OpenRouter API connection test returned empty response - Model: {Model}", CurrentModelName);
            }

            return success;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "OpenRouter API connection test failed - Model: {Model}, Error: {ErrorMessage}", CurrentModelName, ex.Message);
            return false;
        }
    }

    private async Task<string> GenerateCompletionAsync(string userPrompt, byte[]? screenshot = null)
    {
        Logger.LogInformation(
            "Using OpenRouter model: {ModelName} (Tier: {Tier}, Temperature: {Temperature}, Multimodal: {IsMultimodal})",
            CurrentModelName,
            ModelTier,
            Temperature,
            screenshot != null);

        JsonArray userContent;
        if (screenshot == null)
        {
            userContent = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "text",
                    ["text"] = userPrompt
                }
            };
        }
        else
        {
            var base64Image = Convert.ToBase64String(screenshot);
            userContent = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "text",
                    ["text"] = userPrompt
                },
                new JsonObject
                {
                    ["type"] = "image_url",
                    ["image_url"] = new JsonObject
                    {
                        ["url"] = $"data:image/jpeg;base64,{base64Image}"
                    }
                }
            };
        }

        var payload = new JsonObject
        {
            ["model"] = CurrentModelName,
            ["messages"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "system",
                    ["content"] = "You are a professional, conservative test automation engineer and Cypress code generator. " +
                                  "Convert Gherkin scenarios into production-ready Cypress test files. " +
                                  "Strictly follow the instructions included in the prompt. " +
                                  "Follow professional best practices: prioritize test stability, be conservative in assertions, and avoid inventing selectors or assumptions not present in the prompt."
                },
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = userContent
                }
            },
            ["temperature"] = Temperature,
            ["max_tokens"] = 65536
        };

        try
        {
            return await ExecuteWithTransientRetryAsync(payload, allowTransportRetry: true);
        }
        catch (Exception ex) when (screenshot != null && (IsTransientTransportException(ex) || IsImageInputUnsupportedException(ex)))
        {
            Logger.LogWarning(ex,
            "OpenRouter multimodal upload failed. Retrying without screenshot. Model: {Model}",
                CurrentModelName);

            var textOnlyPayload = new JsonObject
            {
                ["model"] = payload["model"]?.DeepClone(),
                ["messages"] = new JsonArray
                {
                    payload["messages"]?[0]?.DeepClone(),
                    new JsonObject
                    {
                        ["role"] = "user",
                        ["content"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["type"] = "text",
                                ["text"] = userPrompt
                            }
                        }
                    }
                },
                ["temperature"] = payload["temperature"]?.DeepClone(),
                ["max_tokens"] = payload["max_tokens"]?.DeepClone()
            };

            return await ExecuteWithTransientRetryAsync(textOnlyPayload, allowTransportRetry: true);
        }
    }

    private async Task<string> ExecuteWithTransientRetryAsync(JsonObject payload, bool allowTransportRetry)
    {
        var payloadJson = payload.ToJsonString();
        Exception? lastException = null;

        for (var attempt = 1; attempt <= MaxTransientAttempts; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
                {
                    Version = HttpVersion.Version11,
                    VersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
                    Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
                };

                using var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Logger.LogError("OpenRouter returned HTTP {StatusCode}: {Body}", response.StatusCode, body);

                    var statusCode = (int)response.StatusCode;

                    if (IsImageInputUnsupportedResponse(statusCode, body))
                    {
                        throw new InvalidOperationException("OpenRouter model does not support image input.");
                    }

                    if (IsRetryableStatusCode(statusCode) && allowTransportRetry && attempt < MaxTransientAttempts)
                    {
                        Logger.LogWarning(
                            "Retryable HTTP {StatusCode} on attempt {Attempt}/{MaxAttempts}. Retrying after delay...",
                            statusCode, attempt, MaxTransientAttempts);
                        await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt));
                        continue;
                    }

                    throw new InvalidOperationException($"OpenRouter request failed with HTTP {statusCode}");
                }

                var root = JsonNode.Parse(body);
                var firstChoice = root?["choices"]?[0];
                var contentNode = firstChoice?["message"]?["content"];

                var text = ExtractContentText(contentNode);
                if (string.IsNullOrWhiteSpace(text))
                {
                    Logger.LogError("OpenRouter returned an empty response body content.");
                    throw new InvalidOperationException("OpenRouter returned an empty response");
                }

                return text;
            }
            catch (Exception ex) when (allowTransportRetry && attempt < MaxTransientAttempts && IsTransientTransportException(ex))
            {
                lastException = ex;
                Logger.LogWarning(ex,
                    "Transient OpenRouter transport error on attempt {Attempt}/{MaxAttempts}. Retrying...",
                    attempt,
                    MaxTransientAttempts);

                await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt));
            }
            catch (Exception ex)
            {
                lastException = ex;
                throw;
            }
        }

        throw new InvalidOperationException("OpenRouter request failed after retries.", lastException);
    }

    private static bool IsTransientTransportException(Exception exception)
    {
        return exception is HttpRequestException ||
               exception.InnerException is HttpRequestException ||
               exception is IOException ||
               exception.InnerException is IOException;
    }

    private static bool IsRetryableStatusCode(int statusCode)
    {
        return statusCode is 429 or 502 or 503 or 504;
    }

    private static bool IsImageInputUnsupportedResponse(int statusCode, string responseBody)
    {
        if (statusCode != (int)HttpStatusCode.NotFound)
        {
            return false;
        }

        return responseBody.Contains("No endpoints found that support image input", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsImageInputUnsupportedException(Exception exception)
    {
        return exception.Message.Contains("does not support image input", StringComparison.OrdinalIgnoreCase) ||
               (exception.InnerException?.Message?.Contains("does not support image input", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static string ExtractContentText(JsonNode? contentNode)
    {
        if (contentNode == null)
        {
            return string.Empty;
        }

        if (contentNode is JsonValue)
        {
            return contentNode.GetValue<string>();
        }

        if (contentNode is JsonArray contentArray)
        {
            var textParts = contentArray
                .Select(part => part?["text"]?.GetValue<string>() ?? string.Empty)
                .Where(part => !string.IsNullOrWhiteSpace(part));

            return string.Join("\n", textParts);
        }

        return string.Empty;
    }
}
