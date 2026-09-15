#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs dotnet test in parallel for multiple filters using PowerShell Start-Job
.DESCRIPTION
    Enables maximum parallelization within a single Kubernetes pod by running
    multiple dotnet test processes concurrently, each with a different filter.
.PARAMETER Filters
    Array of filter expressions (e.g., @("Sanity", "API", "D2C"))
.PARAMETER Project
    Path to test project
.PARAMETER ArtifactsBase
    Base directory for artifacts
.PARAMETER Configuration
    Build configuration (Release/Debug)
.EXAMPLE
    ./start-parallel-tests.ps1 -Filters @("Sanity", "API", "D2C")
#>

param(
    [Parameter(Mandatory=$true)]
    [string[]]$Filters,
    [string]$Project = "/app/Bolt.Automation.Tests/Bolt.Automation.Tests.csproj",
    [string]$ArtifactsBase = "/app/artifacts",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = 'Continue'

function Write-Info($msg) { Write-Host "[parallel-tests] $msg" -ForegroundColor Cyan }
function Write-Success($msg) { Write-Host "[parallel-tests] $msg" -ForegroundColor Green }
function Write-Warn($msg) { Write-Host "[parallel-tests] $msg" -ForegroundColor Yellow }
function Write-Err($msg) { Write-Host "[parallel-tests] $msg" -ForegroundColor Red }

# Auto-detect CPU cores for concurrency limit
$maxConcurrent = [Environment]::ProcessorCount
if ($maxConcurrent -lt 1) { $maxConcurrent = 1 }

# Generate unique run identifiers
$runId = Get-Date -Format "yyyyMMdd-HHmmss"
$randomSuffix = -join ((48..57) + (97..122) | Get-Random -Count 6 | ForEach-Object {[char]$_})

Write-Info "=============================================="
Write-Info "  Parallel Test Execution"
Write-Info "=============================================="
Write-Info "Run ID: ${runId}_${randomSuffix}"
Write-Info "Filters: $($Filters -join ', ')"
Write-Info "Filter count: $($Filters.Count)"
Write-Info "CPU cores detected: $maxConcurrent"
Write-Info "Max concurrent jobs: $maxConcurrent"
Write-Info "Project: $Project"
Write-Info "Artifacts base: $ArtifactsBase"
Write-Info "=============================================="
Write-Info ""

# Ensure artifacts base directory exists
New-Item -ItemType Directory -Force -Path $ArtifactsBase | Out-Null

# Queue for managing concurrent jobs
$jobQueue = [System.Collections.Generic.List[object]]::new()
$completedJobs = [System.Collections.Generic.List[object]]::new()
$filterIndex = 0
$startTime = Get-Date

# Process filters with concurrency control
while ($filterIndex -lt $Filters.Count -or $jobQueue.Count -gt 0) {

    # Start new jobs up to concurrency limit
    while ($filterIndex -lt $Filters.Count -and $jobQueue.Count -lt $maxConcurrent) {
        $filter = $Filters[$filterIndex]
        $filterIndex++

        # Sanitize filter for file/directory naming
        $safeFilter = $filter -replace '[^a-zA-Z0-9]', '_'

        # Create filter-specific artifact directory
        $filterArtifacts = Join-Path $ArtifactsBase $safeFilter
        New-Item -ItemType Directory -Force -Path $filterArtifacts | Out-Null

        # Unique TRX filename with all identifiers
        $trxName = "test-results_${runId}_${safeFilter}_${randomSuffix}.trx"

        Write-Info "[$filterIndex/$($Filters.Count)] Starting: $filter -> $filterArtifacts"

        # Build test arguments
        $testArgs = @(
            "test", $Project,
            "-c", $Configuration,
            "--no-build", "--no-restore",
            "--filter", "Category=$filter",
            "--results-directory", $filterArtifacts,
            "--logger", "trx;LogFileName=$trxName",
            "--logger", "console;verbosity=normal"
        )

        # Start background job
        $job = Start-Job -ScriptBlock {
            param($testArgs)

            # Run dotnet test and capture output
            $output = & dotnet @testArgs 2>&1
            $exitCode = $LASTEXITCODE

            return @{
                ExitCode = $exitCode
                Output = $output -join "`n"
            }
        } -ArgumentList (,$testArgs)

        $jobQueue.Add(@{
            Job = $job
            Filter = $filter
            SafeFilter = $safeFilter
            ArtifactsPath = $filterArtifacts
            TrxName = $trxName
            StartTime = Get-Date
            Index = $filterIndex
        })
    }

    # Check for completed jobs
    $toRemove = @()
    foreach ($item in $jobQueue) {
        if ($item.Job.State -eq 'Completed' -or $item.Job.State -eq 'Failed') {
            $result = Receive-Job -Job $item.Job
            $duration = (Get-Date) - $item.StartTime

            $item.ExitCode = if ($null -ne $result -and $null -ne $result.ExitCode) { $result.ExitCode } else { 1 }
            $item.Duration = $duration
            $item.Output = if ($null -ne $result -and $null -ne $result.Output) { $result.Output } else { "" }

            if ($item.ExitCode -eq 0) {
                Write-Success "COMPLETED: $($item.Filter) in $($duration.TotalSeconds.ToString('F1'))s"
            } else {
                Write-Warn "FAILED: $($item.Filter) (exit code: $($item.ExitCode)) in $($duration.TotalSeconds.ToString('F1'))s"
            }

            $completedJobs.Add($item)
            $toRemove += $item
            Remove-Job -Job $item.Job -Force
        }
    }

    foreach ($item in $toRemove) {
        $jobQueue.Remove($item) | Out-Null
    }

    # Brief sleep to avoid tight loop
    if ($jobQueue.Count -gt 0) {
        Start-Sleep -Milliseconds 500
    }
}

$totalDuration = (Get-Date) - $startTime

# Summary
Write-Info ""
Write-Info "=============================================="
Write-Info "  Parallel Test Execution Summary"
Write-Info "=============================================="

$passed = ($completedJobs | Where-Object { $_.ExitCode -eq 0 }).Count
$failed = ($completedJobs | Where-Object { $_.ExitCode -ne 0 }).Count

Write-Info "Total filters: $($Filters.Count)"
Write-Info "Passed: $passed"
Write-Info "Failed: $failed"
Write-Info "Total duration: $($totalDuration.TotalSeconds.ToString('F1'))s"
Write-Info ""
Write-Info "Results by filter:"

foreach ($job in ($completedJobs | Sort-Object { $_.Index })) {
    $status = if ($job.ExitCode -eq 0) { "PASS" } else { "FAIL" }
    $statusColor = if ($job.ExitCode -eq 0) { "Green" } else { "Red" }
    Write-Host "  [$status] $($job.Filter) - $($job.Duration.TotalSeconds.ToString('F1'))s - $($job.TrxName)" -ForegroundColor $statusColor
}

Write-Info ""
Write-Info "Artifacts location: $ArtifactsBase"

# List TRX files
Write-Info ""
Write-Info "Generated TRX files:"
Get-ChildItem $ArtifactsBase -Filter "*.trx" -Recurse | ForEach-Object {
    Write-Info "  - $($_.FullName)"
}

Write-Info ""
Write-Info "=============================================="

# Return failure if any test run failed
$finalExitCode = if ($failed -gt 0) { 1 } else { 0 }
Write-Info "Final exit code: $finalExitCode"

exit $finalExitCode
