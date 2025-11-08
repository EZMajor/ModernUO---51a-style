Write-Host "=== Sphere51a-ModernUO Runtime Cleanup ===" -ForegroundColor Cyan
Write-Host "Removing runtime artifacts from server operation..." -ForegroundColor Yellow
Write-Host ""

# Define runtime artifact patterns
$runtimePatterns = @(
    # Log files
    "*.log",
    "Logs/*",
    "logs/*",
    # Save files
    "Saves/*",
    "saves/*",
    "Accounts/*",
    "Backups/*",
    # Temp files
    "*.tmp",
    "*.temp",
    "*.bak",
    "*.swp",
    "*.pid",
    "*.lock",
    # OS files
    ".DS_Store",
    "Thumbs.db",
    "desktop.ini",
    # Database files
    "*.db",
    "*.sqlite",
    "*.sqlite3"
)

$removedCount = 0

foreach ($pattern in $runtimePatterns) {
    try {
        $files = Get-ChildItem -Path . -Filter $pattern -Recurse -File -ErrorAction SilentlyContinue
        foreach ($file in $files) {
            if ($file.FullName -notmatch "node_modules|\\.git|Distribution\\Assemblies") {
                Remove-Item $file.FullName -Force
                Write-Host "Removed: $($file.FullName)" -ForegroundColor Green
                $removedCount++
            }
        }
    } catch {
        # Ignore errors for patterns that don't match
    }
}

# Also clean specific directories that get created
$dirsToClean = @(
    "Distribution/Logs",
    "Distribution/Saves",
    "Distribution/Accounts",
    "Distribution/Backups"
)

foreach ($dir in $dirsToClean) {
    if (Test-Path $dir) {
        Remove-Item $dir -Recurse -Force
        Write-Host "Removed directory: $dir" -ForegroundColor Green
        $removedCount++
    }
}

Write-Host ""
Write-Host "Runtime cleanup complete! Removed $removedCount items." -ForegroundColor Green
Write-Host "Repository is ready for next development cycle." -ForegroundColor Cyan
