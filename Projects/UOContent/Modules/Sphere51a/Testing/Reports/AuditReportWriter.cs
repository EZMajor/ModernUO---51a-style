/*************************************************************************
 * ModernUO - Sphere 51a Audit Report Writer
 * File: AuditReportWriter.cs
 *
 * Description: Generates professional Markdown audit reports from test results.
 *              Implements the standardized report template with baseline comparison.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.IO;
using System.Linq;
using System.Text;
using Server.Logging;

namespace Server.Modules.Sphere51a.Testing.Reports;

/// <summary>
/// Generates Markdown-formatted audit reports for test results.
/// </summary>
public static class AuditReportWriter
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(AuditReportWriter));

    /// <summary>
    /// Writes a complete audit report to the configured output directory.
    /// </summary>
    /// <param name="summary">Test execution results</param>
    /// <param name="testType">Type of test (e.g., "CombatTiming", "SpellTiming", "StressTest")</param>
    /// <param name="config">Test configuration for paths and settings</param>
    public static void WriteReport(AuditResultSummary summary, string testType, TestConfig config = null)
    {
        config ??= TestConfig.Load();

        try
        {
            // Ensure output directory exists
            var outputDir = Path.Combine(global::Server.Core.BaseDirectory, config.ReportSettings.OutputDirectory);
            Directory.CreateDirectory(outputDir);

            // Generate report content
            var markdown = GenerateMarkdownReport(summary, testType);

            // Write dated report
            var timestamp = summary.StartTime.ToString("yyyy-MM-dd_HHmmss");
            var filename = $"{timestamp}_{testType}.md";
            var filepath = Path.Combine(outputDir, filename);
            File.WriteAllText(filepath, markdown);
            logger.Information("Audit report written to {Path}", filepath);

            // Write "Latest" summary if configured
            if (config.ReportSettings.GenerateLatestSummary)
            {
                var latestFilename = $"Latest_{testType}Summary.md";
                var latestFilepath = Path.Combine(outputDir, latestFilename);
                File.WriteAllText(latestFilepath, markdown);
                logger.Information("Latest summary written to {Path}", latestFilepath);
            }

            // Export raw JSONL if configured
            if (config.ReportSettings.ExportRawJsonl)
            {
                ExportRawData(summary, testType, outputDir, timestamp);
            }

            // Archive old reports
            ArchiveOldReports(outputDir, config.ReportSettings);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to write audit report for {TestType}", testType);
        }
    }

    /// <summary>
    /// Generates Markdown content for the audit report.
    /// </summary>
    private static string GenerateMarkdownReport(AuditResultSummary summary, string testType)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("# Sphere51a Combat Audit Report");
        sb.AppendLine();
        sb.AppendLine($"**Date:** {summary.StartTime:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"**Build:** {summary.BuildVersion ?? "Unknown"}");
        sb.AppendLine($"**Test Type:** {testType}");
        sb.AppendLine($"**Mode:** {summary.ExecutionMode}");
        sb.AppendLine($"**Duration:** {summary.Duration.TotalSeconds:F1}s");
        sb.AppendLine();

        // Overall status
        var statusText = summary.Passed ? "PASSED" : "FAILED";
        sb.AppendLine($"**Status:** **{statusText}**");
        sb.AppendLine();

        if (!summary.Passed && summary.FailureReasons.Count > 0)
        {
            sb.AppendLine("### Failure Reasons");
            sb.AppendLine();
            foreach (var reason in summary.FailureReasons)
            {
                sb.AppendLine($"- {reason}");
            }
            sb.AppendLine();
        }

        // Summary section
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| Metric | Target | Actual | Result |");
        sb.AppendLine("|--------|--------|--------|--------|");

        if (testType.Contains("Weapon", StringComparison.OrdinalIgnoreCase) || testType.Contains("Combat", StringComparison.OrdinalIgnoreCase))
        {
            WriteCombatSummaryTable(sb, summary);
        }
        else if (testType.Contains("Spell", StringComparison.OrdinalIgnoreCase) || testType.Contains("Magic", StringComparison.OrdinalIgnoreCase))
        {
            WriteSpellSummaryTable(sb, summary);
        }
        else if (testType.Contains("Stress", StringComparison.OrdinalIgnoreCase))
        {
            WriteStressSummaryTable(sb, summary);
        }

        sb.AppendLine();

        // Baseline comparison
        if (summary.BaselineComparison?.HasBaseline == true)
        {
            WriteBaselineComparison(sb, summary.BaselineComparison);
        }

        // Detailed results
        if (summary.WeaponResults.Count > 0)
        {
            WriteWeaponDetailedResults(sb, summary.WeaponResults);
        }

        if (summary.SpellResults.Count > 0)
        {
            WriteSpellDetailedResults(sb, summary.SpellResults);
        }

        if (summary.StressResult != null)
        {
            WriteStressDetailedResults(sb, summary.StressResult);
        }

        // Environment
        WriteEnvironmentInfo(sb, summary.Environment);

        // Observations
        if (summary.Observations.Count > 0)
        {
            WriteObservations(sb, summary.Observations);
        }

        // Conclusion
        WriteConclusion(sb, summary);

        // Footer
        sb.AppendLine("---");
        sb.AppendLine($"*Report generated by Sphere51a Testing Framework at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC*");

        return sb.ToString();
    }

    private static void WriteCombatSummaryTable(StringBuilder sb, AuditResultSummary summary)
    {
        var baselineAccuracy = summary.BaselineComparison?.BaselineAccuracyPercent ?? 98.5;
        var accuracyStatus = summary.Summary.AccuracyPercent >= baselineAccuracy - 1.0 ? "PASS" : "FAIL";

        sb.AppendLine($"| Swing Precision | ±25 ms | {summary.Summary.AverageVarianceMs:F1} ms | {accuracyStatus} |");
        sb.AppendLine($"| Accuracy | ≥{baselineAccuracy:F1}% | {summary.Summary.AccuracyPercent:F1}% | {accuracyStatus} |");
        sb.AppendLine($"| Total Swings | - | {summary.Summary.TotalActions:N0} | INFO |");
        sb.AppendLine($"| Outliers | ≤5 | {summary.Summary.OutlierCount} | {(summary.Summary.OutlierCount <= 5 ? "PASS" : "FAIL")} |");
    }

    private static void WriteSpellSummaryTable(StringBuilder sb, AuditResultSummary summary)
    {
        var baselineAccuracy = summary.BaselineComparison?.BaselineAccuracyPercent ?? 97.0;
        var accuracyStatus = summary.Summary.AccuracyPercent >= baselineAccuracy - 1.0 ? "PASS" : "FAIL";

        sb.AppendLine($"| Cast Precision | ±50 ms | {summary.Summary.AverageVarianceMs:F1} ms | {accuracyStatus} |");
        sb.AppendLine($"| Accuracy | ≥{baselineAccuracy:F1}% | {summary.Summary.AccuracyPercent:F1}% | {accuracyStatus} |");
        sb.AppendLine($"| Total Casts | - | {summary.Summary.TotalActions:N0} | INFO |");
        sb.AppendLine($"| Double-Casts | 0 | {summary.Summary.DoubleCastCount} | {(summary.Summary.DoubleCastCount == 0 ? "PASS" : "WARNING")} |");
        sb.AppendLine($"| Fizzle Rate | ≤5% | {summary.Summary.FizzleRatePercent:F1}% | {(summary.Summary.FizzleRatePercent <= 5.0 ? "PASS" : "WARNING")} |");
    }

    private static void WriteStressSummaryTable(StringBuilder sb, AuditResultSummary summary)
    {
        var stress = summary.StressResult;
        if (stress == null) return;

        sb.AppendLine($"| Avg Tick Time | <10 ms | {stress.AvgTickTimeMs:F2} ms | {(stress.AvgTickTimeMs < 10 ? "PASS" : "FAIL")} |");
        sb.AppendLine($"| Max Tick Time | <25 ms | {stress.MaxTickTimeMs:F2} ms | {(stress.MaxTickTimeMs < 25 ? "PASS" : "WARNING")} |");
        sb.AppendLine($"| Throughput | ≥100 act/s | {stress.ActionsPerSecond:F1} act/s | {(stress.ActionsPerSecond >= 100 ? "PASS" : "FAIL")} |");
        sb.AppendLine($"| Throttle Events | 0 | {stress.ThrottleEventCount} | {(stress.ThrottleEventCount == 0 ? "PASS" : "WARNING")} |");
        sb.AppendLine($"| Memory Used | <500 MB | {stress.MemoryUsedMB} MB | {(stress.MemoryUsedMB < 500 ? "PASS" : "WARNING")} |");
    }

    private static void WriteBaselineComparison(StringBuilder sb, BaselineComparison comparison)
    {
        sb.AppendLine("## Baseline Comparison");
        sb.AppendLine();
        sb.AppendLine($"**Baseline Version:** {comparison.BaselineVersion} ({comparison.BaselineDate:yyyy-MM-dd})");
        sb.AppendLine();
        sb.AppendLine("| Metric | Baseline | Current | Delta |");
        sb.AppendLine("|--------|----------|---------|-------|");
        sb.AppendLine($"| Accuracy | {comparison.BaselineAccuracyPercent:F1}% | {comparison.CurrentAccuracyPercent:F1}% | {comparison.AccuracyDelta:+0.0;-0.0}% |");
        sb.AppendLine($"| Avg Variance | {comparison.BaselineAvgVarianceMs:F1} ms | {comparison.CurrentAvgVarianceMs:F1} ms | {comparison.VarianceDelta:+0.0;-0.0} ms |");
        sb.AppendLine();

        if (comparison.IsRegression)
        {
            sb.AppendLine("### WARNING: Regression Detected");
            sb.AppendLine();
            foreach (var detail in comparison.RegressionDetails)
            {
                sb.AppendLine($"- {detail}");
            }
            sb.AppendLine();
        }
    }

    private static void WriteWeaponDetailedResults(StringBuilder sb, System.Collections.Generic.List<WeaponTimingResult> results)
    {
        sb.AppendLine("## Detailed Weapon Timings");
        sb.AppendLine();
        sb.AppendLine("| Weapon | Expected | Actual | Variance | Result |");
        sb.AppendLine("|--------|----------|--------|----------|--------|");

        foreach (var weapon in results.OrderBy(w => w.WeaponName))
        {
            sb.AppendLine($"| {weapon.WeaponName} | {weapon.ExpectedDelayMs:F0} ms | {weapon.ActualAvgDelayMs:F0} ms | {weapon.VarianceMs:+0;-0} ms | {weapon.Status} |");
        }

        sb.AppendLine();
    }

    private static void WriteSpellDetailedResults(StringBuilder sb, System.Collections.Generic.List<SpellTimingResult> results)
    {
        sb.AppendLine("## Detailed Spell Timings");
        sb.AppendLine();
        sb.AppendLine("| Spell | Circle | Casts | Expected | Actual | Variance | Fizzles | Result |");
        sb.AppendLine("|-------|--------|-------|----------|--------|----------|---------|--------|");

        foreach (var spell in results.OrderBy(s => s.Circle).ThenBy(s => s.SpellName))
        {
            sb.AppendLine($"| {spell.SpellName} | {spell.Circle} | {spell.CastCount} | {spell.ExpectedDelayMs:F0} ms | {spell.ActualAvgDelayMs:F0} ms | {spell.VarianceMs:+0;-0} ms | {spell.FizzleCount} | {spell.Status} |");
        }

        sb.AppendLine();
    }

    private static void WriteStressDetailedResults(StringBuilder sb, StressTestResult stress)
    {
        sb.AppendLine("## Stress Test Performance");
        sb.AppendLine();
        sb.AppendLine($"**Concurrent Combatants:** {stress.ConcurrentCombatants}");
        sb.AppendLine($"**Total Actions:** {stress.TotalActions:N0}");
        sb.AppendLine($"**Duration:** {stress.DurationSeconds:F1}s");
        sb.AppendLine($"**Throughput:** {stress.ActionsPerSecond:F1} actions/sec");
        sb.AppendLine();
        sb.AppendLine("### Tick Time Distribution");
        sb.AppendLine();
        sb.AppendLine("| Percentile | Time |");
        sb.AppendLine("|------------|------|");
        sb.AppendLine($"| Average | {stress.AvgTickTimeMs:F2} ms |");
        sb.AppendLine($"| P95 | {stress.P95TickTimeMs:F2} ms |");
        sb.AppendLine($"| P99 | {stress.P99TickTimeMs:F2} ms |");
        sb.AppendLine($"| Max | {stress.MaxTickTimeMs:F2} ms |");
        sb.AppendLine();
    }

    private static void WriteEnvironmentInfo(StringBuilder sb, EnvironmentInfo env)
    {
        sb.AppendLine("## Environment");
        sb.AppendLine();
        sb.AppendLine($"- **Platform:** {env.Platform}");
        sb.AppendLine($"- **OS:** {env.OSVersion}");
        sb.AppendLine($"- **Processors:** {env.ProcessorCount}");
        sb.AppendLine($"- **.NET Version:** {env.DotNetVersion}");
        sb.AppendLine($"- **Server Version:** {env.ServerVersion ?? "Unknown"}");
        sb.AppendLine($"- **Audit System:** {(env.AuditSystemEnabled ? "Enabled" : "Disabled")} ({env.AuditLevel})");
        sb.AppendLine($"- **Shadow Mode:** {(env.ShadowModeEnabled ? "Enabled" : "Disabled")}");
        sb.AppendLine();
    }

    private static void WriteObservations(StringBuilder sb, System.Collections.Generic.List<string> observations)
    {
        sb.AppendLine("## Observations");
        sb.AppendLine();
        foreach (var obs in observations)
        {
            sb.AppendLine($"- {obs}");
        }
        sb.AppendLine();
    }

    private static void WriteConclusion(StringBuilder sb, AuditResultSummary summary)
    {
        sb.AppendLine("## Conclusion");
        sb.AppendLine();

        if (summary.Passed)
        {
            sb.AppendLine($"**All tests PASSED.** The combat/magic timing system is performing within acceptable parameters.");
        }
        else
        {
            sb.AppendLine($"**Test FAILED.** The following issues were detected:");
            sb.AppendLine();
            foreach (var reason in summary.FailureReasons)
            {
                sb.AppendLine($"- {reason}");
            }
        }

        sb.AppendLine();
    }

    private static void ExportRawData(AuditResultSummary summary, string testType, string outputDir, string timestamp)
    {
        try
        {
            var jsonFilename = $"{timestamp}_{testType}_raw.json";
            var jsonPath = Path.Combine(outputDir, jsonFilename);

            var json = System.Text.Json.JsonSerializer.Serialize(summary, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(jsonPath, json);
            logger.Information("Raw data exported to {Path}", jsonPath);
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to export raw data for {TestType}", testType);
        }
    }

    private static void ArchiveOldReports(string outputDir, ReportSettings settings)
    {
        try
        {
            var archiveDir = Path.Combine(global::Server.Core.BaseDirectory, settings.ArchiveDirectory);
            Directory.CreateDirectory(archiveDir);

            var cutoffDate = DateTime.UtcNow.AddDays(-settings.RetentionDaysLocal);
            var files = Directory.GetFiles(outputDir, "*.md")
                .Where(f => !Path.GetFileName(f).StartsWith("Latest_"))
                .Select(f => new FileInfo(f))
                .Where(fi => fi.LastWriteTimeUtc < cutoffDate)
                .ToList();

            foreach (var file in files)
            {
                var archivePath = Path.Combine(archiveDir, file.Name);
                File.Move(file.FullName, archivePath, true);
                logger.Debug("Archived report: {File}", file.Name);
            }

            if (files.Count > 0)
            {
                logger.Information("Archived {Count} old reports to {Path}", files.Count, archiveDir);
            }
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to archive old reports");
        }
    }
}
