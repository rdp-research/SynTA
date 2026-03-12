using Microsoft.Playwright;
using System.Collections.Concurrent;

namespace SynTA.Services.AI;

/// <summary>
/// Configuration options for the browser pool.
/// </summary>
public class BrowserPoolOptions
{
    /// <summary>
    /// Maximum number of browser instances in the pool.
    /// </summary>
    public int MaxInstances { get; set; } = 3;

    /// <summary>
    /// Maximum number of concurrent contexts per browser.
    /// </summary>
    public int MaxContextsPerBrowser { get; set; } = 5;

    /// <summary>
    /// Timeout for acquiring a browser from the pool (milliseconds).
    /// </summary>
    public int AcquireTimeoutMs { get; set; } = 30000;
}

/// <summary>
/// Pool of Playwright browser instances for improved concurrency.
/// Manages multiple browser instances to handle concurrent scraping requests efficiently.
/// </summary>
public class BrowserPool : IAsyncDisposable
{
    private readonly ILogger<BrowserPool> _logger;
    private readonly BrowserPoolOptions _options;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly ConcurrentBag<BrowserInstance> _availableBrowsers = new();
    private readonly SemaphoreSlim _poolSemaphore;
    private IPlaywright? _playwright;
    private bool _isInitialized;
    private bool _disposed;
    private int _totalBrowsersCreated;

    public BrowserPool(ILogger<BrowserPool> logger, BrowserPoolOptions? options = null)
    {
        _logger = logger;
        _options = options ?? new BrowserPoolOptions();
        _poolSemaphore = new SemaphoreSlim(_options.MaxInstances, _options.MaxInstances);
    }

    /// <summary>
    /// Gets the current number of available browsers in the pool.
    /// </summary>
    public int AvailableBrowsers => _availableBrowsers.Count;

    /// <summary>
    /// Gets the total number of browsers created since initialization.
    /// </summary>
    public int TotalBrowsersCreated => _totalBrowsersCreated;

    /// <summary>
    /// Acquires a browser instance from the pool.
    /// Creates a new browser if none available and under max limit.
    /// </summary>
    public async Task<IBrowser> AcquireBrowserAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        var acquired = await _poolSemaphore.WaitAsync(_options.AcquireTimeoutMs, cancellationToken);
        if (!acquired)
        {
            _logger.LogWarning("Timeout acquiring browser from pool after {Timeout}ms", _options.AcquireTimeoutMs);
            throw new TimeoutException($"Could not acquire browser from pool within {_options.AcquireTimeoutMs}ms");
        }

        try
        {
            // Try to get an existing browser
            if (_availableBrowsers.TryTake(out var browserInstance))
            {
                _logger.LogDebug("Reusing existing browser instance - BrowserId: {BrowserId}", browserInstance.Id);
                return browserInstance.Browser;
            }

            // Create a new browser
            var browser = await CreateBrowserAsync(cancellationToken);
            return browser;
        }
        catch
        {
            _poolSemaphore.Release();
            throw;
        }
    }

    /// <summary>
    /// Returns a browser instance to the pool.
    /// </summary>
    public void ReleaseBrowser(IBrowser browser)
    {
        if (browser == null) return;

        _availableBrowsers.Add(new BrowserInstance
        {
            Browser = browser,
            Id = browser.GetHashCode()
        });

        _poolSemaphore.Release();
        _logger.LogDebug("Browser returned to pool - AvailableBrowsers: {Count}", _availableBrowsers.Count);
    }

    /// <summary>
    /// Initializes Playwright if not already initialized.
    /// </summary>
    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_isInitialized) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized) return;

            _logger.LogInformation("Initializing Playwright browser pool - MaxInstances: {Max}, MaxContexts: {MaxContexts}",
                _options.MaxInstances, _options.MaxContextsPerBrowser);

            _playwright = await Playwright.CreateAsync();
            _isInitialized = true;

            _logger.LogInformation("Playwright browser pool initialized successfully");
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>
    /// Creates a new browser instance.
    /// </summary>
    private async Task<IBrowser> CreateBrowserAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new browser instance - TotalCreated: {Total}", _totalBrowsersCreated + 1);

        var browser = await _playwright!.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = new[]
            {
                "--no-sandbox",
                "--disable-gpu",
                "--disable-dev-shm-usage",
                "--disable-setuid-sandbox",
                "--no-first-run",
                "--no-zygote"
            }
        });

        Interlocked.Increment(ref _totalBrowsersCreated);

        _logger.LogInformation("Browser instance created successfully - BrowserId: {BrowserId}, TotalCreated: {Total}",
            browser.GetHashCode(), _totalBrowsersCreated);

        return browser;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _logger.LogInformation("Disposing browser pool - TotalBrowsers: {Total}", _totalBrowsersCreated);

        // Close all browsers in the pool
        while (_availableBrowsers.TryTake(out var browserInstance))
        {
            try
            {
                await browserInstance.Browser.CloseAsync();
                _logger.LogDebug("Closed browser instance - BrowserId: {BrowserId}", browserInstance.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing browser instance - BrowserId: {BrowserId}", browserInstance.Id);
            }
        }

        _playwright?.Dispose();
        _playwright = null;
        _initLock.Dispose();
        _poolSemaphore.Dispose();

        _logger.LogInformation("Browser pool disposed successfully");

        GC.SuppressFinalize(this);
    }

    private class BrowserInstance
    {
        public IBrowser Browser { get; set; } = null!;
        public int Id { get; set; }
    }
}
