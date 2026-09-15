#Requires -Version 7
<#
.SYNOPSIS
    Stop-hook reminder: changed test files have not been through the nexus-test-review checklist.

.DESCRIPTION
    Fires when the working tree has modifications under Bolt.Automation.Tests/. Reminds once
    per session — the tree stays dirty for the whole piece of work, so an unconditional
    reminder would fire every turn and could never be satisfied.

    Advisory only: exits 0 and never blocks. A blocking Stop hook keyed on a dirty tree would
    trap the session, since finishing the work is what makes the tree dirty.

.EXAMPLE
    '{"session_id":"abc"}' | pwsh -NoProfile -File scripts/claude-test-review-reminder.ps1
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$sessionId = 'no-session'
$raw = [Console]::In.ReadToEnd()
if (-not [string]::IsNullOrWhiteSpace($raw)) {
    try {
        $payload = $raw | ConvertFrom-Json
        if ($payload.PSObject.Properties['session_id']) {
            $candidate = [string]$payload.PSObject.Properties['session_id'].Value
            if ($candidate) { $sessionId = $candidate }
        }
    } catch { }
}

$repoRoot = (git rev-parse --show-toplevel 2>$null)
if (-not $repoRoot) { exit 0 }

Push-Location $repoRoot
try {
    $changed = @(git status --porcelain -- 'Bolt.Automation.Tests' 2>$null)
} finally {
    Pop-Location
}
if ($changed.Count -eq 0) { exit 0 }

$safeId = ($sessionId -replace '[^A-Za-z0-9_-]', '_')
$sentinel = Join-Path ([System.IO.Path]::GetTempPath()) "claude-test-review-$safeId.flag"
if (Test-Path -LiteralPath $sentinel) { exit 0 }
New-Item -ItemType File -Path $sentinel -Force | Out-Null

$files = ($changed | ForEach-Object { ($_ -replace '^\s*\S+\s+', '') } | Select-Object -First 8) -join ', '

@{
    systemMessage      = 'test-review: changed test files have not been reviewed this session'
    hookSpecificOutput = @{
        hookEventName    = 'Stop'
        additionalContext = "Modified under Bolt.Automation.Tests/: $files`n" +
                            "Before reporting this work complete, run the nexus-test-review checklist over the changed tests: " +
                            "attributes ([Tenant]/[Category]/[TestCaseId]/[Author]), assertions in the test body only, " +
                            "no step-narration comments, no logging in test bodies, and the tests actually run green. " +
                            "Reminder fires once per session."
    }
} | ConvertTo-Json -Depth 5 -Compress

exit 0
