#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Tests if Docker can access the private NuGet feed
#>

$ErrorActionPreference = 'Continue'

function Write-TestResult {
    param([string]$Test, [bool]$Success, [string]$Message = "")
    if ($Success) {
        Write-Host "✓ $Test" -ForegroundColor Green
    } else {
        Write-Host "✗ $Test" -ForegroundColor Red
        if ($Message) {
            Write-Host "  $Message" -ForegroundColor Yellow
        }
    }
}

Write-Host "`n==> Testing Docker Network Connectivity" -ForegroundColor Cyan
Write-Host ""

# Test 1: Can host reach the feed?
Write-Host "Test 1: Host network connectivity..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "https://boltnuget.boltqa.com/" -TimeoutSec 10 -UseBasicParsing
    Write-TestResult "Host can reach boltnuget.boltqa.com" ($response.StatusCode -eq 200)
} catch {
    Write-TestResult "Host can reach boltnuget.boltqa.com" $false $_.Exception.Message
    Write-Host "`nERROR: Your host machine cannot reach the NuGet feed. Check your VPN connection." -ForegroundColor Red
    exit 1
}

# Test 2: Can Docker reach public internet?
Write-Host "`nTest 2: Docker internet connectivity..." -ForegroundColor Yellow
try {
    docker run --rm mcr.microsoft.com/dotnet/sdk:9.0 pwsh -c "curl -s https://api.nuget.org/v3/index.json | Select-Object -First 1" | Out-Null
    Write-TestResult "Docker can reach public internet (nuget.org)" $true
} catch {
    Write-TestResult "Docker can reach public internet" $false $_.Exception.Message
}

# Test 3: Can Docker reach private feed with default network?
Write-Host "`nTest 3: Docker default network to private feed..." -ForegroundColor Yellow
try {
    $output = docker run --rm mcr.microsoft.com/dotnet/sdk:9.0 pwsh -c "curl -s -m 10 https://boltnuget.boltqa.com/ 2>&1"
    $success = $output -match '"version"' -or $output -match '@id'
    Write-TestResult "Docker (default network) can reach boltnuget.boltqa.com" $success
    if (-not $success) {
        Write-Host "  Output: $output" -ForegroundColor DarkGray
    }
} catch {
    Write-TestResult "Docker (default network) can reach private feed" $false $_.Exception.Message
}

# Test 4: Can Docker reach private feed with host network?
Write-Host "`nTest 4: Docker host network to private feed..." -ForegroundColor Yellow
try {
    $output = docker run --rm --network=host mcr.microsoft.com/dotnet/sdk:9.0 pwsh -c "curl -s -m 10 https://boltnuget.boltqa.com/ 2>&1"
    $success = $output -match '"version"' -or $output -match '@id'
    Write-TestResult "Docker (--network=host) can reach boltnuget.boltqa.com" $success
    
    if ($success) {
        Write-Host "`n✓ SUCCESS: Docker with --network=host can access the private feed!" -ForegroundColor Green
        Write-Host "  The build script is configured to use --network=host" -ForegroundColor Green
        Write-Host "  You can now run: .\build-docker-image.ps1" -ForegroundColor Cyan
    } else {
        Write-Host "  Output: $output" -ForegroundColor DarkGray
    }
} catch {
    Write-TestResult "Docker (host network) can reach private feed" $false $_.Exception.Message
}

# Summary
Write-Host "`n==> Summary" -ForegroundColor Cyan
Write-Host ""
Write-Host "If Test 4 passed: Run .\build-docker-image.ps1 (already uses --network=host)" -ForegroundColor Green
Write-Host "If Test 4 failed: See VPN-TROUBLESHOOTING.md for solutions" -ForegroundColor Yellow
Write-Host ""
