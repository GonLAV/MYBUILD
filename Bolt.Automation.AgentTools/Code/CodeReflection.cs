using Bolt.Automation.AgentTools.Commands;

namespace Bolt.Automation.AgentTools.Code;

/// <summary>
/// Shared pre-flight for the reflection-backed code commands (field-lookup,
/// flow-trace): refuse to run against a missing or stale FrontEnds.dll rather
/// than silently reflecting over an out-of-date build.
/// </summary>
internal static class CodeReflection
{
    public static bool PreflightFailed(ReflectionLoader loader, out Task<int> result)
    {
        if (!loader.DllExists)
        {
            result = CommandBase.EmitErrorAsync(
                "frontends_dll_missing",
                "Bolt.Automation.FrontEnds.dll not found. Build first: dotnet build Bolt.Automation.sln",
                exitCode: 2,
                detail: new { repo_root = loader.RepoRoot });
            return true;
        }

        if (loader.IsStale(out var newestSource))
        {
            result = CommandBase.EmitErrorAsync(
                "stale_build",
                "FrontEnds.dll is older than source changes — rebuild first: dotnet build Bolt.Automation.FrontEnds",
                exitCode: 2,
                detail: new
                {
                    dll = loader.DllPath,
                    dll_mtime = loader.DllMtimeUtc.ToString("o"),
                    newest_source = newestSource.ToString("o"),
                });
            return true;
        }

        result = Task.FromResult(0);
        return false;
    }
}
