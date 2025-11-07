param(
    [string]$BaselineCommit = "f51ff3f37518a853d99ecbb409827e03d8b50500"
)

Write-Host "=== Sphere51a-ModernUO Repository Cleanup Script ===" -ForegroundColor Cyan
Write-Host "Baseline commit: $BaselineCommit" -ForegroundColor Yellow
Write-Host ""

# Check if we're in a git repository
if (-not (Test-Path ".git")) {
    Write-Host "Error: Not in a git repository!" -ForegroundColor Red
    exit 1
}

# Check current branch
$currentBranch = git branch --show-current
Write-Host "Current branch: $currentBranch" -ForegroundColor Green

# Get list of files that differ from baseline
Write-Host "Analyzing files compared to baseline..." -ForegroundColor Yellow
$changedFiles = git diff --name-only $BaselineCommit
$newFiles = git ls-files --others --exclude-standard
$allExtraFiles = $changedFiles + $newFiles | Where-Object { $_ -ne "" } | Sort-Object -Unique

if ($allExtraFiles.Count -eq 0) {
    Write-Host "Repository is clean - no extra files found!" -ForegroundColor Green
    exit 0
}

Write-Host "Found $($allExtraFiles.Count) files that differ from baseline:" -ForegroundColor Yellow
$allExtraFiles | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }

# Define patterns for files that should be removed
$removePatterns = @(
    # Build artifacts
    "*/bin/*",
    "*/obj/*",
    "*.dll",
    "*.exe",
    "*.pdb",
    # Logs
    "*.log",
    "Logs/*",
    "logs/*",
    # Runtime data
    "Saves/*",
    "saves/*",
    "Accounts/*",
    "Backups/*",
    "Data/*.db",
    "Data/*.sqlite*",
    # Temp files
    "*.tmp",
    "*.temp",
    "*.bak",
    "*.swp",
    # OS files
    ".DS_Store",
    "Thumbs.db",
    "desktop.ini"
)

$filesToRemove = @()
$filesToReview = @()

foreach ($file in $allExtraFiles) {
    $shouldRemove = $false
    foreach ($pattern in $removePatterns) {
        if ($file -like $pattern) {
            $shouldRemove = $true
            break
        }
    }

    if ($shouldRemove) {
        $filesToRemove += $file
    } else {
        $filesToReview += $file
    }
}

Write-Host ""
Write-Host "Files to automatically remove ($($filesToRemove.Count)):" -ForegroundColor Red
$filesToRemove | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }

if ($filesToReview.Count -gt 0) {
    Write-Host ""
    Write-Host "Files requiring manual review ($($filesToReview.Count)):" -ForegroundColor Yellow
    $filesToReview | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
}

# Ask for confirmation
Write-Host ""
$confirmation = Read-Host "Proceed with automatic cleanup? (y/N)"
if ($confirmation -ne "y" -and $confirmation -ne "Y") {
    Write-Host "Cleanup cancelled." -ForegroundColor Yellow
    exit 0
}

# Remove files
$removedCount = 0
foreach ($file in $filesToRemove) {
    if (Test-Path $file) {
        try {
            Remove-Item $file -Force -Recurse
            Write-Host "Removed: $file" -ForegroundColor Green
            $removedCount++
        } catch {
            Write-Host "Failed to remove: $file ($($_.Exception.Message))" -ForegroundColor Red
        }
    }
}

# Reset modified files that match patterns
foreach ($file in $changedFiles) {
    $shouldReset = $false
    foreach ($pattern in $removePatterns) {
        if ($file -like $pattern) {
            $shouldReset = $true
            break
        }
    }

    if ($shouldReset -and (Test-Path $file)) {
        try {
            git checkout $BaselineCommit -- $file
            Write-Host "Reset: $file" -ForegroundColor Green
            $removedCount++
        } catch {
            Write-Host "Failed to reset: $file ($($_.Exception.Message))" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "Cleanup complete! Removed/reset $removedCount files." -ForegroundColor Green

if ($filesToReview.Count -gt 0) {
    Write-Host ""
    Write-Host "Manual review required for $($filesToReview.Count) files:" -ForegroundColor Yellow
    $filesToReview | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
    Write-Host "Use 'git diff $BaselineCommit -- <file>' to see changes" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "To verify repository is clean, run: scripts/verify-baseline.ps1" -ForegroundColor Cyan
