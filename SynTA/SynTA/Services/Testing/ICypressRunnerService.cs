using SynTA.Models.Testing;

namespace SynTA.Services.Testing;

/// <summary>
/// Service interface for running Cypress tests programmatically.
/// </summary>
public interface ICypressRunnerService
{
    /// <summary>
    /// Runs a Cypress test file.
    /// </summary>
    /// <param name="testFilePath">Absolute path to the .cy.ts file.</param>
    /// <param name="baseUrl">Base URL for the application under test.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Test run results.</returns>
    /// <summary>
    /// Starts a Cypress test run from a file in the background.
    /// </summary>
    /// <param name="testFilePath">Absolute path to the .cy.ts file.</param>
    /// <param name="baseUrl">Base URL for the application under test.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Run ID.</returns>
    Task<Guid> StartTestRunFileAsync(
        string testFilePath,
        string baseUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a Cypress test run from content in the background.
    /// </summary>
    /// <param name="testContent">The Cypress test script content.</param>
    /// <param name="baseUrl">Base URL for the application under test.</param>
    /// <param name="testFileName">Optional file name for the test (for reporting).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Run ID.</returns>
    Task<Guid> StartTestRunContentAsync(
        string testContent,
        string baseUrl,
        string? testFileName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a running or completed test.
    /// </summary>
    Task<CypressRunStatus?> GetRunStatusAsync(Guid runId);

    /// <summary>
    /// Validates that Cypress is installed and can execute.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if Cypress is available and working.</returns>
    Task<bool> ValidateCypressInstallationAsync(CancellationToken cancellationToken = default);
}
