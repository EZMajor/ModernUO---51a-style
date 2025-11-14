/*************************************************************************
 * ModernUO - Sphere 51a Audit Result Summary
 * File: AuditResultSummary.cs
 *
 * Description: Data model for test execution results and analysis.
 *              Used by AuditReportWriter to generate Markdown reports.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Modules.Sphere51a.Testing.Reports;

/// <summary>
/// Comprehensive summary of test execution results.
/// </summary>
public class AuditResultSummary
{
    public string TestType { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public string BuildVersion { get; set; }
    public string ExecutionMode { get; set; } = "Headless";

    // Overall pass/fail
    public bool Passed { get; set; }
    public List<string> FailureReasons { get; set; } = new List<string>();

    // Summary metrics
    public SummaryMetrics Summary { get; set; } = new SummaryMetrics();

    // Per-weapon/spell detailed results
    public List<WeaponTimingResult> WeaponResults { get; set; } = new List<WeaponTimingResult>();
    public List<SpellTimingResult> SpellResults { get; set; } = new List<SpellTimingResult>();

    // Stress test results
    public StressTestResult StressResult { get; set; }

    // Environment information
    public EnvironmentInfo Environment { get; set; } = new EnvironmentInfo();

    // Baseline comparison
    public BaselineComparison BaselineComparison { get; set; }

    // Observations and notes
    public List<string> Observations { get; set; } = new List<string>();
}

/// <summary>
/// High-level summary metrics for the test run.
/// </summary>
public class SummaryMetrics
{
    public int TotalActions { get; set; }
    public double AverageVarianceMs { get; set; }
    public double MaxVarianceMs { get; set; }
    public double MinVarianceMs { get; set; }
    public int WithinTargetCount { get; set; }
    public double AccuracyPercent { get; set; }
    public int OutlierCount { get; set; }
    public double OutlierPercent { get; set; }

    // Magic-specific
    public int DoubleCastCount { get; set; }
    public int FizzleCount { get; set; }
    public double FizzleRatePercent { get; set; }

    // Stress test-specific
    public double AvgTickTimeMs { get; set; }
    public double MaxTickTimeMs { get; set; }
    public int ThrottleEventCount { get; set; }
    public double ActionsPerSecond { get; set; }
}

/// <summary>
/// Detailed timing results for a single weapon type.
/// </summary>
public class WeaponTimingResult
{
    public string WeaponName { get; set; }
    public int WeaponSpeed { get; set; }
    public int SwingCount { get; set; }
    public double ExpectedDelayMs { get; set; }
    public double ActualAvgDelayMs { get; set; }
    public double VarianceMs { get; set; }
    public bool Passed { get; set; }
    public string Status => Passed ? "✅" : "❌";

    // Per-dex breakdown
    public List<DexVariationResult> DexVariations { get; set; } = new List<DexVariationResult>();
}

/// <summary>
/// Timing results for weapon at specific dexterity value.
/// </summary>
public class DexVariationResult
{
    public int Dexterity { get; set; }
    public int SwingCount { get; set; }
    public double ExpectedDelayMs { get; set; }
    public double ActualAvgDelayMs { get; set; }
    public double VarianceMs { get; set; }
    public bool Passed { get; set; }
}

/// <summary>
/// Detailed timing results for a single spell.
/// </summary>
public class SpellTimingResult
{
    public string SpellName { get; set; }
    public int Circle { get; set; }
    public int CastCount { get; set; }
    public double ExpectedDelayMs { get; set; }
    public double ActualAvgDelayMs { get; set; }
    public double VarianceMs { get; set; }
    public bool Passed { get; set; }
    public string Status => Passed ? "✅" : "❌";

    // Spell-specific metrics
    public int FizzleCount { get; set; }
    public double FizzleRatePercent { get; set; }
    public int DoubleCastCount { get; set; }
    public double AvgManaUsed { get; set; }
    public double ExpectedManaUsed { get; set; }
}

/// <summary>
/// Stress test execution results.
/// </summary>
public class StressTestResult
{
    public int ConcurrentCombatants { get; set; }
    public int TotalActions { get; set; }
    public double DurationSeconds { get; set; }
    public double ActionsPerSecond { get; set; }
    public double AvgTickTimeMs { get; set; }
    public double MaxTickTimeMs { get; set; }
    public double P95TickTimeMs { get; set; }
    public double P99TickTimeMs { get; set; }
    public int ThrottleEventCount { get; set; }
    public long MemoryUsedMB { get; set; }
    public bool Passed { get; set; }
}

/// <summary>
/// Execution environment details.
/// </summary>
public class EnvironmentInfo
{
    public string Platform { get; set; } = Environment.OSVersion.Platform.ToString();
    public string OSVersion { get; set; } = Environment.OSVersion.VersionString;
    public int ProcessorCount { get; set; } = Environment.ProcessorCount;
    public string DotNetVersion { get; set; } = Environment.Version.ToString();
    public string ServerVersion { get; set; }
    public bool AuditSystemEnabled { get; set; }
    public string AuditLevel { get; set; }
    public bool ShadowModeEnabled { get; set; }
}

/// <summary>
/// Comparison against baseline metrics.
/// </summary>
public class BaselineComparison
{
    public bool HasBaseline { get; set; }
    public string BaselineVersion { get; set; }
    public DateTime BaselineDate { get; set; }

    public double BaselineAccuracyPercent { get; set; }
    public double CurrentAccuracyPercent { get; set; }
    public double AccuracyDelta { get; set; }

    public double BaselineAvgVarianceMs { get; set; }
    public double CurrentAvgVarianceMs { get; set; }
    public double VarianceDelta { get; set; }

    public bool IsRegression { get; set; }
    public List<string> RegressionDetails { get; set; } = new List<string>();
}

/// <summary>
/// Helper extensions for result analysis.
/// </summary>
public static class AuditResultExtensions
{
    /// <summary>
    /// Calculates overall pass/fail status based on baseline comparison.
    /// </summary>
    public static bool DeterminePassStatus(this AuditResultSummary summary, BaselineMetrics baseline)
    {
        if (baseline == null)
            return true; // No baseline = informational only

        var failures = new List<string>();

        // Check accuracy
        if (summary.Summary.AccuracyPercent < baseline.AccuracyPercent - 1.0) // 1% tolerance
        {
            failures.Add($"Accuracy below baseline: {summary.Summary.AccuracyPercent:F1}% < {baseline.AccuracyPercent:F1}%");
        }

        // Check variance
        if (summary.Summary.AverageVarianceMs > baseline.MaxVarianceMs)
        {
            failures.Add($"Average variance exceeds baseline: {summary.Summary.AverageVarianceMs:F1}ms > {baseline.MaxVarianceMs:F1}ms");
        }

        // Check outliers
        if (summary.Summary.OutlierPercent > baseline.MaxOutliersPercent)
        {
            failures.Add($"Outlier rate exceeds baseline: {summary.Summary.OutlierPercent:F1}% > {baseline.MaxOutliersPercent:F1}%");
        }

        // Magic-specific checks
        if (baseline.MaxDoubleCasts > 0 && summary.Summary.DoubleCastCount > baseline.MaxDoubleCasts)
        {
            failures.Add($"Double-cast count exceeds baseline: {summary.Summary.DoubleCastCount} > {baseline.MaxDoubleCasts}");
        }

        if (baseline.MaxFizzleRatePercent > 0 && summary.Summary.FizzleRatePercent > baseline.MaxFizzleRatePercent)
        {
            failures.Add($"Fizzle rate exceeds baseline: {summary.Summary.FizzleRatePercent:F1}% > {baseline.MaxFizzleRatePercent:F1}%");
        }

        summary.FailureReasons.AddRange(failures);
        return failures.Count == 0;
    }

    /// <summary>
    /// Generates baseline comparison object.
    /// </summary>
    public static BaselineComparison CompareToBaseline(this AuditResultSummary summary, BaselineMetrics baseline)
    {
        if (baseline == null)
        {
            return new BaselineComparison { HasBaseline = false };
        }

        var comparison = new BaselineComparison
        {
            HasBaseline = true,
            BaselineVersion = baseline.LastBuild,
            BaselineDate = DateTime.TryParse(baseline.LastUpdated, out var date) ? date : DateTime.MinValue,

            BaselineAccuracyPercent = baseline.AccuracyPercent,
            CurrentAccuracyPercent = summary.Summary.AccuracyPercent,
            AccuracyDelta = summary.Summary.AccuracyPercent - baseline.AccuracyPercent,

            BaselineAvgVarianceMs = baseline.MaxVarianceMs / 2.0, // Approximation
            CurrentAvgVarianceMs = summary.Summary.AverageVarianceMs,
            VarianceDelta = summary.Summary.AverageVarianceMs - (baseline.MaxVarianceMs / 2.0)
        };

        comparison.IsRegression = comparison.AccuracyDelta < -1.0 || comparison.VarianceDelta > 10.0;

        if (comparison.IsRegression)
        {
            if (comparison.AccuracyDelta < -1.0)
                comparison.RegressionDetails.Add($"Accuracy decreased by {Math.Abs(comparison.AccuracyDelta):F1}%");

            if (comparison.VarianceDelta > 10.0)
                comparison.RegressionDetails.Add($"Variance increased by {comparison.VarianceDelta:F1}ms");
        }

        return comparison;
    }
}
