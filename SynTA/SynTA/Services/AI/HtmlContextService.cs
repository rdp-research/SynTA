using Microsoft.Extensions.DependencyInjection;
using SynTA.Models.DTOs;
using SynTA.Services.ImageProcessing;

namespace SynTA.Services.AI;

/// <summary>
/// Facade service for extracting HTML context from websites.
/// Coordinates between WebScraperService (fetching) and HtmlContentProcessor (processing).
/// </summary>
public class HtmlContextService : IHtmlContextService, IAsyncDisposable
{
    private readonly ILogger<HtmlContextService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly WebScraperService _webScraper;
    private readonly HtmlContentProcessor _htmlProcessor;

    // Image size limits for AI providers
    private const int MaxImageSizeBytes = 18 * 1024 * 1024;

    public HtmlContextService(
        ILogger<HtmlContextService> logger,
        IServiceProvider serviceProvider,
        WebScraperService webScraper,
        HtmlContentProcessor htmlProcessor)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _webScraper = webScraper;
        _htmlProcessor = htmlProcessor;
    }

    public async Task<HtmlContextResult> FetchAndSimplifyHtmlAsync(string url)
    {
        return await FetchAndSimplifyHtmlAsync(url, new HtmlFetchOptions());
    }

    public async Task<HtmlContextResult> FetchAndSimplifyHtmlAsync(string url, HtmlFetchOptions options)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be null or empty", nameof(url));

        // Step 1: Fetch raw content using WebScraperService
        var rawContent = await _webScraper.FetchRawContentAsync(url, options);

        // Step 2: Process screenshot if it exceeds size limits
        byte[]? processedScreenshot = null;
        if (options.CaptureScreenshot && rawContent.Screenshot != null)
        {
            processedScreenshot = await ProcessScreenshotAsync(rawContent.Screenshot, rawContent.OperationId);
        }

        // Step 3: Process HTML using HtmlContentProcessor
        var simplifiedHtml = _htmlProcessor.ProcessHtmlContent(rawContent, options);

        return new HtmlContextResult
        {
            HtmlContent = simplifiedHtml,
            Screenshot = processedScreenshot,
            Title = rawContent.PageMetadata.Title,
            Url = url
        };
    }

    /// <summary>
    /// Processes screenshot to ensure it meets AI API size limits.
    /// </summary>
    private async Task<byte[]?> ProcessScreenshotAsync(byte[] screenshot, string operationId)
    {
        using var scope = _serviceProvider.CreateScope();
        var imageProcessingService = scope.ServiceProvider.GetRequiredService<IImageProcessingService>();

        if (!imageProcessingService.IsWithinSizeLimit(screenshot, MaxImageSizeBytes))
        {
            _logger.LogWarning(
                "[{OperationId}] Screenshot exceeds size limit ({Size} bytes > {MaxSize} bytes), processing...",
                operationId, screenshot.Length, MaxImageSizeBytes);

            try
            {
                var processed = await imageProcessingService.ProcessImageForAIAsync(
                    screenshot,
                    MaxImageSizeBytes,
                    operationId);

                _logger.LogInformation(
                    "[{OperationId}] Screenshot processed successfully - FinalSize: {Size} bytes ({SizeMB:F2} MB)",
                    operationId, processed.Length, processed.Length / (1024.0 * 1024.0));

                return processed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[{OperationId}] Failed to process screenshot to meet size limits - Error: {ErrorMessage}. Screenshot will not be included.",
                    operationId, ex.Message);
                return null;
            }
        }
        else
        {
            _logger.LogInformation(
                "[{OperationId}] Screenshot is within size limit, no processing needed",
                operationId);
            return screenshot;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _webScraper.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Options for HTML fetching behavior
/// </summary>
public class HtmlFetchOptions
{
    /// <summary>
    /// Timeout in milliseconds for page load
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Optional CSS selector to wait for before extracting HTML
    /// </summary>
    public string? WaitForSelector { get; set; }

    /// <summary>
    /// Additional wait time in milliseconds after page load for JS rendering
    /// </summary>
    public int AdditionalWaitMs { get; set; } = 1000;

    /// <summary>
    /// Whether to capture a screenshot for multimodal AI input.
    /// </summary>
    public bool CaptureScreenshot { get; set; } = true;

    /// <summary>
    /// Whether to include page metadata in generated extraction context.
    /// </summary>
    public bool IncludePageMetadata { get; set; } = true;

    /// <summary>
    /// Whether to include the UI element mapping section in generated extraction context.
    /// </summary>
    public bool IncludeUiElementMap { get; set; } = true;

    /// <summary>
    /// Whether to include accessibility tree context.
    /// </summary>
    public bool IncludeAccessibilityTree { get; set; } = true;

    /// <summary>
    /// Whether to include simplified HTML content.
    /// </summary>
    public bool IncludeSimplifiedHtml { get; set; } = true;
}

/// <summary>
/// Represents an interactive element on the page with comprehensive selector information
/// to reduce AI hallucination when generating Cypress tests.
/// </summary>
public class InteractiveElement
{
    public string Tag { get; set; } = "";
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? ClassName { get; set; }
    public string? Placeholder { get; set; }
    public string? Text { get; set; }
    public string? Href { get; set; }
    public string? DataTestId { get; set; }
    public string? AriaLabel { get; set; }
    public string? Role { get; set; }
    public double Opacity { get; set; } = 1.0;
    public bool InShadowDom { get; set; }
    public bool IsVisible { get; set; } = true;
    public string? SemanticRegion { get; set; }
    public string? CssPath { get; set; }
    public string? RecommendedSelector { get; set; }
    public int SelectorStabilityScore { get; set; }
    public string? AssociatedLabel { get; set; }
    public string? FormContext { get; set; }
    public string? NearbyContext { get; set; }
    public bool IsRequired { get; set; }
    public string? Value { get; set; }
    public string? ValidationPattern { get; set; }
    public string? OtherAttributes { get; set; }
}

/// <summary>
/// Represents extracted page metadata including title, description, and other SEO/identity information.
/// </summary>
public class PageMetadata
{
    public string Title { get; set; } = "";
    public string? MetaDescription { get; set; }
    public string? CanonicalUrl { get; set; }
    public string? OgTitle { get; set; }
    public string? OgDescription { get; set; }
    public string? H1Text { get; set; }
    public string? Language { get; set; }
}
