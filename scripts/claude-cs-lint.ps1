#Requires -Version 7
<#
.SYNOPSIS
    Advisory lint for C# changes, driven by the Claude Code PostToolUse hook.

.DESCRIPTION
    Reads the hook payload on stdin, resolves the edited file, and inspects ONLY the
    lines added relative to HEAD. Every rule here encodes a mistake that actually shipped
    (or nearly shipped) in this repo, so a hit is worth reading rather than dismissing.

    Advisory by design: always exits 0. It never blocks an edit.

.EXAMPLE
    # by hand, on one file
    pwsh -NoProfile -File scripts/claude-cs-lint.ps1 -Path Bolt.Automation.Tests/Tests/Foo.cs

.EXAMPLE
    # the way the hook calls it
    '{"tool_input":{"file_path":"C:\\repo\\Foo.cs"}}' | pwsh -NoProfile -File scripts/claude-cs-lint.ps1
#>
[CmdletBinding()]
param(
    # Bypass stdin and lint this file directly (for manual runs).
    [string]$Path
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-TargetPath {
    if ($Path) { return $Path }

    $raw = [Console]::In.ReadToEnd()
    if ([string]::IsNullOrWhiteSpace($raw)) { return $null }

    try { $payload = $raw | ConvertFrom-Json } catch { return $null }

    # StrictMode turns a missing property into a terminating error, and the payload shape
    # varies by tool, so probe rather than dereference.
    function Get-Prop($obj, [string[]]$path) {
        foreach ($name in $path) {
            if ($null -eq $obj -or -not $obj.PSObject.Properties[$name]) { return $null }
            $obj = $obj.PSObject.Properties[$name].Value
        }
        return $obj
    }

    foreach ($candidate in @(
            (Get-Prop $payload @('tool_response', 'filePath'))
            (Get-Prop $payload @('tool_input', 'file_path'))
        )) {
        if ($candidate) { return [string]$candidate }
    }
    return $null
}

# Lines added relative to HEAD. An untracked file counts as entirely added.
function Get-AddedLines([string]$file) {
    $tracked = $true
    git ls-files --error-unmatch -- $file *> $null
    if ($LASTEXITCODE -ne 0) { $tracked = $false }

    if (-not $tracked) {
        if (-not (Test-Path -LiteralPath $file)) { return @() }
        return Get-Content -LiteralPath $file
    }

    $diff = git diff HEAD --unified=0 -- $file 2>$null
    if (-not $diff) { return @() }

    return $diff |
        Where-Object { $_ -like '+*' -and $_ -notlike '+++*' } |
        ForEach-Object { $_.Substring(1) }
}

# Framework helper surfaces worth checking before a test project grows its own helper.
$script:FrameworkSurfaces = @(
    'Bolt.Automation.FrontEnds/PlaywrightBase/Infrastructure/Interface/IPageHelper.cs',
    'Bolt.Automation.FrontEnds/PlaywrightBase/PageHelper.cs',
    'Bolt.Automation.FrontEnds/PlaywrightBase/Helpers/TableHelper.cs',
    'Bolt.Automation.FrontEnds/PlaywrightBase/Helpers/WaitHelper.cs',
    'Bolt.Automation.FrontEnds/PlaywrightBase/Helpers/ValueHelper.cs',
    'Bolt.Automation.FrontEnds/PlaywrightBase/Helpers/ElementInteractionHelper.cs'
)

# Words too generic to discriminate - searching them would match half the framework.
$script:GenericTokens = @(
    'Get', 'Set', 'Read', 'Try', 'Fetch', 'Load', 'Ensure', 'Wait', 'Assert', 'Check',
    'Value', 'Values', 'Data', 'Async', 'Item', 'Items', 'List', 'From', 'With', 'Then',
    'Result', 'Results', 'Current', 'Selected', 'Table', 'Page', 'Element', 'Field'
)

# Names framework members that look like they already do what a new helper does.
# Searches the distinctive NOUN, not the author's chosen name - a duplicate almost never
# shares it. GetColumnValuesAsync and IPageHelper.GetColumnData overlap only on 'Column',
# which is why searching the full name finds nothing and reads as confirmation.
function Find-SimilarMembers([string]$methodName) {
    $tokens = @([regex]::Matches($methodName, '[A-Z][a-z0-9]*') |
        ForEach-Object { $_.Value } |
        Where-Object { $script:GenericTokens -notcontains $_ -and $_.Length -ge 4 } |
        Select-Object -Unique)

    if ($tokens.Count -eq 0) { return @() }

    $hits = [System.Collections.Generic.List[string]]::new()

    foreach ($token in $tokens) {
        foreach ($surface in $script:FrameworkSurfaces) {
            if (-not (Test-Path -LiteralPath $surface)) { continue }

            $found = @(Select-String -LiteralPath $surface -Pattern "\w*$token\w*\s*\(" -ErrorAction SilentlyContinue)
            foreach ($hit in $found) {
                $text = $hit.Line.Trim()
                if ($text -match '^(//|/// |\*|\[)') { continue }
                if ($text -notmatch '\bTask\b|\bstring\b|\bint\b|\bbool\b|\bILocator\b') { continue }

                $hits.Add("$([System.IO.Path]::GetFileName($surface)):$($hit.LineNumber)  $text")
            }
        }
    }

    return @($hits | Select-Object -Unique -First 4)
}

$target = Get-TargetPath
if (-not $target) { exit 0 }
if ($target -notmatch '\.cs$') { exit 0 }
if (-not (Test-Path -LiteralPath $target)) { exit 0 }

$repoRoot = (git rev-parse --show-toplevel 2>$null)
if (-not $repoRoot) { exit 0 }
Push-Location $repoRoot
try {
    $added = @(Get-AddedLines $target)
} finally {
    Pop-Location
}
if ($added.Count -eq 0) { exit 0 }

$relative = $target.Replace('\', '/')
$findings = [System.Collections.Generic.List[string]]::new()

# --- compact comments (philosophy:compact-comments) -------------------------------------
# One or two lines; a one-line XML <summary>. Multi-line blocks per member are a smell, and
# wide-relevance knowledge belongs in Documentation/agent-knowledge, not inline prose.
# Counted separately: a well-formed one-line <summary> is three lines once the tags are
# counted, so XML docs get a looser threshold than plain // runs.
$plainRun = 0; $worstPlain = 0
$docRun = 0; $worstDoc = 0
foreach ($line in $added) {
    if ($line -match '^\s*///') {
        $plainRun = 0
        # Tag-only lines (<summary>, </summary>, <param …/>) are structure, not prose.
        if ($line -notmatch '^\s*///\s*</?\w+[^>]*>\s*$') {
            $docRun++; if ($docRun -gt $worstDoc) { $worstDoc = $docRun }
        }
    }
    elseif ($line -match '^\s*//') {
        $docRun = 0
        $plainRun++; if ($plainRun -gt $worstPlain) { $worstPlain = $plainRun }
    }
    else { $plainRun = 0; $docRun = 0 }
}
if ($worstPlain -ge 3) {
    $findings.Add("compact-comments: a $worstPlain-line // comment block was added. Keep it to one or two lines. If it is durable, wide-relevance knowledge, put it in Documentation/agent-knowledge and link it. See philosophy:compact-comments.")
}
if ($worstDoc -ge 4) {
    $findings.Add("compact-comments: an XML doc comment with $worstDoc lines of prose was added. A <summary> should be one line; reserve extra lines for a genuinely non-obvious contract. See philosophy:compact-comments.")
}

# --- MsgStatusCd is not a status assertion ----------------------------------------------
if ($added | Where-Object { $_ -match 'Assert' -and $_ -match 'MsgStatusCd' }) {
    $findings.Add("MsgStatusCd is the ACORD envelope's transport status, not the quote status. Assert QuoteStatus / SecondaryStatus instead. See kb partner:pgr-quote-status-polling.")
}

# --- navigation wait whose bool is never false -------------------------------------------
# The wait throws NavigationException on timeout and only ever returns true, so asserting its
# bool is a DEAD assertion — the message never prints and the test dies on the exception.
# Skipped when the diff already catches NavigationException, which is the correct handling.
$navWaitAdded = $added | Where-Object { $_ -match '^\s*(await|var\s+\w+\s*=\s*await)\s+.*WaitForNavigationOrUrlContainsAsync\s*\(' }
$navWaitHandled = $added | Where-Object { $_ -match 'catch\s*\(\s*NavigationException' }
if ($navWaitAdded -and -not $navWaitHandled) {
    $findings.Add("WaitForNavigationOrUrlContainsAsync throws on timeout and never returns false — do not assert its bool, that assertion can never fail. Let it throw when the navigation is a precondition; when the navigation IS the behaviour under test, catch NavigationException and assert the landing URL. See kb framework:navigation-waits.")
}

# --- placeholder test-case id -----------------------------------------------------------
if ($added | Where-Object { $_ -match 'TestCaseId["\s]*[(,]\s*0\s*\)' }) {
    $findings.Add("TestCaseId(0) is a placeholder. Mint the ADO Test Case and wire the real id before committing, and put 'TC #<id>' in the commit message so ADO links it.")
}

# --- new private helper in a test project ------------------------------------------------
# IPageHelper alone carries ~70 members, so "I have not seen it" is not evidence a helper is
# absent. Name concrete candidates rather than telling the author to go looking - the generic
# version of this warning was read and skipped, and a duplicate of IPageHelper.GetColumnData
# shipped anyway.
if ($relative -match 'Bolt\.Automation\.Tests/') {
    $newHelpers = @($added |
        ForEach-Object { [regex]::Match($_, '\bprivate\b[^(]*\basync\s+Task[^\s]*\s+([A-Z]\w+)\s*\(') } |
        Where-Object { $_.Success } |
        ForEach-Object { $_.Groups[1].Value } |
        Select-Object -Unique)

    foreach ($helper in $newHelpers) {
        $candidates = @(Find-SimilarMembers $helper)

        if ($candidates.Count -gt 0) {
            $findings.Add("'$helper' may already exist in the framework - reuse it, or say why not:`n      " + ($candidates -join "`n      "))
        }
        else {
            $findings.Add("A private async helper '$helper' was added to a test project. Confirm the framework does not already do this: nexus-agent code find-similar --pattern <Noun>. Prefer extending a shared helper additively over a per-fixture wrapper.")
        }
    }
}

# --- tests stay clean: raw Playwright, logging, page-identifier literals ------------------
# Scoped to Tests/Tests/ (the test bodies), not Tests/TestHelpers/ — a helper is exactly where
# this logic is SUPPOSED to live. Every rule below shipped in PR 84697 and was caught in review.
if ($relative -match 'Bolt\.Automation\.Tests/Tests/') {

    # Raw Playwright at the test layer. The test should call a page object or a component;
    # touching IPage directly is how conditional UI logic ends up in a test class.
    $playwrightHits = @($added | Where-Object {
            $_ -match '^\s*using\s+Microsoft\.Playwright\s*;' -or
            $_ -match '\b_currentPage\b' -or
            $_ -match '\.WaitForURLAsync\s*\(' -or
            $_ -match '\bPageWaitForURLOptions\b' -or
            $_ -match '\bpage\.Url\b'
        })
    if ($playwrightHits.Count -gt 0) {
        $findings.Add("Raw Playwright at the test layer (" + ($playwrightHits.Count) + " line(s)): " +
            ($playwrightHits[0].Trim()) +
            "`n      Move it behind a page object or a FrontEnds component and have the test call that. Branching on UI state belongs in a component, not a test class. See philosophy:tests-stay-clean item 1 and 6.")
    }

    # Logging in a test body. ExecuteStepAsync/StartStep are step orchestration and stay.
    $loggingHits = @($added | Where-Object {
            $_ -match '\b_logger\s*\.\s*(Info|Debug|Warn|Warning|Error|Trace)\s*\(' -or
            $_ -match '\bLogger\s*\.\s*(Info|Debug|Warn|Warning|Error|Trace)\s*\('
        })
    if ($loggingHits.Count -gt 0) {
        $findings.Add("Logging in a test body: " + ($loggingHits[0].Trim()) +
            "`n      Log inside the page object / helper the test calls, not in the test. Only _logger.ExecuteStepAsync(...) belongs here. See root CLAUDE.md and philosophy:tests-stay-clean.")
    }

    # A URL/page literal re-declared in a test when a page object already owns it. A change to
    # the page object then leaves the test silently waiting out its timeout.
    foreach ($line in $added) {
        $m = [regex]::Match($line, 'const\s+string\s+\w+\s*=\s*"([^"]{3,})"')
        if (-not $m.Success) { continue }

        $literal = $m.Groups[1].Value
        $escaped = [regex]::Escape($literal)
        # Matches both shapes a page object uses: the identifier inline, or via its own constant.
        $ownerPattern = '(PageIdentifier\s*=>\s*"' + $escaped + '"|UrlPart\s*=\s*"' + $escaped + '")'

        $pageFiles = @(Get-ChildItem -Path 'Bolt.Automation.FrontEnds/Projects' -Filter '*.cs' -Recurse -File -ErrorAction SilentlyContinue)
        $owners = @()
        if ($pageFiles.Count -gt 0) {
            $owners = @(Select-String -LiteralPath $pageFiles.FullName -Pattern $ownerPattern -ErrorAction SilentlyContinue |
                ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_.Path) } |
                Select-Object -Unique -First 3)
        }

        if ($owners.Count -gt 0) {
            $findings.Add("The literal `"$literal`" is already the PageIdentifier of " + ($owners -join ', ') +
                ". Reference the page object's own constant instead of re-declaring it, or that page's identifier can change without this test noticing.")
        }
    }

    # [Author] is required on every test (Tests/CLAUDE.md). Checked against the whole file, since
    # the attribute may predate this edit.
    $addsTest = @($added | Where-Object { $_ -match '^\s*\[Test(Case)?\b' -or $_ -match '^\s*\[TestCaseSource\b' })
    if ($addsTest.Count -gt 0) {
        $fileText = Get-Content -LiteralPath $target -Raw -ErrorAction SilentlyContinue
        if ($fileText -and $fileText -notmatch '\[Author\s*\(') {
            $findings.Add("A test was added to a file with no [Author(Author.<name>)]. Tests/CLAUDE.md requires it on every test; if the owner is unknown, ask rather than omit. Remember the alias using: using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;")
        }
    }
}

# --- registry entry that every fill of the page will answer ------------------------------
# Pages + DefaultValue with no DependsOn opts EVERY test filling those pages into answering
# the field. That exact shape regressed a green MPQ3 test.
$blocks = ($added -join "`n") -split 'new\s+UIElement'
foreach ($block in $blocks) {
    if ($block -match 'Pages\s*=\s*\[\s*typeof\(' -and $block -match 'DefaultValue' -and $block -notmatch 'DependsOn') {
        $findings.Add("A FieldRegistry entry adds Pages + DefaultValue with no DependsOn — every test that fills those pages will now answer this field. Gate it with DependsOn/DependsOnValue, or leave it out of Pages and drive it explicitly. Then re-run the suites that already passed on those pages. See kb framework:field-registry.")
        break
    }
}

if ($findings.Count -eq 0) { exit 0 }

$body = "Advisory lint on $relative (added lines only):`n" +
        (($findings | ForEach-Object { "  - $_" }) -join "`n")

@{
    systemMessage      = "cs-lint: $($findings.Count) advisory finding(s) in $relative"
    hookSpecificOutput = @{
        hookEventName    = 'PostToolUse'
        additionalContext = $body
    }
} | ConvertTo-Json -Depth 5 -Compress

exit 0
