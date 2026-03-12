using SynTA.Models.Domain;

namespace SynTA.Services.Workflows;

/// <summary>
/// Interface for test generation workflow orchestration.
/// </summary>
public interface ITestGenerationWorkflowService
{
    /// <summary>
    /// Orchestrates the complete test generation workflow: Gherkin -> HTML Context -> Cypress.
    /// </summary>
    /// <param name="userStoryId">The ID of the user story to generate tests for</param>
    /// <param name="userId">The ID of the user requesting the generation</param>
    /// <param name="targetUrl">Optional target URL for HTML context extraction</param>
    /// <returns>Result containing the generated Cypress script ID and any errors</returns>
    Task<TestGenerationResult> GenerateCompleteTestSuiteAsync(int userStoryId, string userId, string? targetUrl = null);
}

/// <summary>
/// Result of a test generation workflow.
/// </summary>
public class TestGenerationResult
{
    /// <summary>
    /// Indicates whether the generation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The ID of the generated Gherkin scenario.
    /// </summary>
    public int? GherkinScenarioId { get; set; }

    /// <summary>
    /// The ID of the generated Cypress script.
    /// </summary>
    public int? CypressScriptId { get; set; }

    /// <summary>
    /// Error message if generation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Detailed information about the generation process for logging/debugging.
    /// </summary>
    public string? DetailedInfo { get; set; }
}
