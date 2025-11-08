@echo off
echo ========================================
echo Sphere51a-ModernUO Runtime Cleanup
echo ========================================
echo.
echo This will remove runtime artifacts but KEEP configuration files.
echo Your server settings will be preserved.
echo.
pause
echo.

powershell -ExecutionPolicy Bypass -File "%~dp0runtime-cleanup.ps1"

echo.
pause
