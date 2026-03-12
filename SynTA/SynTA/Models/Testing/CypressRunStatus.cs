namespace SynTA.Models.Testing;

/// <summary>
/// Status of an asynchronous Cypress test execution.
/// </summary>
public sealed class CypressRunStatus
{
    public Guid RunId { get; set; }
    
    /// <summary>
    /// Current state of the run.
    /// </summary>
    public RunState State { get; set; }
    
    /// <summary>
    /// Console output accumulated so far.
    /// </summary>
    public string Logs { get; set; } = string.Empty;
    
    /// <summary>
    /// The final result (only populated when State is Completed).
    /// </summary>
    public CypressRunResult? Result { get; set; }
    
    /// <summary>
    /// Error message if the run failed to start or crashed.
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Indicates if the browser window has been detected.
    /// </summary>
    public bool IsBrowserLaunched { get; set; }

    /// <summary>
    /// Completes when the Cypress process has actually started.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public TaskCompletionSource<bool> ProcessStarted { get; } = new();
}

public enum RunState
{
    NotStarted = 0,
    Initializing = 1,
    Running = 2,
    Completed = 3,
    Failed = 4
}
