using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options;
using SynTA.Models.Testing;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace SynTA.Services.Testing;

/// <summary>
/// Service for running Cypress tests programmatically via CLI.
/// </summary>
public sealed class CypressRunnerService : ICypressRunnerService, IDisposable
{
    private readonly ILogger<CypressRunnerService> _logger;
    private readonly CypressSettings _settings;
    private readonly ICypressLogParser _logParser;
    private readonly string _workingDirectory;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _isInitialized;
    private readonly bool _isWindows;

    // Track active and recent runs
    private readonly ConcurrentDictionary<Guid, CypressRunStatus> _activeRuns = new();

    public CypressRunnerService(
        IOptions<CypressSettings> settings,
        ILogger<CypressRunnerService> logger,
        ICypressLogParser logParser)
    {
        _settings = settings.Value;
        _logger = logger;
        _logParser = logParser;
        _workingDirectory = _settings.WorkingDirectory ?? Path.Combine(Path.GetTempPath(), "SynTACypress");
        _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    public Task<Guid> StartTestRunFileAsync(string testFilePath, string baseUrl, CancellationToken cancellationToken = default)
    {
        var runId = Guid.NewGuid();
        var status = new CypressRunStatus { RunId = runId, State = RunState.NotStarted };
        _activeRuns.TryAdd(runId, status);

        // Fire and forget application, but wrapped for safety
        _ = RunInBackgroundAsync(runId, async ct => 
        {
            await EnsureInitializedAsync(ct);

            var fileName = Path.GetFileName(testFilePath);
            var destPath = Path.Combine(_workingDirectory, "cypress", "e2e", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.Copy(testFilePath, destPath, true);
            
            return await ExecuteCypressRunAsync(fileName, baseUrl, runId, ct);
        }, cancellationToken);

        return Task.FromResult(runId);
    }

    public Task<Guid> StartTestRunContentAsync(string testContent, string baseUrl, string? testFileName = null, CancellationToken cancellationToken = default)
    {
        var runId = Guid.NewGuid();
        var status = new CypressRunStatus { RunId = runId, State = RunState.NotStarted };
        _activeRuns.TryAdd(runId, status);

        _ = RunInBackgroundAsync(runId, async ct => 
        {
             await EnsureInitializedAsync(ct);

             testFileName ??= $"test-{Guid.NewGuid():N}.cy.ts";
             if (!testFileName.EndsWith(".cy.ts") && !testFileName.EndsWith(".cy.js"))
                 testFileName += ".cy.ts";

             var filePath = Path.Combine(_workingDirectory, "cypress", "e2e", testFileName);
             Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
             await File.WriteAllTextAsync(filePath, testContent, ct);
             
             return await ExecuteCypressRunAsync(testFileName, baseUrl, runId, ct);
        }, cancellationToken);

        return Task.FromResult(runId);
    }

    public Task<CypressRunStatus?> GetRunStatusAsync(Guid runId)
    {
        _activeRuns.TryGetValue(runId, out var status);
        return Task.FromResult(status);
    }

    private async Task RunInBackgroundAsync(Guid runId, Func<CancellationToken, Task<CypressRunResult>> action, CancellationToken cancellationToken)
    {
        if (!_activeRuns.TryGetValue(runId, out var status)) return;

        // Note: State stays as NotStarted until process.Start() succeeds in ExecuteCypressRunAsync
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Create a linked token that cancels if app shuts down, but we also set a safety timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds + 60)); 

            var result = await action(cts.Token);
            
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            
            status.Result = result;
            status.State = RunState.Completed;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Background test run {RunId} failed", runId);
            status.State = RunState.Failed;
            status.ErrorMessage = ex.Message;
            status.Logs += $"\n[System Error] {ex.Message}\n{ex.StackTrace}";
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateCypressInstallationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var (fileName, args) = GetCommandLine(_settings.NpxPath, "cypress --version");
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return false;

            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cypress installation validation failed");
            return false;
        }
    }

    /// <summary>
    /// Gets the correct command line for the current OS.
    /// On Windows, wraps commands in cmd.exe /c to ensure PATH is resolved.
    /// </summary>
    private (string FileName, string Arguments) GetCommandLine(string command, string arguments)
    {
        if (_isWindows)
        {
            // On Windows, cmd.exe /c is needed to resolve PATH for commands like npm, npx
            return ("cmd.exe", $"/c {command} {arguments}");
        }
        else
        {
            // On Unix-like systems, commands are typically available directly
            return (command, arguments);
        }
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_isInitialized) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized) return;

            _logger.LogInformation("Initializing Cypress working directory: {Dir}", _workingDirectory);

            // Create directory structure
            Directory.CreateDirectory(_workingDirectory);
            Directory.CreateDirectory(Path.Combine(_workingDirectory, "cypress", "e2e"));
            Directory.CreateDirectory(Path.Combine(_workingDirectory, "cypress", "support"));

            // Create minimal cypress.config.ts
            var configPath = Path.Combine(_workingDirectory, "cypress.config.ts");
            if (!File.Exists(configPath))
            {
                var configContent = GenerateCypressConfig();
                await File.WriteAllTextAsync(configPath, configContent, cancellationToken);
            }

            // Create support file
            var supportPath = Path.Combine(_workingDirectory, "cypress", "support", "e2e.ts");
            if (!File.Exists(supportPath))
            {
                await File.WriteAllTextAsync(supportPath, "// Support file for Cypress\n", cancellationToken);
            }

            // Create tsconfig.json
            var tsConfigPath = Path.Combine(_workingDirectory, "tsconfig.json");
            if (!File.Exists(tsConfigPath))
            {
                var tsConfigContent = """
                {
                  "compilerOptions": {
                    "target": "es5",
                    "lib": ["es5", "dom"],
                    "types": ["cypress", "node"],
                    "esModuleInterop": true
                  },
                  "include": ["cypress/**/*.ts", "cypress/**/*.js"]
                }
                """;
                await File.WriteAllTextAsync(tsConfigPath, tsConfigContent, cancellationToken);
            }

            // Create/Update package.json
            var packagePath = Path.Combine(_workingDirectory, "package.json");
            var packageJson = """
            {
              "name": "synta-cypress-runner",
              "version": "1.0.0",
              "private": true,
              "description": "Temporary Cypress runner for SynTA",
              "scripts": {
                "cy:run": "cypress run"
              },
              "devDependencies": {
                "cypress": "^13.0.0",
                "mochawesome": "^7.1.3",
                "mochawesome-merge": "^4.3.0",
                "mochawesome-report-generator": "^6.2.0",
                "typescript": "^5.0.0"
              }
            }
            """;
            await File.WriteAllTextAsync(packagePath, packageJson, cancellationToken);

            // Install Cypress if needed
            await InstallCypressIfNeededAsync(cancellationToken);

            _isInitialized = true;
            _logger.LogInformation("Cypress working directory initialized successfully");
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task InstallCypressIfNeededAsync(CancellationToken cancellationToken)
    {
        var cypressPath = Path.Combine(_workingDirectory, "node_modules", ".bin", "cypress");
        var cypressCmdPath = cypressPath + ".cmd"; // Windows
        var mochawesomePath = Path.Combine(_workingDirectory, "node_modules", "mochawesome");

        if ((File.Exists(cypressPath) || File.Exists(cypressCmdPath)) && Directory.Exists(mochawesomePath))
        {
            _logger.LogDebug("Cypress and dependencies all installed in working directory");
            return;
        }

        _logger.LogInformation("Installing Cypress dependencies...");

        var (fileName, args) = GetCommandLine("npm", "install");
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            WorkingDirectory = _workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start npm install process. Ensure Node.js and npm are installed and in your PATH.");
        }

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            _logger.LogError("npm install failed - Exit: {ExitCode}, StdErr: {StdErr}, StdOut: {StdOut}", 
                process.ExitCode, error, output);
            throw new InvalidOperationException($"npm install failed: {error}");
        }

        _logger.LogInformation("Cypress dependencies installed successfully");
    }

    private string GenerateCypressConfig()
    {
        return $$"""
            import { defineConfig } from 'cypress';

            export default defineConfig({
              e2e: {
                setupNodeEvents(on, config) {
                  // Node event listeners
                },
                supportFile: 'cypress/support/e2e.ts',
                specPattern: 'cypress/e2e/**/*.cy.{js,ts}',
                viewportWidth: {{_settings.ViewportWidth}},
                viewportHeight: {{_settings.ViewportHeight}},
                video: {{_settings.RecordVideo.ToString().ToLowerInvariant()}},
                screenshotOnRunFailure: {{_settings.ScreenshotsOnFailure.ToString().ToLowerInvariant()}},
                defaultCommandTimeout: 10000,
                pageLoadTimeout: 60000,
                requestTimeout: 10000,
                responseTimeout: 10000,
              },
            });
            """;
    }

    private async Task<CypressRunResult> ExecuteCypressRunAsync(
        string testFileName,
        string baseUrl,
        Guid runId,
        CancellationToken cancellationToken)
    {
        // Get status object to stream logs
        _activeRuns.TryGetValue(runId, out var status);
        string operationId = runId.ToString("N");
        // Clean previous results
        var resultsDir = Path.Combine(_workingDirectory, "cypress", "results");
        if (Directory.Exists(resultsDir))
        {
            Directory.Delete(resultsDir, true);
        }
        Directory.CreateDirectory(resultsDir);

        var arguments = new StringBuilder();
        arguments.Append("cypress run");
        arguments.Append($" --spec \"cypress/e2e/{testFileName}\"");
        arguments.Append($" --config baseUrl={baseUrl}");
        arguments.Append($" --browser {_settings.Browser}");
        arguments.Append(" --reporter mochawesome");
        
        if (_settings.Headless)
        {
            arguments.Append(" --headless");
        }
        else
        {
            arguments.Append(" --headed");
            arguments.Append(" --no-exit"); // Keep browser open for analysis
        }

        _logger.LogDebug("[{OperationId}] Executing: npx {Args}", operationId, arguments);

        var (fileName, cmdArgs) = GetCommandLine(_settings.NpxPath, arguments.ToString());
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = cmdArgs,
            WorkingDirectory = _workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        
        // Force Cypress/Chalk to output colors
        psi.EnvironmentVariables["FORCE_COLOR"] = "1";

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        using var process = new Process { StartInfo = psi };
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) 
            {
                // Use parser to transform logs and detect state changes
                var transformedLine = _logParser.TransformLogLine(e.Data);
                outputBuilder.AppendLine(transformedLine);
                if (status != null) 
                {
                    status.Logs = outputBuilder.ToString();
                    
                    // Check for browser detection
                    if (_logParser.IsBrowserLaunchDetected(e.Data))
                    {
                        status.IsBrowserLaunched = true;
                    }
                    
                    // Check for test start detection - THIS triggers the running state
                    if (status.State == RunState.Initializing && _logParser.IsTestStartDetected(e.Data))
                    {
                        status.State = RunState.Running;
                    }
                }
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
                if (status != null) status.Logs = outputBuilder.ToString() + "\nERR:\n" + errorBuilder.ToString();
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        // Signal that the Cypress process has started, but mostly likely just "Initializing"
        if (status != null)
        {
            status.State = RunState.Initializing;
            status.ProcessStarted.TrySetResult(true);
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
            throw;
        }

        var rawOutput = outputBuilder.ToString();
        var rawError = errorBuilder.ToString();

        // 1. Try processing JSON report
        var jsonFiles = Directory.Exists(resultsDir) ? Directory.GetFiles(resultsDir, "*.json") : Array.Empty<string>();
        
        if (jsonFiles.Any())
        {
            try 
            {
                var jsonContent = await File.ReadAllTextAsync(jsonFiles.First(), cancellationToken);
                var result = _logParser.ParseMochaJsonReport(jsonContent);
                result.ProcessCompleted = true;
                result.RawOutput = rawOutput;
                
                // If we have failures, try to extract more details from console output
                if (!result.Success || result.FailedTests > 0)
                {
                    _logParser.EnrichFailuresWithConsoleOutput(result, rawOutput);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse Cypress JSON report");
            }
            finally
            {
                foreach (var file in jsonFiles)
                {
                    try { File.Delete(file); } catch { /* ignore */ }
                }
            }
        }

        // Fall back to parsing console output
        return _logParser.ParseConsoleOutput(rawOutput, process.ExitCode == 0);
    }

    public void Dispose()
    {
        _initLock.Dispose();
    }
}

