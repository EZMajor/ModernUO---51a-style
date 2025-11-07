@echo off
echo === Sphere51a-ModernUO Cleanup ===
powershell -ExecutionPolicy Bypass -File "scripts/cleanup.ps1" %*
pause
