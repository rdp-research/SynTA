using System.Text.Json;
using System.Text.RegularExpressions;
using SynTA.Models.Testing;

namespace SynTA.Services.Testing;

public partial class CypressLogParser : ICypressLogParser
{
    // ANSI color codes
    private const string ANSI_RED = "\x1b[31m";
    private const string ANSI_GREEN = "\x1b[32m";
    private const string ANSI_YELLOW = "\x1b[33m";
    private const string ANSI_RESET = "\x1b[0m";

    public string TransformLogLine(string line)
    {
        // Transform failed lines: "  1) Scenario..." -> "  ✖ Scenario..." (red)
        var failedMatch = FailedLineRegex().Match(line);
        if (failedMatch.Success)
        {
            var indent = failedMatch.Groups[1].Value;
            var rest = line.Substring(failedMatch.Length);
            return $"{indent}{ANSI_RED}✖{ANSI_RESET} {rest}";
        }

        // Transform pending lines: "  - Scenario..." -> "  ○ Scenario..." (yellow)
        var pendingMatch = PendingLineRegex().Match(line);
        if (pendingMatch.Success)
        {
            var indent = pendingMatch.Groups[1].Value;
            var rest = line.Substring(pendingMatch.Length);
            return $"{indent}{ANSI_YELLOW}○{ANSI_RESET} {rest}";
        }

        // Transform passed lines: ensure green color
        var passedMatch = PassedLineRegex().Match(line);
        if (passedMatch.Success)
        {
            var indent = passedMatch.Groups[1].Value;
            var rest = line.Substring(passedMatch.Length);
            return $"{indent}{ANSI_GREEN}✓{ANSI_RESET} {rest}";
        }

        return line;
    }

    public bool IsBrowserLaunchDetected(string line)
    {
        // Detects common Cypress output indicating browser launch
        return line.Contains("Cypress requires a browser") || // Sometimes printed before
               line.Contains("Opening Cypress") ||
               Regex.IsMatch(line, @"Browser:\s+.*\(.*\)");
    }

    public bool IsTestStartDetected(string line)
    {
        // Detects when specs actually start running
        return line.Contains("Running:") || 
               line.Contains("Specs:") ||
               Regex.IsMatch(line, @"Running:\s+.*\.cy\.(ts|js)");
    }

    public CypressRunResult ParseConsoleOutput(string output, bool processExitedSuccessfully)
    {
        var result = new CypressRunResult
        {
            ProcessCompleted = true,
            RawOutput = output
        };

        // Parse summary line: "X passing (Xs)" "Y failing"
        var passingMatch = PassingRegex().Match(output);
        if (passingMatch.Success)
        {
            result.PassedTests = int.Parse(passingMatch.Groups[1].Value);
        }
        else
        {
            // Try table format
            var match = TablePassingRegex().Match(output);
            if (match.Success) result.PassedTests = int.Parse(match.Groups[1].Value);
        }

        var failingMatch = FailingRegex().Match(output);
        if (failingMatch.Success)
        {
            result.FailedTests = int.Parse(failingMatch.Groups[1].Value);
        }
        else
        {
             // Try table format
            var match = TableFailingRegex().Match(output);
            if (match.Success) result.FailedTests = int.Parse(match.Groups[1].Value);
        }

        var pendingMatch = PendingRegex().Match(output);
        if (pendingMatch.Success)
        {
            result.PendingTests = int.Parse(pendingMatch.Groups[1].Value);
        }
        else
        {
             // Try table format
            var match = TablePendingRegex().Match(output);
            if (match.Success) result.PendingTests = int.Parse(match.Groups[1].Value);
        }

        result.TotalTests = result.PassedTests + result.FailedTests + result.PendingTests;
        
        // If still 0, try Total Tests parsing from table
        if (result.TotalTests == 0)
        {
             var match = TableTestsRegex().Match(output);
             if (match.Success) result.TotalTests = int.Parse(match.Groups[1].Value);
        }
        
        // Detect compilation errors or zero-test runs
        if (result.TotalTests == 0)
        {
            result.Success = false;
            
            // Try to extract error message
            var errorMatch = Regex.Match(output, @"Error: (.*?)(\n|$)", RegexOptions.Multiline);
            var errorMessage = errorMatch.Success ? errorMatch.Groups[1].Value.Trim() : "Test run failed with no tests executed (likely compilation error)";
            
            if (output.Contains("Webpack Compilation Error"))
            {
                errorMessage = "Webpack Compilation Error: Check tsconfig.json or dependencies.";
                var tsError = Regex.Match(output, @"TS\d+: (.*?)(\n|$)", RegexOptions.Multiline);
                if (tsError.Success)
                {
                    errorMessage += " " + tsError.Groups[0].Value.Trim();
                }
            }

            result.Tests.Add(new CypressTest
            {
                Title = "Compilation/Runtime Error",
                ErrorMessage = errorMessage,
                FailureType = TestFailureType.ScriptError,
                Status = CypressTestStatus.Failed
            });
        }
        else
        {
            result.Success = result.FailedTests == 0 && processExitedSuccessfully;
        }

        return result;
    }

    public CypressRunResult ParseMochaJsonReport(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var result = new CypressRunResult
        {
            ProcessCompleted = true
        };

        if (root.TryGetProperty("stats", out var stats))
        {
            result.TotalTests = stats.TryGetProperty("tests", out var tests) ? tests.GetInt32() : 0;
            result.PassedTests = stats.TryGetProperty("passes", out var passes) ? passes.GetInt32() : 0;
            result.FailedTests = stats.TryGetProperty("failures", out var failures) ? failures.GetInt32() : 0;
            result.PendingTests = stats.TryGetProperty("pending", out var pending) ? pending.GetInt32() : 0;
            result.SkippedTests = stats.TryGetProperty("skipped", out var skipped) ? skipped.GetInt32() : 0;
        }

        result.Success = result.FailedTests == 0 && result.TotalTests > 0;

        // Recursively extract all tests
        if (root.TryGetProperty("results", out var resultsArray))
        {
            foreach (var suiteResult in resultsArray.EnumerateArray())
            {
                ExtractTestsFromSuite(suiteResult, result.Tests);
            }
        }
        else if (root.TryGetProperty("suites", out var suites)) // Sometimes at root?
        {
             // Fallback
             ExtractTestsFromSuite(root, result.Tests);
        }

        return result;
    }

    public void EnrichFailuresWithConsoleOutput(CypressRunResult result, string consoleOutput)
    {
        // Enrich failed tests if we couldn't get details from JSON
        foreach (var test in result.Tests)
        {
            if (test.Status == CypressTestStatus.Failed)
            {
                if (test.FailureType == TestFailureType.Unknown && !string.IsNullOrEmpty(test.ErrorMessage))
                {
                    test.FailureType = ClassifyFailure(test.ErrorMessage);
                }
                if (string.IsNullOrEmpty(test.FailedSelector) && !string.IsNullOrEmpty(test.ErrorMessage))
                {
                    test.FailedSelector = ExtractSelector(test.ErrorMessage);
                }
            }
        }
    }

    private void ExtractTestsFromSuite(JsonElement suite, List<CypressTest> tests)
    {
        if (suite.TryGetProperty("tests", out var testsArray))
        {
            foreach (var test in testsArray.EnumerateArray())
            {
                var rawTitle = test.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "";
                var cleanTitle = Regex.Replace(rawTitle, @"^\d+\)\s*", ""); // Strip "1) " prefix if present

                var cypressTest = new CypressTest
                {
                   Title = cleanTitle,
                   FullTitle = test.TryGetProperty("fullTitle", out var fullTitle) ? fullTitle.GetString() ?? "" : "",
                   DurationMs = test.TryGetProperty("duration", out var duration) ? duration.GetInt32() : 0,
                   Code = test.TryGetProperty("code", out var code) ? code.GetString() : null
                };

                // Determine status
                if (test.TryGetProperty("state", out var stateProp))
                {
                    var state = stateProp.GetString()?.ToLowerInvariant();
                    cypressTest.Status = state switch
                    {
                        "passed" => CypressTestStatus.Passed,
                        "failed" => CypressTestStatus.Failed,
                        "pending" => CypressTestStatus.Pending,
                        "skipped" => CypressTestStatus.Skipped,
                        _ => CypressTestStatus.Unknown
                    };
                }
                
                // If failed, get error info
                if (test.TryGetProperty("err", out var err) && err.ValueKind == JsonValueKind.Object)
                {
                    cypressTest.ErrorMessage = err.TryGetProperty("message", out var msg) ? msg.GetString() : null;
                    cypressTest.StackTrace = err.TryGetProperty("stack", out var stack) ? stack.GetString() : null;
                    if (cypressTest.ErrorMessage != null)
                    {
                        cypressTest.FailureType = ClassifyFailure(cypressTest.ErrorMessage);
                        cypressTest.FailedSelector = ExtractSelector(cypressTest.ErrorMessage);
                    }
                }
                
                tests.Add(cypressTest);
            }
        }

        if (suite.TryGetProperty("suites", out var nestedSuites))
        {
            foreach (var nested in nestedSuites.EnumerateArray())
            {
                ExtractTestsFromSuite(nested, tests);
            }
        }
    }

    private static TestFailureType ClassifyFailure(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            return TestFailureType.Unknown;

        var msg = errorMessage.ToLowerInvariant();

        if (msg.Contains("expected to find element") || msg.Contains("never found it"))
            return TestFailureType.ElementNotFound;

        if (msg.Contains("not visible") || msg.Contains("is not visible"))
            return TestFailureType.ElementNotVisible;

        if (msg.Contains("timed out") || msg.Contains("timeout"))
            return TestFailureType.Timeout;

        if (msg.Contains("expected") && (msg.Contains("to be") || msg.Contains("to equal") || msg.Contains("to have")))
            return TestFailureType.AssertionFailed;

        if (msg.Contains("network") || msg.Contains("xhr") || msg.Contains("fetch"))
            return TestFailureType.NetworkError;

        if (msg.Contains("script error") || msg.Contains("javascript error"))
            return TestFailureType.ScriptError;

        if (msg.Contains("not interactable") || msg.Contains("covered by"))
            return TestFailureType.ElementNotInteractable;

        return TestFailureType.Unknown;
    }

    private static string? ExtractSelector(string errorMessage)
    {
        var selectorPatterns = new[]
        {
            @"Expected to find element:\s*([^,]+)",
            @"cy\.get\(['""]([^'""]+)['""]\)",
            @"cy\.contains\(['""]([^'""]+)['""]\)",
            @"\[data-testid=['""]([^'""]+)['""]\]",
            @"#([a-zA-Z][a-zA-Z0-9_-]+)"
        };

        foreach (var pattern in selectorPatterns)
        {
            var match = Regex.Match(errorMessage, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
        }

        return null;
    }

    // Regex Definitions
    [GeneratedRegex(@"(\d+) passing")]
    private partial Regex PassingRegex();

    [GeneratedRegex(@"(\d+) failing")]
    private partial Regex FailingRegex();

    [GeneratedRegex(@"(\d+) pending")]
    private partial Regex PendingRegex();

    [GeneratedRegex(@"Tests:\s+(\d+)")]
    private partial Regex TableTestsRegex();

    [GeneratedRegex(@"Passing:\s+(\d+)")]
    private partial Regex TablePassingRegex();

    [GeneratedRegex(@"Failing:\s+(\d+)")]
    private partial Regex TableFailingRegex();

    [GeneratedRegex(@"Pending:\s+(\d+)")]
    private partial Regex TablePendingRegex();

    [GeneratedRegex(@"^(\s*)(\d+)\)\s+")]
    private partial Regex FailedLineRegex();

    [GeneratedRegex(@"^(\s*)[-–]\s+")]
    private partial Regex PendingLineRegex();

    [GeneratedRegex(@"^(\s*)[✓✔]\s+")]
    private partial Regex PassedLineRegex();
}
