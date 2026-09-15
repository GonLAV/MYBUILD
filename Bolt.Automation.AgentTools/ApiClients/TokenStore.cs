using System.Diagnostics;

namespace Bolt.Automation.AgentTools.ApiClients;

/// <summary>
/// Persists the nexus-logger bearer token at
/// <c>%LOCALAPPDATA%\nexus-agent\token</c> with user-only file permissions.
/// Read order: <c>NEXUS_TC_TOKEN</c> env var (handy for CI), then the file.
/// </summary>
internal static class TokenStore
{
    private const string EnvVar = "NEXUS_TC_TOKEN";

    private static string Dir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "nexus-agent");

    private static string TokenPath => Path.Combine(Dir, "token");

    public static string? Read()
    {
        var env = Environment.GetEnvironmentVariable(EnvVar);
        if (!string.IsNullOrWhiteSpace(env)) return env.Trim();
        if (File.Exists(TokenPath))
        {
            var fromFile = File.ReadAllText(TokenPath).Trim();
            if (fromFile.Length > 0) return fromFile;
        }
        return null;
    }

    public static (string Path, string Acl) Save(string token)
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(TokenPath, token.Trim());

        var (ok, acl) = HardenPermissions(TokenPath);
        if (!ok)
        {
            // Fail closed: never leave a bearer token on disk with inherited/default
            // permissions while reporting success. Remove it and tell the user.
            try { File.Delete(TokenPath); } catch { /* best effort */ }
            throw new InvalidOperationException(
                $"Could not restrict permissions on the token file ({acl}); it was not stored. " +
                $"Fix ACLs on '{Dir}', or pass the token per-invocation via the {EnvVar} environment variable.");
        }
        return (TokenPath, acl);
    }

    private static (bool Ok, string Acl) HardenPermissions(string path)
    {
        if (OperatingSystem.IsWindows())
            return HardenWindows(path);

        try
        {
            // 0600 — owner read/write only.
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            return (true, "0600 (owner-only)");
        }
        catch (Exception ex)
        {
            return (false, $"chmod failed: {ex.Message}");
        }
    }

    private static (bool Ok, string Acl) HardenWindows(string path)
    {
        // icacls: reset inheritance (/inheritance:r) then grant only the current
        // user full control (/grant:r). Done for the user automatically so they
        // never have to run icacls by hand.
        var account = $"{Environment.UserDomainName}\\{Environment.UserName}";
        try
        {
            var psi = new ProcessStartInfo("icacls")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add(path);
            psi.ArgumentList.Add("/inheritance:r");
            psi.ArgumentList.Add("/grant:r");
            psi.ArgumentList.Add($"{account}:F");

            using var proc = Process.Start(psi);
            if (proc == null) return (false, "icacls did not start");
            proc.WaitForExit(10_000);
            return proc.ExitCode == 0
                ? (true, $"user-only ({account})")
                : (false, $"icacls exit {proc.ExitCode}");
        }
        catch (Exception ex)
        {
            return (false, $"icacls failed: {ex.Message}");
        }
    }
}
