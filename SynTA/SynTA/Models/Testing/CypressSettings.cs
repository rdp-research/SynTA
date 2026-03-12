namespace SynTA.Models.Testing;

/// <summary>
/// Configuration settings for the Cypress test runner integration.
/// </summary>
public sealed class CypressSettings
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Cypress";

    /// <summary>
    /// Path to Node.js executable. Default: "node" (uses PATH).
    /// </summary>
    public string NodePath { get; set; } = "node";

    /// <summary>
    /// Path to npx executable. Default: "npx" (uses PATH).
    /// </summary>
    public string NpxPath { get; set; } = "npx";

    /// <summary>
    /// Working directory containing cypress.config.ts/js.
    /// If null, a temporary directory will be created.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Maximum time in seconds to wait for a test run to complete.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Run tests in headless mode (no browser UI).
    /// </summary>
    public bool Headless { get; set; } = true;

    /// <summary>
    /// Browser to use for tests. Options: "chrome", "firefox", "edge", "electron".
    /// </summary>
    public string Browser { get; set; } = "electron";

    /// <summary>
    /// Enable video recording of test runs.
    /// </summary>
    public bool RecordVideo { get; set; } = false;

    /// <summary>
    /// Take screenshots on test failure.
    /// </summary>
    public bool ScreenshotsOnFailure { get; set; } = true;

    /// <summary>
    /// Base URL for the target application (can be overridden per test).
    /// </summary>
    public string? DefaultBaseUrl { get; set; }

    /// <summary>
    /// Viewport width for tests.
    /// </summary>
    public int ViewportWidth { get; set; } = 1920;

    /// <summary>
    /// Viewport height for tests.
    /// </summary>
    public int ViewportHeight { get; set; } = 1080;
}
