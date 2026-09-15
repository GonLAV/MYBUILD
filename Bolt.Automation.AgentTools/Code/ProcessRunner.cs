using System.Diagnostics;
using System.Text;

namespace Bolt.Automation.AgentTools.Code;

public sealed record ProcessResult(bool Started, int ExitCode, string StdOut, string StdErr);

/// <summary>
/// Thin wrapper over <see cref="Process"/> for the external tools the code
/// commands shell out to (<c>git</c>, <c>rg</c>). Captures stdout/stderr,
/// enforces a timeout, and reports cleanly when the executable is absent
/// (so callers can fall back rather than crash).
/// </summary>
internal static class ProcessRunner
{
    public static ProcessResult Run(string fileName, IEnumerable<string> args, string? workingDir = null, int timeoutMs = 60_000)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDir ?? Directory.GetCurrentDirectory(),
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        Process? proc;
        try
        {
            proc = Process.Start(psi);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            return new ProcessResult(false, -1, string.Empty, $"{fileName} not found: {ex.Message}");
        }
        if (proc == null) return new ProcessResult(false, -1, string.Empty, $"{fileName} did not start.");

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        proc.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        proc.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        if (!proc.WaitForExit(timeoutMs))
        {
            try { proc.Kill(entireProcessTree: true); } catch { /* best effort */ }
            return new ProcessResult(true, -1, stdout.ToString(), $"Timed out after {timeoutMs}ms.");
        }
        proc.WaitForExit(); // flush async buffers

        return new ProcessResult(true, proc.ExitCode, stdout.ToString(), stderr.ToString());
    }
}
