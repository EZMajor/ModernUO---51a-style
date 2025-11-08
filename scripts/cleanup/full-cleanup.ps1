Write-Host "=== Sphere51a-ModernUO Full Cleanup ===" -ForegroundColor Cyan
Write-Host "This script removes ALL runtime artifacts and configuration files." -ForegroundColor Yellow
Write-Host "After running this, the server will start fresh with all prompts." -ForegroundColor Yellow
Write-Host ""

$removedCount = 0

# ============================================================================
# SECTION 1: Configuration Files (Always Clean - Triggers Fresh Prompts)
# ============================================================================

Write-Host "[1/5] Cleaning configuration files..." -ForegroundColor Cyan

$configFilesToClean = @(
    "Configuration/modernuo.json",
    "Configuration/expansion.json",
    "Configuration/email-settings.json",
    "Distribution/Configuration/modernuo.json",
    "Distribution/Configuration/expansion.json",
    "Distribution/Configuration/email-settings.json",
    "Projects/UOContent/Modules/Sphere51a/Configuration/config.json",
    "Distribution/Projects/UOContent/Modules/Sphere51a/Configuration/config.json"
)

foreach ($configFile in $configFilesToClean) {
    if (Test-Path $configFile) {
        Remove-Item $configFile -Force -ErrorAction SilentlyContinue
        Write-Host "  Removed: $configFile" -ForegroundColor Green
        $removedCount++
    }
}

# ============================================================================
# SECTION 2: Runtime Data (Saves, Accounts, Logs)
# ============================================================================

Write-Host ""
Write-Host "[2/5] Cleaning runtime data directories..." -ForegroundColor Cyan

$dataDirsToClean = @(
    "Distribution/Logs",
    "Distribution/Saves",
    "Distribution/Accounts",
    "Distribution/Backups",
    "Logs",
    "Saves",
    "Accounts",
    "Backups"
)

foreach ($dir in $dataDirsToClean) {
    if (Test-Path $dir) {
        Remove-Item $dir -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  Removed directory: $dir" -ForegroundColor Green
        $removedCount++
    }
}

# ============================================================================
# SECTION 3: Runtime Files (Logs, Temp Files, Database Files)
# ============================================================================

Write-Host ""
Write-Host "[3/5] Cleaning runtime files..." -ForegroundColor Cyan

$runtimeFilePatterns = @(
    "*.log",
    "*.tmp",
    "*.temp",
    "*.bak",
    "*.swp",
    "*.pid",
    "*.lock",
    "*.db",
    "*.sqlite",
    "*.sqlite3",
    ".DS_Store",
    "Thumbs.db",
    "desktop.ini"
)

foreach ($pattern in $runtimeFilePatterns) {
    try {
        $files = Get-ChildItem -Path . -Filter $pattern -Recurse -File -ErrorAction SilentlyContinue
        foreach ($file in $files) {
            # Skip files in .git, node_modules, or Distribution/Assemblies
            if ($file.FullName -notmatch "\.git|node_modules|Distribution\\Assemblies") {
                Remove-Item $file.FullName -Force -ErrorAction SilentlyContinue
                Write-Host "  Removed: $($file.Name)" -ForegroundColor Green
                $removedCount++
            }
        }
    } catch {
        # Ignore errors for patterns that don't match
    }
}

# ============================================================================
# SECTION 4: Distribution Assemblies (Built Binaries)
# ============================================================================

Write-Host ""
Write-Host "[4/5] Cleaning Distribution assemblies..." -ForegroundColor Cyan

$distDirsToClean = @(
    "Distribution/Assemblies"
    # NOTE: Distribution/Data contains essential game data files (expansions.json, skills.json,
    # regions.json, spawns, decorations, etc.) that are part of the baseline commit and are
    # REQUIRED for ModernUO to start. These files must NEVER be deleted.
)

foreach ($dir in $distDirsToClean) {
    if (Test-Path $dir) {
        Remove-Item $dir -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  Removed directory: $dir" -ForegroundColor Green
        $removedCount++
    }
}

# Also clean specific Distribution files
$distFilesToClean = @(
    "Distribution/*.dll",
    "Distribution/*.exe",
    "Distribution/*.pdb",
    "Distribution/*.json",
    "Distribution/*.config"
)

foreach ($pattern in $distFilesToClean) {
    try {
        $files = Get-ChildItem -Path $pattern -ErrorAction SilentlyContinue
        foreach ($file in $files) {
            Remove-Item $file.FullName -Force -ErrorAction SilentlyContinue
            Write-Host "  Removed: $($file.Name)" -ForegroundColor Green
            $removedCount++
        }
    } catch {
        # Ignore errors
    }
}

# ============================================================================
# SECTION 5: Build Artifacts (Optional - Requires Rebuild)
# ============================================================================

Write-Host ""
$cleanBuild = Read-Host "[5/5] Clean ALL build artifacts (bin/obj)? This requires full rebuild. (y/N)"

if ($cleanBuild -eq "y" -or $cleanBuild -eq "Y") {
    Write-Host "Cleaning build artifacts..." -ForegroundColor Yellow

    # Find all bin and obj directories
    $buildDirs = Get-ChildItem -Path "Projects" -Directory -Recurse -ErrorAction SilentlyContinue | Where-Object {
        $_.Name -eq "bin" -or $_.Name -eq "obj"
    }

    foreach ($dir in $buildDirs) {
        Remove-Item $dir.FullName -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  Removed: $($dir.FullName)" -ForegroundColor Green
        $removedCount++
    }
}

# ============================================================================
# SUMMARY
# ============================================================================

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Cleanup Complete!" -ForegroundColor Green
Write-Host "Removed $removedCount items." -ForegroundColor Green
Write-Host ""

if ($cleanBuild -eq "y" -or $cleanBuild -eq "Y") {
    Write-Host "Status: Ready for CLEAN REBUILD" -ForegroundColor Yellow
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Run: dotnet build" -ForegroundColor White
    Write-Host "  2. Run: cd Distribution && dotnet ModernUO.dll" -ForegroundColor White
    Write-Host "  3. Complete all setup prompts (Data Path, IP, Expansion, Maps, Sphere51a)" -ForegroundColor White
} else {
    Write-Host "Status: Ready for FRESH START" -ForegroundColor Yellow
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Run: cd Distribution && dotnet ModernUO.dll" -ForegroundColor White
    Write-Host "  2. Complete all setup prompts (Data Path, IP, Expansion, Maps, Sphere51a)" -ForegroundColor White
}

Write-Host ""
Write-Host "All configuration prompts will appear on next startup!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
