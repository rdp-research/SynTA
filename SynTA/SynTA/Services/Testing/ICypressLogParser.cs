using SynTA.Models.Testing;

namespace SynTA.Services.Testing;

/// <summary>
/// Service responsible for parsing Cypress console output and extracting structured information.
/// </summary>
public interface ICypressLogParser
{
    /// <summary>
    /// transforms a raw log line into a formatted string with ANSI colors if needed.
    /// </summary>
    string TransformLogLine(string line);

    /// <summary>
    /// Parses the accumulated output to determine the run result.
    /// </summary>
    CypressRunResult ParseConsoleOutput(string output, bool processExitedSuccessfully);

    /// <summary>
    /// Parses a Mocha JSON report string into a result object.
    /// </summary>
    CypressRunResult ParseMochaJsonReport(string json);

    /// <summary>
    /// Enriches a result with additional failure details extracted from console output.
    /// </summary>
    void EnrichFailuresWithConsoleOutput(CypressRunResult result, string consoleOutput);

    /// <summary>
    /// Determines if a log line indicates the browser has launched.
    /// </summary>
    bool IsBrowserLaunchDetected(string line);

    /// <summary>
    /// Determines if a log line indicates tests have started running.
    /// </summary>
    bool IsTestStartDetected(string line);
}
