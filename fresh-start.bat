@echo off
echo === Sphere51a-ModernUO Fresh Start ===
echo This will reset the repository to baseline state and clean all artifacts
echo.
set /p confirm="Are you sure? This will delete uncommitted changes! (y/N): "
if /i not "%confirm%"=="y" goto :cancel

echo.
echo Resetting to baseline commit...
git reset --hard f51ff3f37518a853d99ecbb409827e03d8b50500
if errorlevel 1 (
    echo Error: Failed to reset to baseline
    pause
    exit /b 1
)

echo.
echo Running cleanup...
call cleanup.bat

echo.
echo Fresh start complete!
echo Repository is now at clean baseline state.
pause
goto :end

:cancel
echo Operation cancelled.
pause

:end
