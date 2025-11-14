# Sphere51a Testing Framework Troubleshooting Guide

This guide helps diagnose and resolve common issues with the Sphere51a testing framework.

## Quick Diagnosis

### Check Test Status

```bash
# View latest test results
cat Distribution/AuditReports/Latest_Weapon\ Swing\ Timing\ TestSummary.md

# Check for recent logs
ls -la Distribution/AuditReports/Logs/

# Look for recovery files (indicates interruptions)
ls -la Distribution/AuditReports/Recovery/
```

### Common Symptoms & Solutions

| Symptom | Likely Cause | Quick Fix |
|---------|-------------|-----------|
| "No timing measurements collected" | Events not firing | Check Sphere51a initialization |
| Test times out after 10+ minutes | Infinite loop/deadlock | Use `--duration 15` and check logs |
| High timing variance (>50ms) | System load/interference | Run on dedicated machine |
| Build fails | Missing dependencies | `dotnet restore` then rebuild |
| "Could not find ModernUO.dll" | Not built | `dotnet build Projects/Application/Application.csproj` |

## Detailed Issue Resolution

### 1. Test Fails: "No timing measurements collected"

**Symptoms:**
```
Status: **FAILED**
Total Swings: 0
Accuracy: 0.0%
```

**Root Causes:**
- Sphere51a module not initialized
- Combat events not firing
- Test mobiles improperly configured
- Missing `--quick-test` or `--test-mode` flag

**Diagnostic Steps:**

1. **Check Sphere51a Initialization**
   ```bash
   grep -i "sphere51a" Distribution/AuditReports/Logs/test-run-*.log
   ```
   Expected: `"Initializing Sphere51a module..."`

2. **Verify Test Mode**
   ```bash
   grep -i "test.mode\|quick.test" Distribution/AuditReports/Logs/test-run-*.log
   ```
   Expected: Test mode indicators

3. **Check Combat Events**
   ```bash
   grep -i "swing\|combat" Distribution/AuditReports/Logs/test-run-*.log
   ```
   Expected: Weapon swing event logs

**Solutions:**

- **Missing Test Flag:** Use `--quick-test` or `--test-mode`
- **Sphere51a Disabled:** Check `modernuo.json` has `"sphere51a.enabled": "true"`
- **Module Load Failure:** Check for assembly loading errors in logs

### 2. Test Execution Times Out

**Symptoms:**
- Process killed after 10 minutes
- `timeout-{timestamp}.txt` file created
- No test results

**Diagnostic Steps:**

1. **Check Timeout File**
   ```bash
   cat Distribution/AuditReports/Recovery/timeout-*.txt
   ```

2. **Review Recent Logs**
   ```bash
   tail -50 Distribution/AuditReports/Logs/test-run-*.log
   ```

3. **Check System Resources**
   ```bash
   # Windows
   Get-Process | Sort-Object CPU -Descending | Select-Object -First 5

   # Linux
   top -b -n1 | head -20
   ```

**Common Causes:**
- Infinite loop in weapon swing timing
- Deadlock in Sphere51a combat system
- Memory exhaustion preventing completion
- File I/O blocking on slow storage

**Solutions:**

1. **Reduce Test Scope**
   ```bash
   # Shorter duration
   ./run_test.sh --quick --duration 15

   # Single weapon test (modify test code temporarily)
   ```

2. **Check for Memory Issues**
   ```bash
   # Monitor memory usage
   dotnet run --project Projects/Application/Application.csproj -- --quick-test --scenario weapon_timing --duration 10
   ```

3. **Isolate Problem Code**
   - Comment out sections of `PerformWeaponSwing()`
   - Test with minimal weapon/dex combinations
   - Check for exceptions in logs

### 3. High Timing Variance (>50ms outliers)

**Symptoms:**
```
Accuracy: 85.2% (Target: ≥95%)
Outliers: 15.3% with >50ms variance
```

**Diagnostic Steps:**

1. **Check System Load**
   ```bash
   # Windows
   Get-Counter '\Processor(_Total)\% Processor Time' -SampleInterval 1 -MaxSamples 5

   # Linux
   uptime && vmstat 1 5
   ```

2. **Review Timing Distribution**
   ```bash
   # Check raw data for patterns
   grep -o '"varianceMs":[0-9.]*' Distribution/AuditReports/*_raw.json | sort -n
   ```

3. **Compare with Baseline**
   ```bash
   # Check if this is a regression
   git log --oneline -10 --grep="baseline\|timing"
   ```

**Common Causes:**
- Background processes consuming CPU
- Timer resolution issues on virtual machines
- Anti-virus interference
- Power management affecting timers

**Solutions:**

1. **Optimize Environment**
   ```bash
   # Close unnecessary applications
   # Disable anti-virus real-time scanning
   # Use high-performance power plan
   ```

2. **Use Quick Mode**
   ```bash
   # More consistent timing
   ./run_test.sh --quick --duration 30 --verbose
   ```

3. **Run Multiple Times**
   ```bash
   # Average results over several runs
   for i in {1..3}; do
     ./run_test.sh --quick --duration 20
     sleep 5
   done
   ```

### 4. Build Failures

**Symptoms:**
- `dotnet build` fails
- Missing references or dependencies
- Compilation errors

**Diagnostic Steps:**

1. **Check Build Output**
   ```bash
   dotnet build Projects/Application/Application.csproj --verbosity detailed
   ```

2. **Verify Dependencies**
   ```bash
   dotnet restore
   dotnet list package --outdated
   ```

3. **Check Project References**
   ```bash
   # Verify all project files exist
   ls -la Projects/*/ *.csproj
   ```

**Solutions:**

1. **Clean Build**
   ```bash
   dotnet clean
   dotnet restore
   dotnet build
   ```

2. **Update Dependencies**
   ```bash
   dotnet list package --outdated
   dotnet update package
   ```

3. **Check File Permissions**
   ```bash
   # Ensure write access to Distribution/
   ls -la Distribution/
   ```

### 5. Log Files Not Generated

**Symptoms:**
- No files in `Distribution/AuditReports/Logs/`
- Console output truncated

**Diagnostic Steps:**

1. **Check Directory Permissions**
   ```bash
   ls -la Distribution/AuditReports/
   mkdir -p Distribution/AuditReports/Logs
   ```

2. **Verify Logging Configuration**
   ```bash
   grep -i "log" Projects/Logger/LogFactory.cs
   ```

3. **Test File Creation**
   ```bash
   echo "test" > Distribution/AuditReports/Logs/test.txt
   ```

**Solutions:**

- Ensure `--verbose` flag is used
- Check available disk space
- Verify test mode is properly activated

### 6. Recovery Files Accumulating

**Symptoms:**
- Many files in `Distribution/AuditReports/Recovery/`
- Test interruptions common

**Diagnostic Steps:**

1. **Check Recovery Files**
   ```bash
   ls -la Distribution/AuditReports/Recovery/
   find Distribution/AuditReports/Recovery/ -name "*.json" -exec wc -l {} \;
   ```

2. **Analyze Patterns**
   ```bash
   # Group by date
   ls Distribution/AuditReports/Recovery/ | cut -d- -f2 | sort | uniq -c
   ```

**Solutions:**

1. **Clean Old Files**
   ```bash
   # Remove files older than 7 days
   find Distribution/AuditReports/Recovery/ -mtime +7 -delete
   ```

2. **Address Root Cause**
   - Fix timeout issues
   - Improve system stability
   - Use shorter test durations

## Advanced Diagnostics

### Performance Profiling

```bash
# Use TestRunner with profiling
dotnet run --project Projects/TestRunner/TestRunner.csproj -- --profile --scenario weapon_timing --quick --duration 20

# Profile memory usage
dotnet-trace collect --process-id $PID --providers Microsoft-DotNETCore-SampleProfiler

# Analyze with Visual Studio or dotTrace
```

### Network Diagnostics (if applicable)

```bash
# Check for network interference
ping -t 127.0.0.1

# Disable network adapters temporarily
# (Windows: Device Manager, Linux: ifconfig down)
```

### Assembly Loading Issues

```bash
# Check assembly resolution
MONO_LOG_LEVEL=debug dotnet run --project Projects/Application/Application.csproj -- --quick-test --scenario weapon_timing 2>&1 | grep -i assembly

# Verify assemblies exist
ls -la Distribution/Assemblies/
```

### Combat System Debugging

```bash
# Enable Sphere51a debug logging (if available)
# Check for combat system initialization
grep -i "combat\|sphere" Distribution/AuditReports/Logs/*.log

# Verify weapon timing provider
grep -i "weapontiming\|provider" Distribution/AuditReports/Logs/*.log
```

## Environment-Specific Issues

### Windows

**Issue:** High CPU usage during tests
**Solution:** Disable Windows Defender real-time protection for test directory

**Issue:** File locking errors
**Solution:** Close Visual Studio and other file watchers

### Linux/macOS

**Issue:** Permission denied on log files
**Solution:**
```bash
chmod 755 Distribution/
chmod 755 Distribution/AuditReports/
chmod 755 Distribution/AuditReports/Logs/
```

**Issue:** Timer resolution issues
**Solution:** Use real-time scheduling if available

### Virtual Machines

**Issue:** Inconsistent timing
**Solutions:**
- Use host passthrough for timers
- Disable dynamic CPU allocation
- Run on dedicated CPU cores

## Getting Help

### Information to Include

When reporting issues, provide:

1. **System Information**
   ```bash
   uname -a  # Linux/macOS
   systeminfo | findstr /B /C:"OS"  # Windows
   dotnet --info
   ```

2. **Test Command & Output**
   ```bash
   # Exact command used
   ./run_test.sh --quick --duration 30 --verbose

   # Last 50 lines of output
   tail -50 Distribution/AuditReports/Logs/test-run-*.log
   ```

3. **Test Results**
   ```bash
   cat Distribution/AuditReports/Latest_Weapon\ Swing\ Timing\ TestSummary.md
   ```

4. **Environment Details**
   - CPU/memory usage during test
   - Background processes
   - Anti-virus/firewall status

### Escalation Path

1. **Check Documentation:** This guide and `docs/testing.md`
2. **Search Issues:** GitHub issues with similar symptoms
3. **Gather Diagnostics:** Run with `--verbose` and collect all logs
4. **Create Issue:** Include system info, commands, and full logs

### Emergency Workarounds

**Test Completely Broken:**
```bash
# Skip automated tests temporarily
# Run manual validation
dotnet run --project Projects/Application/Application.csproj -- --quick-test --scenario weapon_timing --duration 5
```

**CI/CD Failing:**
```bash
# Temporarily disable weapon timing test in CI
# Comment out the test step in .github/workflows/
```

**Development Blocked:**
```bash
# Use minimal reproduction case
dotnet run --project Projects/Application/Application.csproj -- --quick-test --scenario weapon_timing --duration 1
```

## Prevention

### Regular Maintenance

- **Weekly:** Clear old recovery files
- **Monthly:** Update baselines, review performance trends
- **Quarterly:** Full test suite audit

### Best Practices

- Always use `--quick` mode for development
- Run tests on dedicated hardware when possible
- Keep system updated and free of background processes
- Monitor test execution times for regressions
- Archive successful test runs for comparison

### Monitoring

Set up alerts for:
- Test failure rate >5%
- Average execution time >2x baseline
- Memory usage >1GB during tests
- Log file size >100MB
