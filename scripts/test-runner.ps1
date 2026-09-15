#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs tests from the pre-built Docker image
#>

[string]$Project = "Bolt.Automation.Tests/Bolt.Automation.Tests.csproj"
$projectPath = "/app/$Project"
Write-Host "Testing project $projectPath"


$ErrorActionPreference = 'Stop'

function Write-Info($msg) { Write-Host "[test-runner] $msg" -ForegroundColor Cyan }

# Read environment variables with defaults
$envName = if ($env:ASPNETCORE_ENVIRONMENT) { $env:ASPNETCORE_ENVIRONMENT } else { "QA" }
$headless = if ($env:HEADLESS) { $env:HEADLESS } else { "true" }
$testFilter = $env:TEST_FILTER
$artifacts = if ($env:ARTIFACTS_PATH) { $env:ARTIFACTS_PATH } else { "/app/artifacts" }


# Ensure artifacts directory exists
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null

Write-Info "Configuration:"
Write-Info "  Environment: $envName"
Write-Info "  Headless: $headless"
Write-Info "  Test Filter: $testFilter"
Write-Info "  Artifacts Path: $artifacts"

# Set environment variables
$env:HEADLESS = $headless
$env:ASPNETCORE_ENVIRONMENT = $envName
# CI logging configuration - reduce console noise
# Use LOGLEVEL env var to control NLog output (Trace, Debug, Info, Warn, Error, Fatal)
$logLevel = if ($env:LOGLEVEL) { $env:LOGLEVEL } else { "Warning" }
$env:LOGLEVEL = $logLevel
$consoleVerbosity = if ($env:CONSOLE_VERBOSITY) { $env:CONSOLE_VERBOSITY } else { "minimal" }
Write-Info "  Log Level: $logLevel"
Write-Info "  Console Verbosity: $consoleVerbosity"

# =============================================================================
# Parallel Execution Support
# =============================================================================
# If TEST_FILTER contains commas, run filters in parallel using start-parallel-tests.ps1
# Single filter or no filter uses legacy single-process execution

$filters = @()
$useParallelMode = $false

if ($testFilter) {
    if ($testFilter -match ',') {
        # Comma-separated = parallel mode
        $filters = $testFilter -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ }
        $useParallelMode = $true
        Write-Info "Parallel mode detected: $($filters.Count) filters"
        Write-Info "  Filters: $($filters -join ', ')"
    } else {
        # Single filter = legacy mode (no parallel)
        $filters = @($testFilter)
        Write-Info "Single filter mode: $testFilter"
    }
} else {
    # No filter specified - single process, run all tests
    Write-Info "No filter specified - running all tests in single process"
}

if ($useParallelMode -and $filters.Count -gt 1) {
    # =============================================================================
    # PARALLEL EXECUTION MODE
    # =============================================================================
    Write-Info ""
    Write-Info "=============================================="
    Write-Info "  Launching Parallel Test Execution"
    Write-Info "=============================================="

    $parallelScript = "/app/scripts/start-parallel-tests.ps1"

    if (-not (Test-Path $parallelScript)) {
        Write-Info "ERROR: Parallel test script not found at $parallelScript"
        exit 1
    }

    & $parallelScript `
        -Filters $filters `
        -Project $projectPath `
        -ArtifactsBase $artifacts `
        -Configuration "Release"

    $exitCode = $LASTEXITCODE

} else {
    # =============================================================================
    # LEGACY SINGLE-PROCESS EXECUTION MODE
    # =============================================================================
    Write-Info ""
    Write-Info "=============================================="
    Write-Info "  Single Process Test Execution"
    Write-Info "=============================================="

    # Build test command - use project file instead of DLL for better test results handling
    $testArgs = @("test", $projectPath, "-c", "Release", "--no-build", "--no-restore")

    # Add test filter if specified
    if ($testFilter) {
        $testArgs += @("--filter", $testFilter)
    }

    # Set results directory directly to artifacts
    $testArgs += @("--results-directory", $artifacts)

    # Add logger for TRX output with full path
    $trxFileName = "test-results-$(Get-Date -Format 'yyyyMMdd-HHmmss').trx"
    $testArgs += @("--logger", "trx;LogFileName=$trxFileName")

    # Add console logger for immediate output (use minimal verbosity in CI to reduce log noise)
    $testArgs += @("--logger", "console;verbosity=$consoleVerbosity")

    # Run tests
    Write-Info "Running tests: dotnet $($testArgs -join ' ')"
    Write-Info ""

    dotnet @testArgs
    $exitCode = $LASTEXITCODE
}

# Copy additional test artifacts
Write-Info ""
Write-Info "Collecting additional test artifacts..."

# Copy TestResults from build directory if any
$testResultsDir = "/app/Bolt.Automation.Tests/TestResults"
if (Test-Path $testResultsDir) {
    Get-ChildItem $testResultsDir -Recurse | ForEach-Object {
        $relativePath = $_.FullName.Substring($testResultsDir.Length).TrimStart('/')
        $destPath = Join-Path $artifacts "TestResults/$relativePath"
        $destDir = Split-Path $destPath -Parent
        
        if (-not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Force -Path $destDir | Out-Null
        }
        
        Copy-Item $_.FullName -Destination $destPath -Force
    }
    Write-Info "  Copied TestResults directory"
}

# Copy screenshots if any
$screenshotsDir = "/app/Bolt.Automation.Tests/bin/Release/net10.0/Screenshots"
if (Test-Path $screenshotsDir) {
    Copy-Item $screenshotsDir -Destination "$artifacts/Screenshots" -Recurse -Force
    Write-Info "  Copied Screenshots directory"
}

# Copy logs
$logsDir = "/app/Bolt.Automation.Tests/logs"
if (Test-Path $logsDir) {
    Copy-Item $logsDir -Destination "$artifacts/logs" -Recurse -Force
    Write-Info "  Copied logs directory"
}

# List all TRX files in artifacts
Write-Info ""
Write-Info "TRX files in artifacts:"
Get-ChildItem $artifacts -Filter *.trx -Recurse | ForEach-Object {
    Write-Info "  - $($_.FullName)"
}

Write-Info ""
Write-Info "Artifacts saved to: $artifacts"
Write-Info "Test run completed with exit code: $exitCode"

exit $exitCode
