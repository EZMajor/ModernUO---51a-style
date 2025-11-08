@echo off
echo ========================================
echo Sphere51a-ModernUO FULL Cleanup
echo ========================================
echo.
echo This will remove ALL runtime artifacts and configuration files.
echo After running this, your server will start fresh with all prompts.
echo.
pause
echo.

powershell -ExecutionPolicy Bypass -File "%~dp0full-cleanup.ps1"

echo.
pause
