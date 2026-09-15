using System.Text.Json;
using Bolt.Automation.AgentTools.Code;
using Bolt.Automation.AgentTools.Commands.Secrets;

namespace Bolt.Automation.AgentTools.Commands.Doctor;

/// <summary>
/// One-command environment bootstrap/diagnosis for non-dev (QA) machines:
/// checks everything a nexus-qa-assist session needs — repo, build freshness,
/// Playwright browsers, AWS CLI + SSO profile, secrets bundle, BOLT_SECRETS_PATH,
/// the checked-in Claude permissions allowlist — and reports each as
/// ok / fixable / needs-human, with the exact fix. <c>--fix</c> applies the
/// non-interactive fixes (Playwright install, user-level BOLT_SECRETS_PATH).
///
/// Also maintains the LAST-KNOWN-GOOD build record (broken-develop resilience):
/// when repo + build checks pass, the current commit is recorded to
/// <c>%LOCALAPPDATA%\nexus-agent\last-good.json</c>; when today's develop is broken,
/// the skill checks out that commit (detached) so the QA keeps working.
/// </summary>
internal static class DoctorCommand
{
    private sealed record Check(string Name, bool Ok, string Detail, string? Fix = null, bool FixApplied = false);

    private sealed record LastGood(string Commit, string Branch, string RecordedAtUtc);

    private static string LastGoodPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "nexus-agent", "last-good.json");

    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Any(a => a is "--help" or "-h"))
        {
            Console.WriteLine("""
                nexus-agent doctor [--fix]

                Checks (and with --fix, repairs where non-interactive) everything a
                QA-assist session needs on this machine. Interactive steps (AWS SSO
                setup) are reported with instructions, never run silently.
                """);
            return 0;
        }

        var fix = args.Contains("--fix");
        var checks = new List<Check>();

        // 1. .NET — trivially present (this tool runs on it), report the version.
        checks.Add(new Check("dotnet_runtime", true, $".NET {Environment.Version}"));

        // 2. Repo root (RepoLocator: git toplevel first, sln-walk fallback, worktree-safe).
        var repoRoot = RepoLocator.Resolve();
        checks.Add(repoRoot != null
            ? new Check("repo", true, repoRoot)
            : new Check("repo", false, "repo root not found from the current directory.",
                "cd into the bolt_automation_nexus checkout (or clone it) and re-run."));

        // 3. Git state.
        string? commit = null, branch = null;
        var dirty = false;
        if (repoRoot != null)
        {
            branch = RunGit(repoRoot, "rev-parse --abbrev-ref HEAD");
            commit = RunGit(repoRoot, "rev-parse HEAD");
            var status = RunGit(repoRoot, "status --porcelain");
            dirty = !string.IsNullOrWhiteSpace(status);
            checks.Add(commit != null
                ? new Check("git", true, $"branch={branch} commit={commit[..8]} dirty={dirty}")
                : new Check("git", false, "git did not respond — is it installed and on PATH?",
                    "winget install --id Git.Git"));
        }

        // 4. Build freshness — via ReflectionLoader, the SAME staleness rule the
        //    browser/code verbs enforce (ranked DLL resolution + source-mtime check),
        //    so doctor can never say "fresh" where a command would say "stale".
        var buildOk = false;
        if (repoRoot != null)
        {
            var loader = new ReflectionLoader(repoRoot);
            if (!loader.DllExists)
                checks.Add(new Check("build_current", false,
                    "no FrontEnds build output found", "dotnet build Bolt.Automation.sln"));
            else if (loader.IsStale(out var newestSource))
                checks.Add(new Check("build_current", false,
                    $"stale build: {loader.DllPath} ({loader.DllMtimeUtc:u}) < newest source {newestSource:u}",
                    "dotnet build Bolt.Automation.sln"));
            else
            {
                buildOk = true;
                checks.Add(new Check("build_current", true, $"{loader.DllPath} built {loader.DllMtimeUtc:u}"));
            }
        }

        // 5. Playwright browsers.
        var pwDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ms-playwright");
        static bool HasChromium(string dir) =>
            Directory.Exists(dir) && Directory.EnumerateDirectories(dir, "chromium-*").Any();
        var chromium = HasChromium(pwDir);
        if (!chromium && fix)
        {
            // Re-guard after install: PLAYWRIGHT_BROWSERS_PATH machines install
            // elsewhere and pwDir may still not exist — report, never crash.
            var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
            chromium = exit == 0 && HasChromium(pwDir);
            checks.Add(new Check("playwright_chromium", chromium,
                chromium ? "installed by --fix"
                    : exit == 0 ? "install reported success but chromium not found under %LOCALAPPDATA%\\ms-playwright (PLAYWRIGHT_BROWSERS_PATH override?)"
                    : "install failed — see output above", FixApplied: true));
        }
        else
        {
            checks.Add(new Check("playwright_chromium", chromium,
                chromium ? "installed" : "chromium not found under %LOCALAPPDATA%\\ms-playwright",
                chromium ? null : "nexus-agent doctor --fix (runs `playwright install chromium`)"));
        }

        // 6. AWS CLI (needed once per secrets sync).
        var awsPath = FindAws();
        checks.Add(new Check("aws_cli", awsPath != null,
            awsPath ?? "AWS CLI v2 not found",
            awsPath != null ? null : "winget install --id Amazon.AWSCLI --accept-source-agreements --accept-package-agreements (then reopen the terminal)"));

        // 7. AWS SSO profile (interactive — never auto-fixed).
        var awsConfig = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aws", "config");
        var ssoConfigured = File.Exists(awsConfig)
            && File.ReadAllText(awsConfig).Contains("sso", StringComparison.OrdinalIgnoreCase);
        checks.Add(new Check("aws_sso_profile", ssoConfigured,
            ssoConfigured ? awsConfig : "no SSO profile in ~/.aws/config",
            ssoConfigured ? null : "run `aws configure sso` — the nexus-secrets skill has the exact values (start URL, account, role); the USER runs this, it opens a browser."));

        // 8. Secrets bundle.
        var bundleExists = File.Exists(SecretsCache.BundlePath);
        var sidecar = SecretsCache.Read();
        var bundleDetail = !bundleExists
            ? "no bundle cached"
            : sidecar == null
                ? "bundle present (age unknown)"
                : $"synced {(DateTimeOffset.UtcNow - sidecar.SyncedAt).TotalHours:F1}h ago (ttl {sidecar.TtlHours}h)";
        checks.Add(new Check("secrets_bundle", bundleExists, bundleDetail,
            bundleExists ? null : "nexus-agent secrets sync (after aws sso login)"));

        // 9. BOLT_SECRETS_PATH — the ONE env var the framework reads for local secrets.
        var envNow = Environment.GetEnvironmentVariable("BOLT_SECRETS_PATH");
        var envUser = OperatingSystem.IsWindows()
            ? Environment.GetEnvironmentVariable("BOLT_SECRETS_PATH", EnvironmentVariableTarget.User)
            : envNow;
        var envOk = !string.IsNullOrEmpty(envNow) || !string.IsNullOrEmpty(envUser);
        if (!envOk && fix && OperatingSystem.IsWindows())
        {
            Environment.SetEnvironmentVariable("BOLT_SECRETS_PATH", SecretsCache.Dir, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("BOLT_SECRETS_PATH", SecretsCache.Dir);
            checks.Add(new Check("bolt_secrets_path", true, $"set to {SecretsCache.Dir} (user-level; reopen other terminals/IDEs)", FixApplied: true));
        }
        else
        {
            checks.Add(new Check("bolt_secrets_path", envOk,
                envOk ? (envNow ?? envUser)! : "not set (process or user)",
                envOk ? null : "nexus-agent doctor --fix (sets it user-level to the secrets cache dir)"));
        }

        // 10. Claude permissions allowlist (kills permission-prompt spam for QAs).
        if (repoRoot != null)
        {
            var settings = Path.Combine(repoRoot, ".claude", "settings.json");
            var present = File.Exists(settings);
            checks.Add(new Check("claude_allowlist", present,
                present ? settings : ".claude/settings.json missing from the checkout",
                present ? null : "git pull — the allowlist is checked in; if still missing, the branch predates it."));
        }

        // 11. Last-known-good build record (broken-develop fallback). Skip the
        //     rewrite when the recorded commit hasn't changed.
        LastGood? lastGood = ReadLastGood();
        if (buildOk && commit != null && !dirty && lastGood?.Commit != commit)
        {
            lastGood = new LastGood(commit, branch ?? "?", DateTime.UtcNow.ToString("o"));
            Directory.CreateDirectory(Path.GetDirectoryName(LastGoodPath)!);
            await File.WriteAllTextAsync(LastGoodPath, JsonSerializer.Serialize(lastGood));
        }

        var allOk = checks.All(c => c.Ok);
        var payload = new
        {
            ok = allOk,
            fix_mode = fix,
            checks = checks.Select(c => new
            {
                c.Name,
                c.Ok,
                c.Detail,
                fix = c.Fix,
                fix_applied = c.FixApplied ? true : (bool?)null,
            }),
            last_good_build = lastGood == null ? null : new
            {
                commit = lastGood.Commit,
                branch = lastGood.Branch,
                recorded_at_utc = lastGood.RecordedAtUtc,
                fallback_hint = "If develop is broken: git checkout <commit> (detached), build, work; `git checkout develop` to return.",
            },
        };

        // Same JSON either way; exit 2 signals "issues found" so agents can branch.
        return await CommandBase.EmitJsonAsync(payload, exitCode: allOk ? 0 : 2);
    }

    private static string? FindAws()
    {
        var direct = @"C:\Program Files\Amazon\AWSCLIV2\aws.exe";
        if (File.Exists(direct)) return direct;
        var fromPath = Environment.GetEnvironmentVariable("PATH")?
            .Split(Path.PathSeparator)
            .Select(p => Path.Combine(p.Trim(), OperatingSystem.IsWindows() ? "aws.exe" : "aws"))
            .FirstOrDefault(File.Exists);
        return fromPath;
    }

    // ProcessRunner drains stdout+stderr asynchronously (a git that chats on stderr
    // can otherwise fill the pipe and deadlock a naive ReadToEnd) and enforces the timeout.
    private static string? RunGit(string repoRoot, params string[] arguments)
    {
        var result = ProcessRunner.Run("git", arguments, repoRoot, timeoutMs: 15_000);
        return result.Started && result.ExitCode == 0 ? result.StdOut.Trim() : null;
    }

    private static LastGood? ReadLastGood()
    {
        try
        {
            return File.Exists(LastGoodPath)
                ? JsonSerializer.Deserialize<LastGood>(File.ReadAllText(LastGoodPath))
                : null;
        }
        catch
        {
            return null;
        }
    }
}
