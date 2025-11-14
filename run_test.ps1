#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Sphere51a Test Runner Script (Windows)
.DESCRIPTION
    Runs Sphere51a weapon timing tests with proper error handling and logging.
.PARAMETER Scenario
    Test scenario to run (default: weapon_timing)
.PARAMETER Duration
    Test duration in seconds (default: 30)
.PARAMETER Quick
    Use quick test mode for faster execution
.PARAMETER Verbose
    Enable verbose logging
.EXAMPLE
    .\run_test.ps1 -Quick -Duration 15
#>

param(
    [string]$Scenario = "weapon_timing",
    [int]$Duration = 30,
    [switch]$Quick,
    [switch]$Verbose
)

# Script configuration
$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Join-Path $scriptDir "Projects\Application"
$logDir = Join-Path $scriptDir "Distribution\AuditReports\Logs"

# Ensure log directory exists
if (!(Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
}

# Build arguments
$args = @("--test-mode", "--scenario", $Scenario, "--duration", $Duration.ToString())

if ($Quick) {
    $args = @("--quick-test", "--scenario", $Scenario, "--duration", $Duration.ToString())
}

if ($Verbose) {
    $args += "--verbose"
}

# Display execution info
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Sphere51a Test Runner (Windows)" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Scenario: $Scenario" -ForegroundColor Yellow
Write-Host "Duration: $Duration seconds" -ForegroundColor Yellow
Write-Host "Mode: $(if ($Quick) { 'Quick' } else { 'Standard' })" -ForegroundColor Yellow
Write-Host "Verbose: $Verbose" -ForegroundColor Yellow
Write-Host "Project: $projectDir" -ForegroundColor Yellow
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Change to project directory
Push-Location $projectDir

try {
    # Build the project
    Write-Host "Building project..." -ForegroundColor Green
    $buildResult = dotnet build --configuration Release --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }

    # Run the test
    Write-Host "Running test..." -ForegroundColor Green
    Write-Host "Command: dotnet run --project . $($args -join ' ')" -ForegroundColor Gray

    # Execute with output capture for error handling
    $startTime = Get-Date
    dotnet run --project . @args
    $exitCode = $LASTEXITCODE
    $endTime = Get-Date

    # Display results
    $duration = $endTime - $startTime
    Write-Host ""
    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host "  Test Execution Complete" -ForegroundColor Cyan
    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host "Exit Code: $exitCode" -ForegroundColor $(if ($exitCode -eq 0) { 'Green' } else { 'Red' })
    Write-Host "Duration: $($duration.TotalSeconds.ToString('F1')) seconds" -ForegroundColor Yellow

    # Show recent log files
    $recentLogs = Get-ChildItem $logDir -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 3
    if ($recentLogs) {
        Write-Host ""
        Write-Host "Recent Log Files:" -ForegroundColor Yellow
        foreach ($log in $recentLogs) {
            Write-Host "  $($log.FullName)" -ForegroundColor Gray
        }
    }

    # Show recent reports
    $reportDir = Join-Path $scriptDir "Distribution\AuditReports"
    $recentReports = Get-ChildItem $reportDir -Filter "Latest_*.md" | Sort-Object LastWriteTime -Descending
    if ($recentReports) {
        Write-Host ""
        Write-Host "Latest Reports:" -ForegroundColor Yellow
        foreach ($report in $recentReports) {
            Write-Host "  $($report.FullName)" -ForegroundColor Gray
        }
    }

    exit $exitCode

} catch {
    Write-Error "Test execution failed: $($_.Exception.Message)"
    exit 1
} finally {
    Pop-Location
}
