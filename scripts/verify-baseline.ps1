param([string]$BaselineCommit = "f51ff3f37518a853d99ecbb409827e03d8b50500")

Write-Host "=== Sphere51a-ModernUO Baseline Verification ===" -ForegroundColor Cyan
Write-Host "Baseline commit: $BaselineCommit" -ForegroundColor Yellow
Write-Host ""

if (-not (Test-Path ".git")) {
    Write-Host "Error: Not in a git repository!" -ForegroundColor Red
    exit 1
}

$currentBranch = git branch --show-current
Write-Host "Current branch: $currentBranch" -ForegroundColor Green

$status = git status --porcelain
if ($status) {
    Write-Host "Warning: You have uncommitted changes!" -ForegroundColor Yellow
    Write-Host $status
    Write-Host ""
}

Write-Host "Checking files against baseline..." -ForegroundColor Yellow
$changedFiles = git diff --name-only $BaselineCommit
$newFiles = git ls-files --others --exclude-standard
$allExtraFiles = ($changedFiles + $newFiles) | Where-Object { $_ -ne "" } | Sort-Object -Unique

if ($allExtraFiles.Count -eq 0) {
    Write-Host "Repository is clean - matches baseline!" -ForegroundColor Green
    exit 0
}

Write-Host "Found $($allExtraFiles.Count) files that differ from baseline:" -ForegroundColor Red
$allExtraFiles | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }

Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Total extra files: $($allExtraFiles.Count)" -ForegroundColor Yellow

Write-Host ""
Write-Host "Recommendations:" -ForegroundColor Cyan
Write-Host "  Run 'scripts/cleanup.ps1' to remove common artifacts" -ForegroundColor Green
Write-Host "  Review files manually before committing" -ForegroundColor Yellow

exit 1
