namespace SynTA.Constants;

/// <summary>
/// Constants for AI service provider names.
/// Used for keyed service resolution and configuration.
/// </summary>
public static class AIProviders
{
    /// <summary>
    /// OpenAI provider identifier.
    /// </summary>
    public const string OpenAI = nameof(OpenAI);

    /// <summary>
    /// Google Gemini provider identifier.
    /// </summary>
    public const string Gemini = nameof(Gemini);

    /// <summary>
    /// OpenRouter provider identifier.
    /// </summary>
    public const string OpenRouter = nameof(OpenRouter);

    /// <summary>
    /// Gets all available provider names.
    /// </summary>
    public static readonly string[] All = { OpenAI, Gemini, OpenRouter };

    /// <summary>
    /// Validates if a provider name is supported.
    /// </summary>
    public static bool IsValid(string provider)
    {
        return Array.Exists(All, p => p.Equals(provider, StringComparison.OrdinalIgnoreCase));
    }
}
