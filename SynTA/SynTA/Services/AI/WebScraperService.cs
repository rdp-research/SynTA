using Microsoft.Playwright;
using SynTA.Models.DTOs;
using SynTA.Services.AI.Scripts;

namespace SynTA.Services.AI;

/// <summary>
/// Service responsible for web scraping using Playwright.
/// Handles browser lifecycle, network interception, DOM manipulation, and screenshot capture.
/// </summary>
public class WebScraperService : IAsyncDisposable
{
    private readonly ILogger<WebScraperService> _logger;
    private readonly SemaphoreSlim _browserLock = new(1, 1);
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private bool _isInitialized;
    private bool _disposed;

    // Configuration constants
    private const int DefaultTimeoutMs = 30000;
    private const int NetworkIdleTimeoutMs = 5000;

    public WebScraperService(ILogger<WebScraperService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initializes Playwright and the browser instance lazily
    /// </summary>
    private async Task EnsureInitializedAsync()
    {
        // If we think we're initialized, verify the browser is still connected.
        if (_isInitialized)
        {
            try
            {
                if (_browser == null || !_browser.IsConnected)
                {
                    _logger.LogWarning("Playwright browser is not connected - clearing initialized flag to allow re-initialization");
                    _isInitialized = false;
                }
                else
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while checking Playwright browser connection - will attempt re-initialization");
                _isInitialized = false;
            }
        }

        await _browserLock.WaitAsync();
        try
        {
            if (_isInitialized) return;

            _logger.LogInformation("Initializing Playwright headless browser for web scraping...");

            try
            {
                _playwright = await Playwright.CreateAsync();
                _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    Args = new[]
                    {
                        "--no-sandbox",
                        "--disable-gpu",
                        "--disable-dev-shm-usage"
                    }
                });

                _isInitialized = true;
                _logger.LogInformation("Playwright browser initialized successfully - BrowserType: Chromium, Headless: true");
            }
            catch (Exception ex)
            {
                // Ensure we don't leave the service in an initialized (but broken) state
                _isInitialized = false;
                _logger.LogError(ex, "Failed to initialize Playwright browser - Error: {ErrorMessage}", ex.Message);
                throw;
            }
        }
        finally
        {
            _browserLock.Release();
        }
    }

    /// <summary>
    /// Fetches raw HTML and screenshot from a URL using Playwright.
    /// </summary>
    public async Task<RawWebContent> FetchRawContentAsync(string url, HtmlFetchOptions options)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be null or empty", nameof(url));

        var operationId = Guid.NewGuid().ToString("N")[..8];
        _logger.LogInformation(
            "[{OperationId}] Starting web content fetch - Url: {Url}, WaitForSelector: {Selector}, Timeout: {Timeout}ms",
            operationId, url, options.WaitForSelector ?? "none", options.TimeoutMs);

        try
        {
            await EnsureInitializedAsync();

            var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                BypassCSP = true,
                JavaScriptEnabled = true,
                IgnoreHTTPSErrors = true,
                ExtraHTTPHeaders = new Dictionary<string, string>
                {
                    ["Accept-Language"] = "en-US,en;q=0.9",
                    ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7",
                    ["sec-ch-ua"] = "\"Not A(Brand\";v=\"99\", \"Google Chrome\";v=\"121\", \"Chromium\";v=\"121\""
                }
            });

            // Block unnecessary resources for performance improvement
            // IMPORTANT: Allow fonts (critical for layout and icon fonts), use placeholders for images
            await context.RouteAsync("**/*", async route =>
            {
                var request = route.Request;
                var resourceType = request.ResourceType;

                // Abort media (videos, audio) - not critical for layout
                if (resourceType == "media")
                {
                    await route.AbortAsync();
                }
                // Replace images with 1x1 transparent GIF to preserve container dimensions
                else if (resourceType == "image")
                {
                    // 1x1 transparent GIF base64 encoded
                    await route.FulfillAsync(new RouteFulfillOptions
                    {
                        Status = 200,
                        ContentType = "image/gif",
                        BodyBytes = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7")
                    });
                }
                // Allow fonts - critical for layout calculation and icon fonts (which act as buttons)
                else
                {
                    await route.ContinueAsync();
                }
            });

            await using var contextDisposable = context;

            try
            {
                var page = await context.NewPageAsync();

                // Set default timeout
                page.SetDefaultTimeout(options.TimeoutMs);

                _logger.LogDebug("[{OperationId}] Navigating to {Url}...", operationId, url);

                // Navigate to the page
                var response = await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = options.TimeoutMs
                });

                if (response == null || !response.Ok)
                {
                    _logger.LogWarning("[{OperationId}] Page load returned non-OK status - Url: {Url}, Status: {Status}",
                        operationId, url, response?.Status ?? 0);
                }
                else
                {
                    _logger.LogDebug("[{OperationId}] Page loaded successfully - Status: {Status}", operationId, response.Status);
                }

                // Wait for network to be idle (dynamic content to load)
                try
                {
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
                    {
                        Timeout = NetworkIdleTimeoutMs
                    });
                    _logger.LogDebug("[{OperationId}] Network idle state reached", operationId);
                }
                catch (TimeoutException)
                {
                    // Network idle timeout is acceptable - page might have continuous polling
                    _logger.LogDebug("[{OperationId}] Network idle timeout for {Url}, continuing with current content", operationId, url);
                }

                // Perform smart scroll to trigger lazy loading and hydration
                await PerformSmartScrollAsync(page);

                // Enrich DOM with visibility data (including shadow DOM)
                await EnrichDomDataAsync(page);

                // Identify trigger relationships (menu buttons → hidden content like dropdowns, modals)
                _logger.LogDebug("[{OperationId}] Analyzing trigger relationships for hidden content", operationId);
                await page.EvaluateAsync(HtmlExtractionScripts.TriggerRelationshipScript);

                // Wait for specific selector if provided
                if (!string.IsNullOrEmpty(options.WaitForSelector))
                {
                    try
                    {
                        await page.WaitForSelectorAsync(options.WaitForSelector, new PageWaitForSelectorOptions
                        {
                            Timeout = options.TimeoutMs / 2
                        });
                    }
                    catch (TimeoutException)
                    {
                        _logger.LogWarning("Selector '{Selector}' not found on {Url}", options.WaitForSelector, url);
                    }
                }

                // Additional wait for JavaScript frameworks to render
                await page.WaitForTimeoutAsync(options.AdditionalWaitMs);

                // SCROLL PAGE: Trigger scroll-based animations and lazy loading
                // This works for ALL website types (Wix scroll-reveal, WordPress fade-in, React AOS, etc.)
                _logger.LogInformation("[{OperationId}] Scrolling page to trigger animations and lazy loading", operationId);
                try
                {
                    await page.EvaluateAsync(@"
                        (async () => {
                            const height = document.body.scrollHeight;
                            const step = window.innerHeight;
                            // Scroll down in steps
                            for (let y = 0; y < height; y += step) {
                                window.scrollTo(0, y);
                                await new Promise(r => setTimeout(r, 100));
                            }
                            // Scroll to bottom
                            window.scrollTo(0, height);
                            await new Promise(r => setTimeout(r, 200));
                            // Return to top
                            window.scrollTo(0, 0);
                            await new Promise(r => setTimeout(r, 300));
                        })()
                    ");
                    _logger.LogInformation("[{OperationId}] Page scroll completed successfully", operationId);
                }
                catch (Exception scrollEx)
                {
                    _logger.LogWarning(scrollEx, "[{OperationId}] Page scroll failed, continuing without scroll trigger", operationId);
                }

                // ATOMIC DOM CAPTURE: Execute visibility annotation, interactive elements extraction,
                // and HTML capture in a SINGLE JavaScript call to prevent race conditions.
                // Previously, these were separate calls and React/Vue/Angular could re-render between them.
                _logger.LogDebug("[{OperationId}] Executing atomic DOM capture", operationId);
                
                string html;
                List<InteractiveElement> interactiveElements;
                
                try
                {
                    var atomicResult = await page.EvaluateAsync<System.Text.Json.JsonElement>(HtmlExtractionScripts.AtomicDomCaptureScript);
                    
                    if (atomicResult.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                    {
                        html = atomicResult.GetProperty("html").GetString() ?? await page.ContentAsync();
                        interactiveElements = ParseAtomicElements(atomicResult.GetProperty("elements"));
                        _logger.LogDebug("[{OperationId}] Atomic capture successful: {ElementCount} elements, {HtmlLen} chars",
                            operationId, interactiveElements.Count, html.Length);
                    }
                    else
                    {
                        // Fallback to legacy approach if atomic fails
                        _logger.LogWarning("[{OperationId}] Atomic capture failed, using legacy approach", operationId);
                        await AnnotateElementVisibilityAsync(page);
                        html = await page.ContentAsync();
                        interactiveElements = await ExtractInteractiveElementsAsync(page);
                    }
                }
                catch (Exception atomicEx)
                {
                    // Fallback to legacy approach on any error
                    _logger.LogWarning(atomicEx, "[{OperationId}] Atomic capture exception, using legacy approach", operationId);
                    await AnnotateElementVisibilityAsync(page);
                    html = await page.ContentAsync();
                    interactiveElements = await ExtractInteractiveElementsAsync(page);
                }

                // Extract page metadata (title, meta description, etc.) - lightweight, no race condition
                var pageMetadata = options.IncludePageMetadata
                    ? await ExtractPageMetadataAsync(page)
                    : new PageMetadata();

                // Extract accessibility tree for semantic context - separate call is OK
                var accessibilityTree = options.IncludeAccessibilityTree
                    ? await GetAccessibilityTreeAsync(page)
                    : string.Empty;

                if (!options.IncludeUiElementMap)
                {
                    interactiveElements = new List<InteractiveElement>();
                }

                // Capture screenshot
                byte[]? screenshot = options.CaptureScreenshot
                    ? await CaptureScreenshotAsync(page, operationId)
                    : null;

                _logger.LogInformation(
                    "[{OperationId}] Successfully fetched web content - Url: {Url}, HtmlSize: {HtmlSize} chars, InteractiveElements: {ElementCount}, ScreenshotCaptured: {HasScreenshot}",
                    operationId, url, html.Length, interactiveElements.Count, screenshot != null);

                return new RawWebContent
                {
                    Html = html,
                    Screenshot = screenshot,
                    PageMetadata = pageMetadata,
                    AccessibilityTree = accessibilityTree,
                    InteractiveElements = interactiveElements,
                    Url = url,
                    OperationId = operationId
                };
            }
            catch (Exception)
            {
                // Ensure context is disposed on exception, then rethrow
                await contextDisposable.DisposeAsync();
                throw;
            }
        }
        catch (PlaywrightException ex)
        {
            // Clear initialized flag so we attempt re-initialization on next request
            _isInitialized = false;
            _logger.LogError(ex,
                "[{OperationId}] Playwright error fetching content - Url: {Url}, Error: {ErrorMessage}",
                operationId, url, ex.Message);
            throw new InvalidOperationException($"Failed to fetch content from {url}: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            // Clear initialized flag to allow recovery if the browser is in a bad state
            _isInitialized = false;
            _logger.LogError(ex,
                "[{OperationId}] Unexpected error fetching content - Url: {Url}, Error: {ErrorMessage}",
                operationId, url, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Captures a screenshot of the page.
    /// </summary>
    private async Task<byte[]?> CaptureScreenshotAsync(IPage page, string operationId)
    {
        try
        {
            // Ensure viewport is set to standard desktop size before screenshot
            await page.SetViewportSizeAsync(1920, 1080);

            var screenshot = await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Type = ScreenshotType.Jpeg,
                Quality = 70,
                FullPage = true // Capture entire page to match HTML context sent to AI
            });

            _logger.LogInformation(
                "[{OperationId}] Screenshot captured - Size: {Size} bytes ({SizeMB:F2} MB), FullPage: true",
                operationId, screenshot.Length, screenshot.Length / (1024.0 * 1024.0));

            return screenshot;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[{OperationId}] Failed to capture screenshot, continuing without it - Error: {ErrorMessage}",
                operationId, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Performs a smart scroll through the page to trigger lazy loading and hydration.
    /// </summary>
    private async Task PerformSmartScrollAsync(IPage page)
    {
        try
        {
            _logger.LogDebug("Performing smart scroll to trigger lazy loading...");
            await page.EvaluateAsync(HtmlExtractionScripts.SmartScrollScript);
            _logger.LogDebug("Smart scroll completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to perform smart scroll, continuing without it");
        }
    }

    /// <summary>
    /// Enriches the DOM with visibility data by recursively traversing all elements including shadow DOM.
    /// </summary>
    private async Task EnrichDomDataAsync(IPage page)
    {
        try
        {
            _logger.LogDebug("Enriching DOM with visibility data (including shadow DOM)...");
            await page.EvaluateAsync(HtmlExtractionScripts.DomEnrichmentScript);
            _logger.LogDebug("DOM enrichment completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enrich DOM with visibility data, continuing without it");
        }
    }

    /// <summary>
    /// Extracts page metadata including title, meta description, canonical URL, and other SEO/identity info.
    /// </summary>
    private async Task<PageMetadata> ExtractPageMetadataAsync(IPage page)
    {
        var metadata = new PageMetadata();

        try
        {
            _logger.LogDebug("Extracting page metadata...");

            var metadataJson = await page.EvaluateAsync<System.Text.Json.JsonElement>(HtmlExtractionScripts.PageMetadataScript);

            metadata.Title = GetJsonStringOrNull(metadataJson, "title") ?? "";
            metadata.MetaDescription = GetJsonStringOrNull(metadataJson, "metaDescription");
            metadata.CanonicalUrl = GetJsonStringOrNull(metadataJson, "canonicalUrl");
            metadata.OgTitle = GetJsonStringOrNull(metadataJson, "ogTitle");
            metadata.OgDescription = GetJsonStringOrNull(metadataJson, "ogDescription");
            metadata.H1Text = GetJsonStringOrNull(metadataJson, "h1Text");
            metadata.Language = GetJsonStringOrNull(metadataJson, "language");

            _logger.LogDebug("Page metadata extracted: Title='{Title}', H1='{H1}'", metadata.Title, metadata.H1Text);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract page metadata, continuing without it");
        }

        return metadata;
    }

    /// <summary>
    /// Annotates elements with visibility markers without removing them from the DOM.
    /// </summary>
    private async Task AnnotateElementVisibilityAsync(IPage page)
    {
        try
        {
            await page.EvaluateAsync(HtmlExtractionScripts.VisibilityAnnotationScript);
            _logger.LogDebug("Annotated elements with visibility markers");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to annotate element visibility, continuing without annotations");
        }
    }

    /// <summary>
    /// Gets the accessibility tree from the page and converts it to a simple string representation.
    /// </summary>
    private async Task<string> GetAccessibilityTreeAsync(IPage page)
    {
        try
        {
            _logger.LogDebug("Extracting accessibility tree...");

            var snapshot = await page.EvaluateAsync<System.Text.Json.JsonElement?>(HtmlExtractionScripts.AccessibilityTreeScript);

            if (snapshot == null || snapshot.Value.ValueKind == System.Text.Json.JsonValueKind.Null)
            {
                _logger.LogDebug("Accessibility snapshot returned null");
                return string.Empty;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<!-- ACCESSIBILITY TREE -->");
            BuildAccessibilityTreeFromJson(snapshot.Value, sb, 0);
            sb.AppendLine("<!-- END ACCESSIBILITY TREE -->");

            _logger.LogDebug("Accessibility tree extracted successfully");
            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract accessibility tree, continuing without it");
            return string.Empty;
        }
    }

    /// <summary>
    /// Recursively builds a string representation of the accessibility tree from a JSON element.
    /// </summary>
    private static void BuildAccessibilityTreeFromJson(System.Text.Json.JsonElement node, System.Text.StringBuilder sb, int depth)
    {
        if (node.ValueKind != System.Text.Json.JsonValueKind.Object)
            return;

        var indent = new string(' ', depth * 2);
        var role = node.TryGetProperty("role", out var roleProp) ? roleProp.GetString() ?? "unknown" : "unknown";
        var name = node.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
        var value = node.TryGetProperty("value", out var valueProp) ? valueProp.GetString() : null;

        var nameStr = !string.IsNullOrEmpty(name) ? $", Name: {name}" : "";
        var valueStr = !string.IsNullOrEmpty(value) ? $", Value: {value}" : "";

        sb.AppendLine($"<!-- {indent}Role: {role}{nameStr}{valueStr} -->");

        if (node.TryGetProperty("children", out var children) && children.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var child in children.EnumerateArray())
            {
                BuildAccessibilityTreeFromJson(child, sb, depth + 1);
            }
        }
    }

    /// <summary>
    /// Extracts interactive elements from the page with comprehensive selector information.
    /// </summary>
    private async Task<List<InteractiveElement>> ExtractInteractiveElementsAsync(IPage page)
    {
        var elements = new List<InteractiveElement>();

        try
        {
            var formData = await page.EvaluateAsync<System.Text.Json.JsonElement>(HtmlExtractionScripts.InteractiveElementsScript);

            if (formData.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in formData.EnumerateArray())
                {
                    elements.Add(new InteractiveElement
                    {
                        Tag = GetJsonString(item, "tag"),
                        Id = GetJsonStringOrNull(item, "id"),
                        Name = GetJsonStringOrNull(item, "name"),
                        Type = GetJsonStringOrNull(item, "type"),
                        ClassName = GetJsonStringOrNull(item, "className"),
                        Placeholder = GetJsonStringOrNull(item, "placeholder"),
                        Text = GetJsonStringOrNull(item, "text"),
                        Href = GetJsonStringOrNull(item, "href"),
                        DataTestId = GetJsonStringOrNull(item, "dataTestId"),
                        AriaLabel = GetJsonStringOrNull(item, "ariaLabel"),
                        Role = GetJsonStringOrNull(item, "role"),
                        Opacity = GetJsonDouble(item, "opacity"),
                        InShadowDom = GetJsonBool(item, "inShadowDom"),
                        IsVisible = GetJsonBool(item, "isVisible"),
                        SemanticRegion = GetJsonStringOrNull(item, "semanticRegion"),
                        CssPath = GetJsonStringOrNull(item, "cssPath"),
                        RecommendedSelector = GetJsonStringOrNull(item, "recommendedSelector"),
                        SelectorStabilityScore = GetJsonInt(item, "selectorStabilityScore"),
                        AssociatedLabel = GetJsonStringOrNull(item, "associatedLabel"),
                        FormContext = GetJsonStringOrNull(item, "formContext"),
                        NearbyContext = GetJsonStringOrNull(item, "nearbyContext"),
                        IsRequired = GetJsonBool(item, "isRequired"),
                        Value = GetJsonStringOrNull(item, "value"),
                        ValidationPattern = GetJsonStringOrNull(item, "validationPattern")
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract interactive elements, continuing without them");
        }

        return elements;
    }

    /// <summary>
    /// Parses interactive elements from the AtomicDomCaptureScript result.
    /// </summary>
    private List<InteractiveElement> ParseAtomicElements(System.Text.Json.JsonElement elementsArray)
    {
        var elements = new List<InteractiveElement>();

        try
        {
            if (elementsArray.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in elementsArray.EnumerateArray())
                {
                    elements.Add(new InteractiveElement
                    {
                        Tag = GetJsonString(item, "tag"),
                        Id = GetJsonStringOrNull(item, "id"),
                        Name = GetJsonStringOrNull(item, "name"),
                        Type = GetJsonStringOrNull(item, "type"),
                        ClassName = GetJsonStringOrNull(item, "className"),
                        Placeholder = GetJsonStringOrNull(item, "placeholder"),
                        Text = GetJsonStringOrNull(item, "text"),
                        Href = GetJsonStringOrNull(item, "href"),
                        DataTestId = GetJsonStringOrNull(item, "dataTestId"),
                        AriaLabel = GetJsonStringOrNull(item, "ariaLabel"),
                        Role = GetJsonStringOrNull(item, "role"),
                        Opacity = GetJsonDouble(item, "opacity"),
                        InShadowDom = GetJsonBool(item, "inShadowDom"),
                        IsVisible = GetJsonBool(item, "isVisible"),
                        SemanticRegion = GetJsonStringOrNull(item, "semanticRegion"),
                        CssPath = GetJsonStringOrNull(item, "cssPath"),
                        RecommendedSelector = GetJsonStringOrNull(item, "recommendedSelector"),
                        SelectorStabilityScore = GetJsonInt(item, "selectorStabilityScore"),
                        AssociatedLabel = GetJsonStringOrNull(item, "associatedLabel"),
                        FormContext = GetJsonStringOrNull(item, "formContext"),
                        NearbyContext = GetJsonStringOrNull(item, "nearbyContext"),
                        IsRequired = GetJsonBool(item, "isRequired"),
                        Value = GetJsonStringOrNull(item, "value"),
                        ValidationPattern = GetJsonStringOrNull(item, "validationPattern")
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse atomic elements, returning empty list");
        }

        return elements;
    }

    // JSON helper methods
    private static string GetJsonString(System.Text.Json.JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            return prop.GetString() ?? "";
        }
        return "";
    }

    private static string? GetJsonStringOrNull(System.Text.Json.JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            var value = prop.GetString();
            return string.IsNullOrEmpty(value) ? null : value;
        }
        return null;
    }

    private static double GetJsonDouble(System.Text.Json.JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == System.Text.Json.JsonValueKind.Number)
        {
            return prop.GetDouble();
        }
        return 1.0;
    }

    private static bool GetJsonBool(System.Text.Json.JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind == System.Text.Json.JsonValueKind.True)
                return true;
            if (prop.ValueKind == System.Text.Json.JsonValueKind.False)
                return false;
        }
        return false;
    }

    private static int GetJsonInt(System.Text.Json.JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == System.Text.Json.JsonValueKind.Number)
        {
            return prop.GetInt32();
        }
        return 0;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            if (_browser != null)
            {
                await _browser.CloseAsync();
                _browser = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while closing Playwright browser during dispose");
        }

        try
        {
            _playwright?.Dispose();
            _playwright = null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while disposing Playwright");
        }

        // Ensure initialization flag is cleared so service can recover if used after dispose
        _isInitialized = false;

        _browserLock.Dispose();

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Represents raw web content fetched from a URL.
/// </summary>
public class RawWebContent
{
    public string Html { get; set; } = "";
    public byte[]? Screenshot { get; set; }
    public PageMetadata PageMetadata { get; set; } = new();
    public string AccessibilityTree { get; set; } = "";
    public List<InteractiveElement> InteractiveElements { get; set; } = new();
    public string Url { get; set; } = "";
    public string OperationId { get; set; } = "";
}
