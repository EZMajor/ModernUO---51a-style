# Sphere51a Audit Reports

This directory contains automated test reports from the Sphere51a Combat & Magic Verification system.

## Directory Structure

```
AuditReports/
├── README.md                           # This file
├── Latest_WeaponSwingTimingTestSummary.md  # Most recent weapon timing test
├── Latest_SpellTimingTestSummary.md        # Most recent spell timing test
├── Latest_CombatSystemStressTestSummary.md # Most recent stress test
├── YYYY-MM-DD_HHmmss_TestType.md           # Dated test reports
├── YYYY-MM-DD_HHmmss_TestType_raw.json     # Raw test data (if enabled)
└── Archive/                                # Old reports (>7 days local, >30 days CI)
```

## Report Types

### Weapon Swing Timing Test
Tests weapon swing timing accuracy across different weapon types and dexterity values.

**Target Metrics:**
- Timing Precision: ±25ms
- Accuracy: ≥98.5%
- Max Outliers: ≤5

### Spell Timing Test
Tests spell cast timing accuracy and double-cast detection.

**Target Metrics:**
- Cast Precision: ±50ms
- Accuracy: ≥97.0%
- Double-Casts: 0
- Fizzle Rate: ≤5%

### Stress Test
Tests system performance under concurrent combat load.

**Target Metrics:**
- Avg Tick Time: <10ms
- Max Tick Time: <25ms
- Throughput: ≥100 actions/sec
- Throttle Events: 0

## Retention Policy

- **Local Development:** Reports retained for 7 days
- **CI/CD Environment:** Reports retained for 30 days
- **Archive:** Old reports moved to `Archive/` subdirectory

## Running Tests Locally

```bash
# Run all tests
dotnet run --project Projects/Application/Application.csproj -- --test-mode --scenario all

# Run specific test
dotnet run --project Projects/Application/Application.csproj -- --test-mode --scenario weapon_timing --duration 60

# Run with verbose logging
dotnet run --project Projects/Application/Application.csproj -- --test-mode --scenario all --verbose

# Update baselines with current results
dotnet run --project Projects/Application/Application.csproj -- --test-mode --scenario weapon_timing --update-baselines
```

## CI/CD Integration

Tests run automatically on:
- Push to `main` or `Sphere51a-ModernUO` branches
- Pull requests to `main` or `Sphere51a-ModernUO` branches
- Manual workflow dispatch

See [.github/workflows/sphere-test.yml](../.github/workflows/sphere-test.yml) for CI/CD configuration.

## Baseline Tracking

Test baselines are tracked in [test-config.json](../Projects/UOContent/Modules/Sphere51a/Configuration/test-config.json).

Baselines can be updated with:
```bash
dotnet run --project Projects/Application/Application.csproj -- --test-mode --scenario all --update-baselines
```

## Report Format

Reports are generated in Markdown format with the following sections:

1. **Summary** - Overall pass/fail status and key metrics
2. **Baseline Comparison** - Comparison against historical baselines
3. **Detailed Results** - Per-weapon/spell breakdown
4. **Environment** - Test environment details
5. **Observations** - Notable findings and recommendations
6. **Conclusion** - Final assessment

## Exit Codes

- `0` - All tests passed
- `1` - One or more tests failed
- `2` - Fatal error during test execution
- `3` - Configuration error

## Support

For issues or questions about the testing framework, see:
- [Testing Documentation](../Projects/UOContent/Modules/Sphere51a/Testing/README.md)
- [GitHub Issues](https://github.com/EZMajor/51a-style-ModernUo/issues)
