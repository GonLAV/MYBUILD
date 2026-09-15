using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace Bolt.Automation.WorkerAgent.Services;

public sealed record BuildResult(bool Success, long DurationMs, string? Error = null);

public sealed class BuildService
{
    private readonly WorkerOptions _options;
    private readonly ILogger<BuildService> _logger;

    public BuildService(IOptions<WorkerOptions> options, ILogger<BuildService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<BuildResult> BuildAsync(bool restore = false, CancellationToken ct = default)
    {
        var projectPath = _options.TestProjectPath;
        _logger.LogInformation("Building {Project}{Restore}", projectPath, restore ? " (with restore)" : "");

        var sw = Stopwatch.StartNew();

        if (restore)
        {
            _logger.LogInformation("Restoring NuGet packages for {Solution}", _options.SolutionPath);
            var restorePsi = new ProcessStartInfo
            {
                FileName = "dotnet",
                // No --configfile: the repo-root NuGet.config resolves through NuGet's
                // standard hierarchy. A pinned config path must exist on every branch or
                // commit a work item can reference — the old ./.nuget/NuGet.config pin
                // broke every cross-branch restore after that file left the repo.
                Arguments = $"restore \"{_options.SolutionPath}\"",
                WorkingDirectory = _options.RepoRootPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var restoreCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            restoreCts.CancelAfter(TimeSpan.FromSeconds(_options.BuildTimeoutSeconds));

            using var restoreProcess = new Process { StartInfo = restorePsi };
            try
            {
                restoreProcess.Start();
            }
            catch (Win32Exception ex)
            {
                sw.Stop();
                var error = $"Failed to start 'dotnet restore' — is dotnet in PATH? {ex.Message}";
                _logger.LogError("{Error}", error);
                return new BuildResult(false, sw.ElapsedMilliseconds, error);
            }

            var restoreStdoutTask = restoreProcess.StandardOutput.ReadToEndAsync(restoreCts.Token);
            var restoreStderrTask = restoreProcess.StandardError.ReadToEndAsync(restoreCts.Token);

            try
            {
                await restoreProcess.WaitForExitAsync(restoreCts.Token);
            }
            catch (OperationCanceledException)
            {
                try { restoreProcess.Kill(entireProcessTree: true); } catch { }
                try { restoreProcess.WaitForExit(5000); } catch { }
                try { await Task.WhenAll(restoreStdoutTask, restoreStderrTask).WaitAsync(TimeSpan.FromSeconds(5)); } catch { }
                try { restoreProcess.StandardOutput.Close(); } catch { }
                try { restoreProcess.StandardError.Close(); } catch { }
                sw.Stop();
                _logger.LogError("NuGet restore timed out after {Duration}ms", sw.ElapsedMilliseconds);
                return new BuildResult(false, sw.ElapsedMilliseconds, "NuGet restore timed out");
            }

            var restoreStdout = await restoreStdoutTask;
            var restoreStderr = await restoreStderrTask;
            if (restoreProcess.ExitCode != 0)
            {
                // NuGet/MSBuild write most restore errors (NU1xxx, missing files) to stdout
                var detail = restoreStderr.Trim();
                if (string.IsNullOrEmpty(detail))
                    detail = restoreStdout.Trim();
                var error = $"dotnet restore failed (exit {restoreProcess.ExitCode}): {detail}";
                if (error.Length > 2000) error = error[..2000] + "... (truncated)";
                _logger.LogError("{Error}", error);
                sw.Stop();
                return new BuildResult(false, sw.ElapsedMilliseconds, error);
            }
            _logger.LogInformation("NuGet restore completed");
        }

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{projectPath}\" -c Release --no-restore",
            WorkingDirectory = _options.RepoRootPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(_options.BuildTimeoutSeconds));

        using var process = new Process { StartInfo = psi };

        try
        {
            process.Start();
        }
        catch (Win32Exception ex)
        {
            sw.Stop();
            var error = $"Failed to start 'dotnet build' — is dotnet in PATH? {ex.Message}";
            _logger.LogError("{Error}", error);
            return new BuildResult(false, sw.ElapsedMilliseconds, error);
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(cts.Token);

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); }
            catch { /* process may have already exited */ }

            try { process.WaitForExit(5000); }
            catch { /* best effort reap */ }

            // Drain pipe tasks so handles are released
            try { await Task.WhenAll(stdoutTask, stderrTask).WaitAsync(TimeSpan.FromSeconds(5)); }
            catch { /* best effort */ }

            // Explicitly close streams to release handles if orphaned tasks are still pending
            try { process.StandardOutput.Close(); } catch { }
            try { process.StandardError.Close(); } catch { }

            sw.Stop();
            var error = "Build timed out";
            _logger.LogError("{Error} after {Duration}ms", error, sw.ElapsedMilliseconds);
            return new BuildResult(false, sw.ElapsedMilliseconds, error);
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        sw.Stop();

        if (process.ExitCode != 0)
        {
            var error = stderr.Trim();
            if (string.IsNullOrEmpty(error))
                error = stdout.Trim();
            // Truncate long error messages
            if (error.Length > 2000)
                error = error[..2000] + "... (truncated)";

            _logger.LogError("Build failed (exit {Code}) in {Duration}ms: {Error}",
                process.ExitCode, sw.ElapsedMilliseconds, error);
            return new BuildResult(false, sw.ElapsedMilliseconds, error);
        }

        _logger.LogInformation("Build succeeded in {Duration}ms", sw.ElapsedMilliseconds);
        return new BuildResult(true, sw.ElapsedMilliseconds);
    }
}
