namespace SynTA.Services.AI.TextProcessing;

/// <summary>
/// Static utility class for cleaning and processing AI response text.
/// Handles markdown code block removal and Gherkin syntax stripping.
/// </summary>
public static class AIResponseCleaner
{
    /// <summary>
    /// Strips markdown code block formatting from AI responses.
    /// AI models often return code wrapped in ```language ... ``` blocks despite instructions not to.
    /// </summary>
    /// <param name="content">The raw AI response content</param>
    /// <returns>The content with markdown code blocks removed</returns>
    public static string StripMarkdownCodeBlocks(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return content;

        var result = content.Trim();
        var originalLength = result.Length;

        // Pattern to match code blocks: ```language or ``` at start, ``` at end
        // Handles: ```typescript, ```ts, ```gherkin, ```feature, ``` (no language), etc.
        if (result.StartsWith("```"))
        {
            // Find the end of the first line (the opening ```language)
            var firstNewline = result.IndexOf('\n');
            if (firstNewline > 0)
            {
                result = result.Substring(firstNewline + 1);
            }
            else
            {
                // Just ``` with no newline, remove it
                result = result.Substring(3);
            }
        }

        // Remove trailing ```
        if (result.TrimEnd().EndsWith("```"))
        {
            result = result.TrimEnd();
            result = result.Substring(0, result.Length - 3).TrimEnd();
        }

        return result;
    }

    /// <summary>
    /// Removes raw Gherkin syntax that the AI may have incorrectly included in Cypress scripts.
    /// AI models sometimes include raw "Scenario:", "Feature:", "Given", "When", "Then" lines
    /// which cause TypeScript compilation errors.
    /// </summary>
    /// <param name="content">The content to clean</param>
    /// <returns>The content with raw Gherkin syntax removed</returns>
    public static string StripRawGherkinSyntax(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return content;

        var lines = content.Split('\n');
        var cleanedLines = new List<string>();

        foreach (var line in lines)
        {
            var trimmedLine = line.TrimStart();

            // Check if the line starts with raw Gherkin keywords (not as comments or strings)
            // These patterns indicate the AI incorrectly included Gherkin syntax as code
            if (IsRawGherkinLine(trimmedLine))
            {
                continue; // Skip this line
            }

            cleanedLines.Add(line);
        }

        return string.Join('\n', cleanedLines);
    }

    /// <summary>
    /// Determines if a line is raw Gherkin syntax that shouldn't be in JavaScript/TypeScript code.
    /// </summary>
    /// <param name="trimmedLine">The line to check (should be trimmed)</param>
    /// <returns>True if the line is raw Gherkin syntax</returns>
    private static bool IsRawGherkinLine(string trimmedLine)
    {
        // Skip empty lines
        if (string.IsNullOrWhiteSpace(trimmedLine))
            return false;

        // Skip lines that are comments (these are OK - Gherkin in comments is expected)
        if (trimmedLine.StartsWith("//") || trimmedLine.StartsWith("/*") || trimmedLine.StartsWith("*"))
            return false;

        // Skip lines that are string literals (e.g., inside describe() or it())
        if (trimmedLine.StartsWith("'") || trimmedLine.StartsWith("\"") || trimmedLine.StartsWith("`"))
            return false;

        // Check for raw Gherkin keywords at the start of the line
        // These should NOT appear as standalone code statements
        string[] gherkinKeywords = ["Scenario:", "Scenario Outline:", "Feature:", "Background:", "Examples:"];

        foreach (var keyword in gherkinKeywords)
        {
            if (trimmedLine.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // Check for Gherkin step keywords that might appear as raw code
        // But be careful not to strip valid JavaScript that might contain these words
        // Only strip if it looks like a raw Gherkin line (keyword followed by text, not in a function call)
        string[] stepKeywords = ["Given ", "When ", "Then ", "And ", "But "];

        foreach (var keyword in stepKeywords)
        {
            if (trimmedLine.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
            {
                // Make sure it's not a valid JS statement (e.g., not followed by '(' or '=')
                var restOfLine = trimmedLine[keyword.Length..].TrimStart();
                if (!string.IsNullOrEmpty(restOfLine) &&
                    !restOfLine.StartsWith("(") &&
                    !restOfLine.StartsWith("=") &&
                    !restOfLine.StartsWith("{"))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
