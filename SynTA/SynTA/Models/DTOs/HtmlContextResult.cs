namespace SynTA.Models.DTOs;

/// <summary>
/// Represents the result of fetching HTML context from a website, including
/// the simplified HTML content and an optional screenshot for multimodal AI processing.
/// </summary>
public class HtmlContextResult
{
    /// <summary>
    /// The simplified HTML content extracted from the page.
    /// </summary>
    public string HtmlContent { get; set; } = string.Empty;

    /// <summary>
    /// The screenshot of the page as a JPEG byte array. Null if screenshot capture failed or was disabled.
    /// </summary>
    public byte[]? Screenshot { get; set; }

    /// <summary>
    /// The page title extracted from the HTML.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The target URL that was fetched.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Indicates whether a screenshot was successfully captured.
    /// </summary>
    public bool HasScreenshot => Screenshot != null && Screenshot.Length > 0;

    /// <summary>
    /// The size of the screenshot in bytes, or 0 if no screenshot.
    /// </summary>
    public int ScreenshotSizeBytes => Screenshot?.Length ?? 0;
}
