using System.ComponentModel;
using System.Diagnostics;
using Bolt.Automation.Common.Reporting;
using Bolt.Automation.WorkerAgent.Models;
using Microsoft.Extensions.Options;

namespace Bolt.Automation.WorkerAgent.Services;

public sealed class TestExecutor
{
    private readonly WorkerOptions _options;
    private readonly ILogger<TestExecutor> _logger;

    public TestExecutor(IOptions<WorkerOptions> options, ILogger<TestExecutor> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SubmitResultRequest> ExecuteAsync(
        WorkItemDto workItem,
        int slotId,
        Dictionary<string, string>? jobEnvironment,
        CancellationToken ct)
    {
        var workItemDir = Path.Combine(_options.ResultsDirectory, workItem.Id);
        Directory.CreateDirectory(workItemDir);

        var trxFileName = "result.trx";
        var trxPath = Path.Combine(workItemDir, trxFileName);

        var sw = Stopwatch.StartNew();

        try
        {
            TrxResult trxResult;

            List<string> stderrLines;
            try
            {
                stderrLines = await RunDotNetTest(workItem, slotId, workItemDir, trxFileName, jobEnvironment, ct);
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                // Timeout — still salvage TRX if dotnet test produced one before being killed
                if (File.Exists(trxPath))
                {
                    trxResult = TrxParser.Parse(trxPath);
                    trxResult = trxResult with { ErrorSummary = $"Test execution timed out. {trxResult.ErrorSummary}".TrimEnd() };
                }
                else
                {
                    trxResult = new TrxResult
                    {
                        Total = 1,
                        Failed = 1,
                        ErrorSummary = "Test execution timed out",
                    };
                }

                return BuildResult(workItem, sw.Elapsed.TotalMilliseconds, trxResult);
            }

            sw.Stop();

            // Parse TRX regardless of exit code (exit code 1 = test failures, which is expected)
            if (File.Exists(trxPath))
            {
                trxResult = TrxParser.Parse(trxPath);
            }
            else
            {
                trxResult = new TrxResult
                {
                    Total = 1,
                    Failed = 1,
                    ErrorSummary = "No TRX file produced — process may have crashed",
                };
            }

            // Detect NUnit PropertyFilter crash (int-typed TestCaseId causes InvalidCastException)
            if (trxResult.Total <= 1 && trxResult.Failed >= 1 && trxResult.Passed == 0)
            {
                var stderr = string.Join("\n", stderrLines);
                if (stderr.Contains("InvalidCastException") && stderr.Contains("PropertyFilter"))
                {
                    trxResult = trxResult with
                    {
                        ErrorSummary = "NUnit PropertyFilter crash: a test in the assembly uses " +
                            ".SetProperty(\"TestCaseId\", intValue) instead of string. " +
                            "This crashes ALL test execution. " +
                            "Fix: change to .SetProperty(\"TestCaseId\", \"stringValue\")."
                    };
                }
            }

            return BuildResult(
                workItem,
                trxResult.DurationMs > 0 ? trxResult.DurationMs : sw.Elapsed.TotalMilliseconds,
                trxResult);
        }
        finally
        {
            try { Directory.Delete(workItemDir, recursive: true); }
            catch { /* best effort cleanup */ }
        }
    }

    private async Task<List<string>> RunDotNetTest(
        WorkItemDto workItem,
        int slotId,
        string resultsDir,
        string trxFileName,
        Dictionary<string, string>? jobEnvironment,
        CancellationToken ct)
    {
        var stderrLines = new List<string>();
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = string.Join(" ",
                "test",
                $"\"{_options.TestProjectPath}\"",
                $"--filter \"{(workItem.TestCaseId.HasValue ? $"TestCaseId={workItem.TestCaseId.Value}" : $"FullyQualifiedName={workItem.FullyQualifiedName}")}\"",
                $"--logger \"trx;LogFileName={trxFileName}\"",
                "--no-build",
                "--no-restore",
                "-c Release",
                "--verbosity minimal",
                $"--results-directory \"{resultsDir}\""),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        // Inject job environment variables
        if (jobEnvironment != null)
        {
            foreach (var (key, value) in jobEnvironment)
            {
                psi.Environment[key] = value;
            }
        }

        // Inject work-item-level env var (set after jobEnvironment so it cannot be overridden)
        psi.Environment["ORCHESTRATOR_WORK_ITEM_ID"] = workItem.Id;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(_options.TestTimeoutSeconds));

        using var process = new Process { StartInfo = psi };

        try
        {
            process.Start();
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to start 'dotnet test' — is dotnet in PATH? {ex.Message}", ex);
        }

        // Read stdout/stderr line by line for live streaming
        var stdoutTask = ReadLinesAsync(
            process.StandardOutput,
            "TestRunner.stdout",
            workItem,
            slotId,
            cts.Token);

        var stderrTask = ReadLinesAsync(
            process.StandardError,
            "TestRunner.stderr",
            workItem,
            slotId,
            cts.Token,
            stderrLines);

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Timeout — kill the process tree
            try { process.Kill(entireProcessTree: true); }
            catch { /* process may have already exited */ }

            try { process.WaitForExit(5000); }
            catch { /* best effort reap */ }

            // Drain pipe tasks so handles are released
            try { await Task.WhenAll(stdoutTask, stderrTask).WaitAsync(TimeSpan.FromSeconds(5)); }
            catch { /* best effort */ }

            throw;
        }

        // Ensure streams are fully drained
        await Task.WhenAll(stdoutTask, stderrTask);

        return stderrLines;
    }

    private async Task ReadLinesAsync(
        StreamReader reader,
        string category,
        WorkItemDto workItem,
        int slotId,
        CancellationToken ct,
        List<string>? collectLines = null)
    {
        // Short ID for compact console prefix — first 8 chars of the work item ID
        var shortId = workItem.Id.Length > 8 ? workItem.Id[..8] : workItem.Id;
        var prefix = $"[S{slotId}|{shortId}]";
        var isStderr = category.EndsWith("stderr");

        try
        {
            while (await reader.ReadLineAsync(ct) is { } line)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    collectLines?.Add(line);

                    using (_logger.BeginScope(new Dictionary<string, object>
                    {
                        ["WorkItemId"] = workItem.Id,
                        ["JobId"] = workItem.JobId,
                        ["SlotId"] = slotId,
                    }))
                    {
                        if (isStderr)
                            _logger.LogWarning("{Prefix} {Line}", prefix, line);
                        else
                            _logger.LogInformation("{Prefix} {Line}", prefix, line);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected on timeout
        }
    }

    private SubmitResultRequest BuildResult(WorkItemDto workItem, double durationMs, TrxResult trx)
    {
        return new SubmitResultRequest(
            WorkItemId: workItem.Id,
            JobId: workItem.JobId,
            WorkerId: _options.WorkerId,
            Passed: trx.Failed == 0 && trx.Total > 0,
            Duration: durationMs,
            TotalTests: trx.Total,
            PassedTests: trx.Passed,
            FailedTests: trx.Failed,
            SkippedTests: trx.Skipped,
            ErrorSummary: trx.ErrorSummary
        );
    }
}
