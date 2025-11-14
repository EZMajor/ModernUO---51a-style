# Sphere51a Testing Framework

This document provides comprehensive guidance for running and maintaining the Sphere51a testing framework.

## Quick Start

### Running Tests

#### Using the Wrapper Scripts (Recommended)

**Windows:**
```powershell
.\run_test.ps1 -Quick -Duration 30 -Verbose
```

**Linux/macOS:**
```bash
./run_test.sh --quick --duration 30 --verbose
```

#### Using the Test Runner Utility

```bash
# Build the test runner
dotnet build Projects/TestRunner/TestRunner.csproj

# Run tests
dotnet run --project Projects/TestRunner/TestRunner.csproj -- --scenario weapon_timing --quick --duration 30

# List available scenarios
dotnet run --project Projects/TestRunner/TestRunner.csproj -- --list

# Show help
dotnet run --project Projects/TestRunner/TestRunner.csproj -- --help
```

#### Direct Application Execution

```bash
# Quick test mode (recommended for development)
dotnet run --project Projects/Application/Application.csproj -- --quick-test --scenario weapon_timing --duration 30 --verbose

# Standard test mode (full server initialization)
dotnet run --project Projects/Application/Application.csproj -- --test-mode --scenario weapon_timing --duration 60
```

## Test Scenarios

### Weapon Swing Timing Test

**Scenario ID:** `weapon_timing`

**Purpose:** Validates weapon swing timing accuracy across different weapon types and dexterity values.

**Parameters:**
- **Weapons:** Katana, Longsword, Broadsword, Dagger, WarAxe
- **Dexterity Values:** 25, 50, 75, 100, 150
- **Target Accuracy:** ≥95% within ±25ms variance
- **Default Duration:** 30 seconds

**Expected Results:**
- ✅ **Status:** PASSED
- ✅ **Accuracy:** ≥95%
- ✅ **Total Swings:** 100+ (depending on duration)
- ✅ **Outliers:** ≤5% with >50ms variance

### Other Scenarios

- `spell_timing` - Spell casting timing validation (not yet implemented)
- `stress_test` - Combat system load testing (not yet implemented)

## Command Line Options

### Global Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--scenario <name>` | `-s` | Test scenario to run | `weapon_timing` |
| `--duration <secs>` | `-d` | Test duration in seconds | `30` |
| `--quick-test` | | Use minimal server initialization | |
| `--test-mode` | `-t` | Use full server initialization | |
| `--verbose` | `-v` | Enable detailed logging | `false` |
| `--auto-exit` | | Exit immediately after test completion | `true` |

### Test Runner Utility Options

| Option | Short | Description |
|--------|-------|-------------|
| `--list` | `-l` | List available test scenarios |
| `--profile` | `-p` | Enable performance profiling |
| `--help` | `-h` | Show help information |

## Test Output

### Directory Structure

```
Distribution/
├── AuditReports/
│   ├── Latest_Weapon Swing Timing TestSummary.md    # Main summary report
│   ├── 2025-11-09_145011_Weapon Swing Timing Test.md # Detailed markdown report
│   ├── 2025-11-09_145011_Weapon Swing Timing Test_raw.json # Raw JSON data
│   ├── Logs/                                         # Test execution logs
│   │   └── test-run-2025-11-09-14-50-11.log
│   └── Recovery/                                     # Recovery files for interrupted tests
│       └── incremental-results-2025-11-09-14-50-11.json
```

### Log Files

Test execution creates timestamped log files containing:
- Full console output
- Test framework initialization details
- Performance metrics
- Error messages and stack traces

### Recovery Files

When tests are interrupted, incremental results are saved to prevent data loss:
- `incremental-results-{timestamp}.json` - Partial measurement data
- `timeout-{timestamp}.txt` - Timeout diagnostic information
- `crash-{timestamp}.txt` - Crash diagnostic information

## Performance Optimization

### Quick Test Mode

Use `--quick-test` for development and CI:

**Benefits:**
- ⚡ **Startup:** <5 seconds (vs ~30s standard)
- 💾 **Memory:** Minimal footprint
- 🔄 **Caching:** Subsequent runs in ≤3 seconds
- 📊 **Reliability:** Reduced failure points

**When to Use:**
- Local development
- CI/CD pipelines
- Quick validation
- Performance testing

### Standard Test Mode

Use `--test-mode` for comprehensive testing:

**Benefits:**
- 🏗️ **Complete Environment:** Full server initialization
- 🎯 **Accuracy:** Most realistic test conditions
- 🔍 **Coverage:** Tests integration with all systems

**When to Use:**
- Final validation
- Production-like testing
- Benchmarking

## CI/CD Integration

### GitHub Actions

The framework includes automated CI/CD validation:

**Trigger Conditions:**
- Push to `main` or `develop` branches
- Pull requests affecting test-related files
- Changes to Sphere51a modules, server, or application code

**CI Pipeline:**
1. **Build:** Compile all projects with Release configuration
2. **Test:** Run weapon timing test with `--quick-test --duration 30`
3. **Validate:** Check test results and performance metrics
4. **Report:** Upload logs and reports as artifacts
5. **Comment:** Post results summary on pull requests

**Artifact Retention:** 30 days

### Local CI Simulation

```bash
# Simulate CI pipeline locally
dotnet build Projects/Application/Application.csproj --configuration Release
./run_test.sh --quick --duration 30 --verbose

# Check results
cat Distribution/AuditReports/Latest_Weapon\ Swing\ Timing\ TestSummary.md
```

## Baseline Management

### Understanding Baselines

Baselines define expected performance thresholds:

```json
{
  "weapon_swing_timing": {
    "accuracy_percent": 98.5,
    "max_variance_ms": 15.0,
    "last_updated": "2025-11-09",
    "last_build": "v0.15.0"
  }
}
```

### Updating Baselines

```bash
# Run test with baseline update
dotnet run --project Projects/Application/Application.csproj -- --quick-test --scenario weapon_timing --update-baselines
```

## Troubleshooting

### Common Issues

#### Test Fails with "No timing measurements collected"

**Symptoms:**
- Status: FAILED
- Total Swings: 0
- No measurement data

**Causes:**
- Combat events not firing
- Sphere51a module not initialized
- Test mobiles not properly configured

**Solutions:**
1. Check logs for Sphere51a initialization errors
2. Verify `--quick-test` or `--test-mode` flag usage
3. Ensure test configuration is valid

#### Test Times Out

**Symptoms:**
- Process killed after 10+ minutes
- Timeout diagnostic file created

**Causes:**
- Infinite loop in test code
- Deadlock in combat system
- Memory exhaustion

**Solutions:**
1. Check timeout diagnostic file in `Recovery/`
2. Reduce test duration with `--duration 15`
3. Run with `--verbose` for detailed logging
4. Check system resources (memory, CPU)

#### High Variance in Timing Results

**Symptoms:**
- Accuracy < 95%
- Many outliers >50ms variance

**Causes:**
- System under load
- Timer resolution issues
- Background processes interfering

**Solutions:**
1. Run on dedicated machine
2. Close unnecessary applications
3. Use `--quick` mode for more consistent timing
4. Run multiple times and average results

### Diagnostic Tools

#### Log Analysis

```bash
# View recent test logs
ls -la Distribution/AuditReports/Logs/
tail -f Distribution/AuditReports/Logs/test-run-*.log
```

#### Recovery File Inspection

```bash
# Check for interrupted test data
ls -la Distribution/AuditReports/Recovery/
cat Distribution/AuditReports/Recovery/incremental-results-*.json | jq .
```

#### Performance Profiling

```bash
# Use test runner with profiling
dotnet run --project Projects/TestRunner/TestRunner.csproj -- --profile --scenario weapon_timing --quick
```

## Development Workflow

### Adding New Test Scenarios

1. **Create Scenario Class**
   ```csharp
   public class MyTestScenario : TestScenario
   {
       public override string ScenarioId => "my_test";
       public override string ScenarioName => "My Test Scenario";

       protected override bool Setup() { /* setup logic */ }
       protected override void RunTest() { /* test logic */ }
       protected override void AnalyzeResults() { /* analysis logic */ }
   }
   ```

2. **Register in TestRunner**
   ```csharp
   _scenarios.Add(("my_test", new MyTestScenario()));
   ```

3. **Update Documentation**
   - Add to this document
   - Update CI/CD triggers if needed

### Modifying Test Parameters

Edit `test-config.json` or use command-line overrides:

```json
{
  "scenarios": {
    "weapon_timing": {
      "enabled": true,
      "duration_seconds": 60,
      "weapons": ["Katana", "Longsword"],
      "dex_values": [50, 100, 150]
    }
  }
}
```

## Maintenance

### Regular Tasks

- **Weekly:** Review test logs for anomalies
- **Monthly:** Update baselines with performance improvements
- **Quarterly:** Audit test coverage and add missing scenarios

### Performance Monitoring

Track these metrics over time:
- Test execution time
- Memory usage during tests
- Timing accuracy trends
- Failure rates

### Updating Dependencies

When updating ModernUO or .NET versions:
1. Test all scenarios with both `--quick-test` and `--test-mode`
2. Update CI/CD pipeline .NET version
3. Verify cross-platform compatibility
4. Update baseline expectations if performance changes

## Support

For issues or questions:
1. Check this documentation
2. Review troubleshooting section
3. Check existing GitHub issues
4. Create new issue with:
   - Test scenario and parameters
   - Full log output
   - System information
   - Steps to reproduce
