@echo off
echo === Sphere51a-ModernUO Baseline Verification ===
powershell -ExecutionPolicy Bypass -File "scripts/verify-baseline.ps1" %*
pause
