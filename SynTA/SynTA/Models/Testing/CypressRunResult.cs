namespace SynTA.Models.Testing;

/// <summary>
/// Represents the result of a Cypress test run.
/// </summary>
public sealed class CypressRunResult
{
    /// <summary>
    /// Whether the test run completed successfully (all tests passed).
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Whether the Cypress process executed without errors.
    /// </summary>
    public bool ProcessCompleted { get; set; }

    /// <summary>
    /// Total number of tests executed.
    /// </summary>
    public int TotalTests { get; set; }

    /// <summary>
    /// Number of tests that passed.
    /// </summary>
    public int PassedTests { get; set; }

    /// <summary>
    /// Number of tests that failed.
    /// </summary>
    public int FailedTests { get; set; }

    /// <summary>
    /// Number of tests that were skipped.
    /// </summary>
    public int SkippedTests { get; set; }

    /// <summary>
    /// Number of tests that are pending (not yet implemented).
    /// </summary>
    public int PendingTests { get; set; }

    /// <summary>
    /// List of all tests executed (passed, failed, pending, skipped).
    /// </summary>
    public List<CypressTest> Tests { get; set; } = [];

    /// <summary>
    /// Helper to get only failed tests.
    /// </summary>
    public IEnumerable<CypressTest> Failures => Tests.Where(t => t.Status == CypressTestStatus.Failed);

    /// <summary>
    /// Raw console output from the Cypress process.
    /// </summary>
    public string RawOutput { get; set; } = string.Empty;

    /// <summary>
    /// Error message if the process failed to execute.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Duration of the test run.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Path to any generated video file.
    /// </summary>
    public string? VideoPath { get; set; }

    /// <summary>
    /// Paths to any generated screenshot files.
    /// </summary>
    public List<string> ScreenshotPaths { get; set; } = [];

    /// <summary>
    /// Creates a result indicating process execution failure.
    /// </summary>
    public static CypressRunResult ProcessError(string errorMessage)
    {
        return new CypressRunResult
        {
            Success = false,
            ProcessCompleted = false,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// Represents a single test failure with diagnostic information.
/// </summary>
/// <summary>
/// Represents a single test execution result.
/// </summary>
public sealed class CypressTest
{
    /// <summary>
    /// Status of the test result.
    /// </summary>
    public CypressTestStatus Status { get; set; }

    /// <summary>
    /// Name of the test.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Name of the test suite (describe block).
    /// </summary>
    public string SuiteName { get; set; } = string.Empty;

    /// <summary>
    /// Full title path (suite > test).
    /// </summary>
    public string FullTitle { get; set; } = string.Empty;

    /// <summary>
    /// The error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Stack trace if failed.
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// The selector that failed (if extractable).
    /// </summary>
    public string? FailedSelector { get; set; }

    /// <summary>
    /// Type of failure classification.
    /// </summary>
    public TestFailureType FailureType { get; set; }

    /// <summary>
    /// Duration of the test in milliseconds.
    /// </summary>
    public int DurationMs { get; set; }

    /// <summary>
    /// Path to screenshot relative to project.
    /// </summary>
    public string? ScreenshotPath { get; set; }
    
    /// <summary>
    /// The actual code of the test (if available).
    /// </summary>
    public string? Code { get; set; }
}

/// <summary>
/// Status of a Cypress test.
/// </summary>
public enum CypressTestStatus
{
    Unknown,
    Passed,
    Failed,
    Pending,
    Skipped
}

/// <summary>
/// Categories of test failures for analysis.
/// </summary>
public enum TestFailureType
{
    /// <summary>Unknown or unclassified failure.</summary>
    Unknown,

    /// <summary>Element selector not found in DOM.</summary>
    ElementNotFound,

    /// <summary>Element found but not visible.</summary>
    ElementNotVisible,

    /// <summary>Command timed out.</summary>
    Timeout,

    /// <summary>Assertion failed.</summary>
    AssertionFailed,

    /// <summary>Network request failed.</summary>
    NetworkError,

    /// <summary>JavaScript error on page.</summary>
    ScriptError,

    /// <summary>Element found but not interactable.</summary>
    ElementNotInteractable
}
