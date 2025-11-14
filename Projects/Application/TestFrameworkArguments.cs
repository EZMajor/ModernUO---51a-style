/*************************************************************************
 * ModernUO - Sphere 51a Test Framework CLI Arguments
 * File: TestFrameworkArguments.cs
 *
 * Description: Parses command-line arguments for headless test execution.
 *              Supports test mode activation, scenario selection, and reporting.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;

namespace Server;

/// <summary>
/// Command-line arguments for headless testing framework.
/// </summary>
public class TestFrameworkArguments
{
    /// <summary>
    /// Enable headless test mode (skip normal server initialization).
    /// </summary>
    public bool TestMode { get; set; }

    /// <summary>
    /// Specific test scenario to run (null = run all enabled scenarios).
    /// Valid values: "weapon_timing", "spell_timing", "stress_test", "all"
    /// </summary>
    public string Scenario { get; set; }

    /// <summary>
    /// Test duration override in seconds (null = use config defaults).
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// Path to custom test configuration file.
    /// </summary>
    public string ConfigPath { get; set; }

    /// <summary>
    /// Exit immediately after generating report (don't wait for input).
    /// </summary>
    public bool AutoExit { get; set; } = true;

    /// <summary>
    /// Enable verbose logging during test execution.
    /// </summary>
    public bool Verbose { get; set; }

    /// <summary>
    /// Only generate report from existing audit logs (don't run tests).
    /// </summary>
    public bool ReportOnly { get; set; }

    /// <summary>
    /// Update baselines with current test results.
    /// </summary>
    public bool UpdateBaselines { get; set; }

    /// <summary>
    /// Parses command-line arguments into structured options.
    /// </summary>
    public static TestFrameworkArguments Parse(string[] args)
    {
        var options = new TestFrameworkArguments();

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i].ToLowerInvariant();

            switch (arg)
            {
                case "--test-mode":
                case "-t":
                    options.TestMode = true;
                    break;

                case "--scenario":
                case "-s":
                    if (i + 1 < args.Length)
                    {
                        options.Scenario = args[++i];
                    }
                    break;

                case "--duration":
                case "-d":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var duration))
                    {
                        options.DurationSeconds = duration;
                    }
                    break;

                case "--config":
                case "-c":
                    if (i + 1 < args.Length)
                    {
                        options.ConfigPath = args[++i];
                    }
                    break;

                case "--verbose":
                case "-v":
                    options.Verbose = true;
                    break;

                case "--report-only":
                case "-r":
                    options.ReportOnly = true;
                    break;

                case "--update-baselines":
                case "-u":
                    options.UpdateBaselines = true;
                    break;

                case "--no-auto-exit":
                    options.AutoExit = false;
                    break;

                case "--help":
                case "-h":
                case "-?":
                    PrintUsage();
                    Environment.Exit(0);
                    break;
            }
        }

        return options;
    }

    /// <summary>
    /// Prints usage information to console.
    /// </summary>
    public static void PrintUsage()
    {
        Console.WriteLine("Sphere51a Headless Testing Framework");
        Console.WriteLine();
        Console.WriteLine("Usage: ModernUO --test-mode [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --test-mode, -t           Enable headless test mode (required)");
        Console.WriteLine("  --scenario, -s <name>     Run specific scenario (weapon_timing, spell_timing, stress_test, all)");
        Console.WriteLine("  --duration, -d <seconds>  Override test duration (default: from config)");
        Console.WriteLine("  --config, -c <path>       Use custom test configuration file");
        Console.WriteLine("  --verbose, -v             Enable verbose logging");
        Console.WriteLine("  --report-only, -r         Generate report from existing logs (don't run tests)");
        Console.WriteLine("  --update-baselines, -u    Update baseline metrics with current results");
        Console.WriteLine("  --no-auto-exit            Wait for input before exiting");
        Console.WriteLine("  --help, -h                Display this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  ModernUO --test-mode --scenario weapon_timing --duration 120");
        Console.WriteLine("  ModernUO --test-mode --scenario all --verbose");
        Console.WriteLine("  ModernUO --test-mode --report-only");
        Console.WriteLine();
        Console.WriteLine("Exit Codes:");
        Console.WriteLine("  0 - All tests passed");
        Console.WriteLine("  1 - One or more tests failed");
        Console.WriteLine("  2 - Fatal error during test execution");
        Console.WriteLine("  3 - Configuration error");
    }

    /// <summary>
    /// Validates parsed arguments and returns error messages if invalid.
    /// </summary>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (!string.IsNullOrEmpty(Scenario))
        {
            var validScenarios = new[] { "weapon_timing", "spell_timing", "stress_test", "all" };
            if (!validScenarios.Contains(Scenario.ToLowerInvariant()))
            {
                errors.Add($"Invalid scenario '{Scenario}'. Valid values: {string.Join(", ", validScenarios)}");
            }
        }

        if (DurationSeconds.HasValue && DurationSeconds.Value < 10)
        {
            errors.Add("Test duration must be at least 10 seconds");
        }

        if (DurationSeconds.HasValue && DurationSeconds.Value > 3600)
        {
            errors.Add("Test duration cannot exceed 3600 seconds (1 hour)");
        }

        if (!string.IsNullOrEmpty(ConfigPath) && !System.IO.File.Exists(ConfigPath))
        {
            errors.Add($"Configuration file not found: {ConfigPath}");
        }

        if (ReportOnly && UpdateBaselines)
        {
            errors.Add("Cannot use --report-only with --update-baselines");
        }

        return errors;
    }

    /// <summary>
    /// Gets the test execution mode as a string.
    /// </summary>
    public string GetExecutionMode()
    {
        if (ReportOnly)
            return "Report Generation";
        if (UpdateBaselines)
            return "Baseline Update";
        return "Headless Testing";
    }

    /// <summary>
    /// Prints configuration summary to console.
    /// </summary>
    public void PrintSummary()
    {
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine("  Sphere51a Test Framework Configuration");
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine($"Mode:         {GetExecutionMode()}");
        Console.WriteLine($"Scenario:     {Scenario ?? "All enabled scenarios"}");
        Console.WriteLine($"Duration:     {(DurationSeconds.HasValue ? $"{DurationSeconds.Value}s" : "From config")}");
        Console.WriteLine($"Config Path:  {ConfigPath ?? "Default (test-config.json)"}");
        Console.WriteLine($"Verbose:      {Verbose}");
        Console.WriteLine($"Auto-Exit:    {AutoExit}");
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine();
    }
}
