/*************************************************************************
 * ModernUO - Sphere 51a Test Runner
 * File: TestRunner.cs
 *
 * Description: Orchestrates test scenario execution and report generation.
 *              Manages test lifecycle and exit codes.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using Server.Logging;
using Server.Modules.Sphere51a.Testing.Reports;
using Server.Modules.Sphere51a.Testing.Scenarios;

namespace Server.Modules.Sphere51a.Testing;

/// <summary>
/// Orchestrates execution of test scenarios and generates reports.
/// </summary>
public class TestRunner
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(TestRunner));

    private readonly TestConfig _config;
    private readonly TestFrameworkArguments _args;
    private readonly List<(string scenarioId, TestScenario scenario)> _scenarios = new();

    public TestRunner(TestConfig config, TestFrameworkArguments args)
    {
        _config = config;
        _args = args;
        RegisterScenarios();
    }

    /// <summary>
    /// Registers available test scenarios.
    /// </summary>
    private void RegisterScenarios()
    {
        // Register all available scenarios
        _scenarios.Add(("weapon_timing", new WeaponSwingTimingTest()));
        _scenarios.Add(("spell_timing", new SpellTimingTest()));
        _scenarios.Add(("stress_test", new StressTest()));

        logger.Information("Registered {Count} test scenarios", _scenarios.Count);
    }

    /// <summary>
    /// Executes selected test scenarios and generates reports.
    /// Returns exit code: 0 = success, 1 = test failure, 2 = fatal error, 3 = config error.
    /// </summary>
    public int Run()
    {
        try
        {
            logger.Information("═══════════════════════════════════════════════");
            logger.Information("  Sphere51a Headless Testing Framework");
            logger.Information("═══════════════════════════════════════════════");
            logger.Information("");

            // Validate configuration
            if (!ValidateConfiguration())
            {
                logger.Error("Configuration validation failed");
                return 3; // Config error
            }

            // Determine which scenarios to run
            var scenariosToRun = SelectScenarios();

            if (scenariosToRun.Count == 0)
            {
                logger.Warning("No scenarios selected for execution");
                return 3;
            }

            logger.Information("Executing {Count} scenario(s): {Scenarios}",
                scenariosToRun.Count,
                string.Join(", ", scenariosToRun.ConvertAll(s => s.scenario.ScenarioName)));
            logger.Information("");

            // Execute each scenario
            var allPassed = true;
            var results = new List<AuditResultSummary>();

            foreach (var (scenarioId, scenario) in scenariosToRun)
            {
                logger.Information("─────────────────────────────────────────────");
                logger.Information("Starting scenario: {Scenario}", scenario.ScenarioName);
                logger.Information("─────────────────────────────────────────────");

                try
                {
                    scenario.Initialize(_config);
                    var passed = scenario.Execute();

                    results.Add(scenario.Results);

                    if (!passed)
                    {
                        allPassed = false;
                        logger.Warning("Scenario FAILED: {Scenario}", scenario.ScenarioName);

                        if (_config.TestSettings.ExitOnFailure)
                        {
                            logger.Information("ExitOnFailure is enabled, stopping test execution");
                            break;
                        }
                    }
                    else
                    {
                        logger.Information("Scenario PASSED: {Scenario}", scenario.ScenarioName);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Fatal error executing scenario: {Scenario}", scenario.ScenarioName);
                    allPassed = false;

                    if (_config.TestSettings.ExitOnFailure)
                    {
                        return 2; // Fatal error
                    }
                }

                logger.Information("");
            }

            // Generate reports
            logger.Information("─────────────────────────────────────────────");
            logger.Information("Generating test reports...");
            logger.Information("─────────────────────────────────────────────");

            GenerateReports(results);

            // Update baselines if requested
            if (_args.UpdateBaselines && allPassed)
            {
                UpdateBaselines(results);
            }

            // Print summary
            PrintSummary(results, allPassed);

            return allPassed ? 0 : 1;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Fatal error during test execution");
            return 2; // Fatal error
        }
    }

    private bool ValidateConfiguration()
    {
        if (_config == null)
        {
            logger.Error("Test configuration is null");
            return false;
        }

        if (_config.ReportSettings == null)
        {
            logger.Error("Report settings are missing from configuration");
            return false;
        }

        // Validate output directory is writable
        try
        {
            var outputDir = Path.Combine(global::Server.Core.BaseDirectory, _config.ReportSettings.OutputDirectory);
            Directory.CreateDirectory(outputDir);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Cannot create output directory: {Path}", _config.ReportSettings.OutputDirectory);
            return false;
        }

        return true;
    }

    private List<(string scenarioId, TestScenario scenario)> SelectScenarios()
    {
        var selected = new List<(string scenarioId, TestScenario scenario)>();

        if (!string.IsNullOrEmpty(_args.Scenario) && _args.Scenario.ToLowerInvariant() != "all")
        {
            // Run specific scenario
            var scenario = _scenarios.Find(s => s.scenarioId.Equals(_args.Scenario, StringComparison.OrdinalIgnoreCase));

            if (scenario.scenario != null)
            {
                selected.Add(scenario);
            }
            else
            {
                logger.Warning("Scenario '{Scenario}' not found", _args.Scenario);
            }
        }
        else
        {
            // Run all enabled scenarios from config
            foreach (var (scenarioId, scenario) in _scenarios)
            {
                var enabled = scenarioId switch
                {
                    "weapon_timing" => _config.Scenarios.WeaponTiming?.Enabled ?? true,
                    "spell_timing" => _config.Scenarios.SpellTiming?.Enabled ?? true,
                    "stress_test" => _config.Scenarios.StressTest?.Enabled ?? true,
                    _ => false
                };

                if (enabled)
                {
                    selected.Add((scenarioId, scenario));
                }
            }
        }

        return selected;
    }

    private void GenerateReports(List<AuditResultSummary> results)
    {
        try
        {
            foreach (var result in results)
            {
                AuditReportWriter.WriteReport(result, result.TestType, _config);
            }

            logger.Information("Reports generated successfully");
            logger.Information("Output directory: {Path}",
                Path.Combine(global::Server.Core.BaseDirectory, _config.ReportSettings.OutputDirectory));
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to generate reports");
        }
    }

    private void UpdateBaselines(List<AuditResultSummary> results)
    {
        try
        {
            logger.Information("Updating baselines with current test results...");

            foreach (var result in results)
            {
                var baselineKey = result.TestType.ToLowerInvariant() switch
                {
                    "weapon swing timing test" => "weapon_swing_timing",
                    "spell timing test" => "spell_timing",
                    "combat system stress test" => "stress_test",
                    _ => null
                };

                if (baselineKey != null && _config.Baselines.ContainsKey(baselineKey))
                {
                    var baseline = _config.Baselines[baselineKey];

                    baseline.AccuracyPercent = result.Summary.AccuracyPercent;
                    baseline.MaxVarianceMs = result.Summary.MaxVarianceMs;
                    baseline.LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd");
                    baseline.LastBuild = result.BuildVersion;

                    logger.Information("Updated baseline for {Scenario}: {Accuracy:F1}% accuracy",
                        baselineKey, result.Summary.AccuracyPercent);
                }
            }

            _config.Save();
            logger.Information("Baselines saved to configuration");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to update baselines");
        }
    }

    private void PrintSummary(List<AuditResultSummary> results, bool allPassed)
    {
        logger.Information("");
        logger.Information("═══════════════════════════════════════════════");
        logger.Information("  Test Execution Summary");
        logger.Information("═══════════════════════════════════════════════");

        foreach (var result in results)
        {
            var statusIcon = result.Passed ? "✅" : "❌";
            logger.Information("{Icon} {TestType}: {Status}",
                statusIcon,
                result.TestType,
                result.Passed ? "PASSED" : "FAILED");

            if (result.Summary.TotalActions > 0)
            {
                logger.Information("   - Actions: {Count}, Accuracy: {Accuracy:F1}%",
                    result.Summary.TotalActions,
                    result.Summary.AccuracyPercent);
            }

            if (!result.Passed && result.FailureReasons.Count > 0)
            {
                logger.Information("   - Failures: {Reasons}",
                    string.Join("; ", result.FailureReasons));
            }
        }

        logger.Information("");
        logger.Information("Overall Result: {Result}",
            allPassed ? "✅ ALL TESTS PASSED" : "❌ SOME TESTS FAILED");
        logger.Information("═══════════════════════════════════════════════");
        logger.Information("");
    }
}
