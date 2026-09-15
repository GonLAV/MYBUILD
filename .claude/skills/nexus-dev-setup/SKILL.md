---
name: nexus-dev-setup
description: Bring a fresh/reset Windows machine from zero to running nexus automation tests locally. Covers .NET 10 SDK (global.json pin), PowerShell 7, VS Code + C# Dev Kit, NuGet restore against the private Bolt feed, Playwright Chromium browsers, the two gitignored *.runsettings files (local + vscode), then AWS/secrets via nexus-secrets, ending in a verified build + green smoke test. Trigger when a developer says they reimaged/reset their machine, cloned the repo for the first time, can't build or restore ("SDK not found", "Bolt.* package not found"), gets "Executable doesn't exist at …ms-playwright…", sees no tests in VS Code Test Explorer, or asks "how do I set this up again". Do NOT trigger for secrets-only problems (nexus-secrets), a failing test on an already-working machine (nexus-debug), or authoring tests (nexus-test-author).
---

# nexus-dev-setup

Take a machine that has nothing (or was just reset) to the point where
`dotnet test` runs a headed UI test green, in the order where each step gates the next.

## Mental model: six layers, strictly ordered

```
1. Toolchain    git · .NET 10 SDK · PowerShell 7 · VS Code + C# Dev Kit   ← OS-level, installed once
2. Source       clone + branch + network reach to the Bolt NuGet feed
3. Build        dotnet restore/build  → produces bin\Debug\net10.0\ (everything below reads it)
4. Browsers     playwright.ps1 install chromium (script lives IN the build output → needs layer 3)
5. Local config local.runsettings + vscode.runsettings (both GITIGNORED, so a fresh clone has neither)
6. Secrets      AWS SSO → nexus-agent secrets sync → BOLT_SECRETS_PATH   → delegate to nexus-secrets
```

The two most common ways this goes wrong: skipping a layer (installing browsers before the
first build; syncing secrets before `nexus-agent` exists), and not knowing that layers 5–6 produce
files and vars that are invisible in git. Nothing in the repo tells you they're missing; tests just fail
oddly. Everything in layers 5–6 is per-developer and never committed.

## Step 0: audit before you install anything

Run this first and skip every step whose check already passes. On a "reset" machine some layers
usually survive (the ms-playwright cache and user env vars live outside the repo).

```powershell
git --version
dotnet --list-sdks                      # need a 10.0.1xx or newer feature band (see Step 2)
pwsh -v                                 # PowerShell 7, separate from Windows PowerShell 5.1
code --list-extensions | Select-String dotnettools
Test-Path .\Bolt.Automation.Tests\bin\Debug\net10.0\playwright.ps1      # layer 3 done?
Get-ChildItem "$env:LOCALAPPDATA\ms-playwright" -EA SilentlyContinue    # layer 4 done?
Test-Path .\Bolt.Automation.Tests\local.runsettings, .\Bolt.Automation.Tests\vscode.runsettings
$env:BOLT_SECRETS_PATH                  # layer 6 done?
```

Report the results as a checklist before touching anything. You (the agent) run the read-only
checks; the *user* runs anything that installs software, opens a browser, or signs in. `winget`
can raise UAC, and `aws sso login` / VS Code sign-in / `claude` auth are interactive by nature.

## Step 1: Claude Code, git, repo

- Claude Code is a prerequisite of this skill, not a step in it. If you're reading this, it's
  installed and authenticated. (For a teammate starting cold: install Claude Code, run `claude`,
  authenticate in the browser, then open the repo folder and ask for this skill.)
- git: `winget install --id Git.Git`. Reopen the shell afterwards.
- Clone from Azure DevOps, then `git checkout develop` (the PR target branch).
- Corporate network: the private feed and every QA/UAT endpoint are internal. If you're off VPN,
  Step 3 fails at restore and Step 7's tests fail at the first HTTP call. Connect first.

## Step 2: .NET 10 SDK (the version is pinned)

`global.json` pins the SDK:

```json
{ "sdk": { "version": "10.0.103", "rollForward": "latestFeature" } }
```

`latestFeature` means any 10.0.1xx or higher feature band on the machine satisfies it (10.0.302
works). What does not work is having only .NET 9 or an older 10.0.0xx. Every `dotnet` command in
the repo folder then dies with *"compatible .NET SDK was not found"*.

```powershell
winget install --id Microsoft.DotNet.SDK.10 --accept-source-agreements --accept-package-agreements
```

Reopen the terminal, then confirm with `dotnet --list-sdks`. Do not edit `global.json` to match a
lower local SDK; it's shared with CI.

## Step 3: PowerShell 7 (`pwsh`)

Needed for two things: `playwright.ps1` (Step 5) and the repo's `scripts\*.ps1`. Windows PowerShell
5.1 cannot substitute. `playwright.ps1` loads the .NET 10 `Microsoft.Playwright.dll` into the
running host, and 5.1 (.NET Framework) fails that load.

```powershell
winget install --id Microsoft.PowerShell --accept-source-agreements --accept-package-agreements
```

If installing pwsh isn't an option, Step 5 has a pwsh-free fallback.

**pwsh also gates the repo's Claude Code hooks** (`scripts\claude-cs-lint.ps1`,
`scripts\claude-test-review-reminder.ps1`, both `#Requires -Version 7`). Without it those hooks
fail silently and you lose the advisory lint. Confirm with `pwsh -v` before moving on.

### Enrol the repo's git hooks

One command per clone. `.githooks/commit-msg` strips AI attribution trailers that would otherwise
reach `develop` (see the "Never do" rule in the root `CLAUDE.md`).

```powershell
git config core.hooksPath .githooks
```

Verify with `git config core.hooksPath` — it should print `.githooks`.

## Step 4: VS Code + C# Dev Kit, then build

```powershell
winget install --id Microsoft.VisualStudioCode
code --install-extension ms-dotnettools.csdevkit
```

`csdevkit` pulls in `ms-dotnettools.csharp` and `ms-dotnettools.vscode-dotnet-runtime`; those three
are what a working machine shows. C# Dev Kit asks you to sign in on first use; do that before
expecting Test Explorer to populate. (`ms-dotnettools.csharp` alone gives you language support and
debugging but a weaker test UI.)

Then restore and build. This is the gate for layers 4–6:

```bash
dotnet build Bolt.Automation.sln
```

NuGet: `NuGet.config` declares two sources with package-source mapping, `nuget.org` for `*` and
`bolt` (`https://boltnuget.boltqa.com/nuget`) for `Bolt.*`. No credentials are configured, so the feed
is reachable by network position. A `Bolt.Microservice.*` "unable to find package" or 401 at restore
means VPN, not a broken config. Never add a PAT into `NuGet.config` (it's committed); use
`dotnet nuget update source` with a user-level store if credentials ever become necessary.

## Step 5: Playwright browsers (Chromium)

The Playwright NuGet package drops `playwright.ps1` into each project's build output, so this only
works after Step 4. Install chromium:

```powershell
pwsh .\Bolt.Automation.Tests\bin\Debug\net10.0\playwright.ps1 install chromium
```

- **Chromium, not chrome.** `BrowserOptions.BrowserType` defaults to `Chromium` (channel `""`,
  Playwright's bundled build). The `"Browser": { "DriverType": "chrome" }` line in `appsettings.json`
  is a stale key name. It doesn't bind to `BrowserType`, so nothing selects the `chrome` channel
  locally. (The Dockerfile installs `chrome` `--with-deps`; that's the Linux CI image, not your box.)
- **Where they land:** `%LOCALAPPDATA%\ms-playwright\chromium-<build>`. Outside the repo, so it
  survives clean/reclone but not an OS reset. `--with-deps` is Linux-only; skip it on Windows.
- **pwsh-free fallback** (same result, uses the bundled node driver):
  ```powershell
  $o = ".\Bolt.Automation.Tests\bin\Debug\net10.0\.playwright"
  & "$o\node\win32_x64\node.exe" "$o\package\cli.js" install chromium
  ```
- Re-run this after a `Microsoft.Playwright` version bump (currently 1.60.0). The driver pins an
  exact browser build, and a stale cache throws *"Executable doesn't exist at …ms-playwright\chromium-…"*.

## Step 6: the two gitignored runsettings files

`.gitignore` excludes `*.runsettings` (and `/Bolt.Automation.Tests/local.runsettings` explicitly), so
a fresh clone has none of these. You create them by hand. Two different consumers:

**a) `Bolt.Automation.Tests\local.runsettings`, for `dotnet test` from a shell.**
```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <TestRunParameters>
    <Parameter name="Environment" value="Qa" />
    <Parameter name="Browser:Headless" value="false" />
  </TestRunParameters>
</RunSettings>
```
Use it with `dotnet test … --settings Bolt.Automation.Tests\local.runsettings`. `Environment` here
overrides `ASPNETCORE_ENVIRONMENT` (`ConfigurationLoader` ranks runsettings parameters above the
env var). That's the #1 cause of "my secrets/tenant resolve to the wrong env" later.

**b) `Bolt.Automation.Tests\vscode.runsettings`, for the VS Code Test Explorer.**
`.vscode\settings.json` is committed and already points at it:
```json
"dotnet.unitTests.runSettingsPath": "Bolt.Automation.Tests/vscode.runsettings"
```
The path is committed; the file it names is gitignored. On a fresh clone this reference dangles
and Test Explorer runs with no settings (or errors). Create it with both blocks:
```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <RunConfiguration>
    <EnvironmentVariables>
      <BOLT_SECRETS_PATH>C:\Users\&lt;you&gt;\AppData\Local\BoltAutomation\secrets</BOLT_SECRETS_PATH>
      <ASPNETCORE_ENVIRONMENT>Qa</ASPNETCORE_ENVIRONMENT>
    </EnvironmentVariables>
  </RunConfiguration>
  <TestRunParameters>
    <Parameter name="Environment" value="Qa" />
    <Parameter name="Browser:Headless" value="false" />
  </TestRunParameters>
</RunSettings>
```
`<EnvironmentVariables>` and `<TestRunParameters>` are not interchangeable. The env name is read
from either, but `BOLT_SECRETS_PATH` is read only from the process environment, so only the
`<RunConfiguration>` block delivers it to the IDE's `testhost`. (Full rationale: nexus-secrets
Step 6b.) For F5 debugging, put the same two vars in the launch.json `"env"` block; launch.json does
not read runsettings.

Never put a credential in either file. They hold environment/browser switches only; secrets come
from the bundle in Step 7.

## Step 7: AWS + secrets (delegate; don't duplicate)

Everything from "install the AWS CLI" through `aws configure sso`, `aws sso login`,
`nexus-agent secrets sync`, and persisting `BOLT_SECRETS_PATH` lives in the nexus-secrets skill.
Follow it there, then come back. Two ordering facts that belong here:

- `nexus-agent` is not a packaged dotnet tool and is not on PATH. It's the build output of
  `Bolt.Automation.AgentTools` (assembly name `nexus-agent`), so Step 4 must be done first. Invoke it
  as `dotnet run --project Bolt.Automation.AgentTools -- secrets sync`, or run
  `Bolt.Automation.AgentTools\bin\Debug\net10.0\nexus-agent.exe` (alias it if you use it often).
- After setting `BOLT_SECRETS_PATH` with `setx`, fully restart VS Code. A reload isn't enough;
  the old process keeps the stale environment.

## Step 8: verify, in this order

```powershell
# 1. toolchain + restore + compile
dotnet build Bolt.Automation.sln

# 2. secrets chain + browser launch, no DB dependency
$env:BOLT_SECRETS_PATH = "$env:LOCALAPPDATA\BoltAutomation\secrets"
dotnet test Bolt.Automation.Tests\Bolt.Automation.Tests.csproj `
  --filter "FullyQualifiedName~VerifyBoltSecrets|FullyQualifiedName~VerifyBrowserLaunchAndNavigation"

# 3. a real headed UI test through your runsettings
dotnet test Bolt.Automation.Tests\Bolt.Automation.Tests.csproj `
  --settings Bolt.Automation.Tests\local.runsettings --filter "<a known-good test>"

# 4. the IDE path (the one people forget): open VS Code, run the same test from Test Explorer
```

Step 4 of that ladder matters. Shell and IDE resolve environment differently, and "works in the
terminal, fails in the IDE" is a distinct failure this setup is prone to. Confirm the log line
`[BoltSecrets] Loading app secrets … for env '<X>'` names the env you expect in both.

## Troubleshooting: symptom to layer

| Symptom | Layer | Fix |
|---|---|---|
| *"compatible .NET SDK was not found"* / SDK 10.0.1xx demanded | 1 | Install the .NET 10 SDK (Step 2). Don't downgrade `global.json`. |
| `Bolt.Microservice.*` not found / 401 on restore | 2 | Off VPN, or `NuGet.config` sources were altered. Reconnect; `dotnet restore --force`. |
| `playwright.ps1` throws on `[Reflection.Assembly]::Load` | 1 | You ran it in Windows PowerShell 5.1. Use `pwsh` (Step 3) or the node fallback (Step 5). |
| *"Executable doesn't exist at …ms-playwright\chromium-…"* | 4 | Browsers missing or stale after a Playwright bump. Re-run the install (Step 5). |
| `playwright.ps1` not found | 3 | You haven't built yet; the script only exists in build output. |
| Test Explorer shows no tests / ignores settings | 5 | `vscode.runsettings` is missing (gitignored, but `.vscode\settings.json` points at it). Create it (Step 6b). Then reload the C# Dev Kit / restart VS Code. |
| Test runs against the wrong environment | 5 | A runsettings `Environment` parameter outranks `ASPNETCORE_ENVIRONMENT`. Check *both* runsettings files. |
| `Set BOLT_SECRETS_PATH…`, null app secrets, `LaunchDarkly SDK key is not configured` | 6 | Go to nexus-secrets (its troubleshooting list, top-down). |
| Passes from the terminal, fails only in VS Code | 5/6 | Stale IDE environment. Fully quit and relaunch VS Code, and/or pin the vars in `vscode.runsettings` `<EnvironmentVariables>`. |
| Tests skip: *"Not configured to run in \<env\>"* | 5 | Env resolution, not secrets. The tenant isn't enabled for the resolved env. |
| `nexus-agent` not recognized | 3 | It's build output, not a global tool: `dotnet run --project Bolt.Automation.AgentTools -- …`. |
| Headed run opens nothing / hangs | 4/5 | `Browser:Headless` still `true` (env var `HEADLESS` defaults to true in CI paths). Set it `false` in the runsettings you're actually using. |

## Guardrails

- **Read-only by default.** Run the Step 0 audit and the verification commands yourself; hand the
  user every command that installs software, opens a browser, or signs in (winget/UAC,
  `aws configure sso`, `aws sso login`, VS Code and Claude authentication). Don't attempt an
  interactive login on their behalf.
- **Never commit local config.** `*.runsettings`, `**/automation_nexus_secrets_store.json`, and the
  secrets cache are gitignored on purpose. Before finishing, run `git status` and confirm the setup
  produced no staged changes. If a runsettings file ever shows up as tracked, that's a bug to fix,
  not a convenience.
- **No credentials in runsettings, `NuGet.config`, `appsettings*.json`, or launch.json.** Environment
  names, browser switches, and paths only. Anything secret comes from the bundle via nexus-secrets.
- **Don't edit shared files to fit one machine.** `global.json`, `NuGet.config`, `Directory.Packages.props`,
  and committed `appsettings*.json` are CI's too. Fix the machine, not the pin.
- **This skill sets up; it doesn't diagnose flows.** Once `dotnet build` is clean and the verification
  tests pass, a still-failing test is a test/framework problem. Go to nexus-debug.
