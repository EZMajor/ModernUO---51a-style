/*************************************************************************
 * ModernUO - Sphere 51a Timing Accuracy Verification
 * File: VerifyTimingAccuracy.cs
 *
 * Description: Command to verify combat timing accuracy by analyzing
 *              recent audit logs and comparing against expected values.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Commands;
using Server.Modules.Sphere51a.Combat.Audit;

namespace Server.Modules.Sphere51a.Commands;

/// <summary>
/// Command to verify combat timing accuracy from audit logs.
/// Usage: [VerifyTimingAccuracy [count]
/// </summary>
public class VerifyTimingAccuracy
{
    public static void Initialize()
    {
        CommandSystem.Register("VerifyTimingAccuracy", AccessLevel.GameMaster, OnCommand);
    }

    [Usage("VerifyTimingAccuracy [count]")]
    [Description("Analyzes recent combat audit logs for timing accuracy. Optionally specify number of recent entries to analyze (default 100).")]
    private static void OnCommand(CommandEventArgs e)
    {
        var mobile = e.Mobile;

        if (!CombatAuditSystem.IsInitialized)
        {
            mobile.SendMessage(0x22, "Combat audit system is not initialized.");
            return;
        }

        if (!CombatAuditSystem.Config.Enabled)
        {
            mobile.SendMessage(0x22, "Combat audit system is disabled.");
            return;
        }

        // Parse optional count parameter
        var count = 100;
        if (e.Arguments.Length > 0 && int.TryParse(e.Arguments[0], out var parsedCount))
        {
            count = Math.Clamp(parsedCount, 10, 1000);
        }

        // Get recent entries from buffer
        var allEntries = CombatAuditSystem.GetBufferSnapshot();

        if (allEntries.Count == 0)
        {
            mobile.SendMessage(0x22, "No audit entries available. Combat actions must occur first.");
            return;
        }

        // Filter to swing complete entries (these have actual vs expected timing)
        var swingEntries = allEntries
            .Where(e => e.ActionType == CombatActionTypes.SwingComplete && e.ActualDelayMs > 0)
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToList();

        if (swingEntries.Count == 0)
        {
            mobile.SendMessage(0x22, "No swing completion entries found. Need completed attacks to analyze.");
            return;
        }

        // Analyze accuracy
        var report = AnalyzeAccuracy(swingEntries);

        // Display report
        mobile.SendMessage(0x59, "═══════════════════════════════════════════════════");
        mobile.SendMessage(0x59, $"  Sphere51a Combat Timing Accuracy Report");
        mobile.SendMessage(0x59, "═══════════════════════════════════════════════════");
        mobile.SendMessage($"Analyzing {report.TotalSwings} recent weapon swings");
        mobile.SendMessage("");

        mobile.SendMessage(0x5D, "Overall Accuracy:");
        mobile.SendMessage($"  Avg Variance: {report.AvgVarianceMs:F1}ms");
        mobile.SendMessage($"  Max Variance: {report.MaxVarianceMs:F1}ms");
        mobile.SendMessage($"  Within ±25ms: {report.Within25Ms} ({report.AccuracyPercent:F1}%)");
        mobile.SendMessage($"  Within ±50ms: {report.Within50Ms} ({report.Within50MsPercent:F1}%)");
        mobile.SendMessage($"  Outliers (>50ms): {report.Outliers}");
        mobile.SendMessage("");

        if (report.WeaponStats.Count > 0)
        {
            mobile.SendMessage(0x5D, "Per-Weapon Breakdown:");
            foreach (var weaponStat in report.WeaponStats.Take(5))
            {
                mobile.SendMessage($"  {weaponStat.WeaponName}:");
                mobile.SendMessage($"    Count: {weaponStat.Count}, Avg: {weaponStat.AvgDelayMs:F0}ms (expected {weaponStat.ExpectedDelayMs:F0}ms)");
                mobile.SendMessage($"    Variance: {weaponStat.AvgVarianceMs:F1}ms, Accuracy: {weaponStat.AccuracyPercent:F1}%");
            }

            if (report.WeaponStats.Count > 5)
            {
                mobile.SendMessage($"  ... and {report.WeaponStats.Count - 5} more weapons");
            }
            mobile.SendMessage("");
        }

        if (report.OutlierEntries.Count > 0)
        {
            mobile.SendMessage(0x22, $"Recent Outliers (>{report.OutlierThreshold}ms variance):");
            foreach (var outlier in report.OutlierEntries.Take(3))
            {
                mobile.SendMessage(0x22, $"  {outlier.Name}: {outlier.WeaponName}, {outlier.ActualDelayMs:F0}ms (expected {outlier.ExpectedDelayMs:F0}ms, Δ{outlier.VarianceMs:F0}ms)");
            }

            if (report.OutlierEntries.Count > 3)
            {
                mobile.SendMessage(0x22, $"  ... and {report.OutlierEntries.Count - 3} more outliers");
            }
            mobile.SendMessage("");
        }

        // Overall assessment
        var status = report.AccuracyPercent >= 95 ? 0x3F : report.AccuracyPercent >= 90 ? 0x35 : 0x22;
        var statusText = report.AccuracyPercent >= 95 ? "EXCELLENT" : report.AccuracyPercent >= 90 ? "GOOD" : "NEEDS ATTENTION";

        mobile.SendMessage(status, $"Status: {statusText}");
        mobile.SendMessage(0x59, "═══════════════════════════════════════════════════");

        // Offer shadow mode comparison if available
        if (ShadowModeVerifier.IsEnabled && ShadowModeVerifier.TotalComparisons > 0)
        {
            mobile.SendMessage("");
            mobile.SendMessage(0x5D, "Shadow Mode Active:");
            mobile.SendMessage($"  Total comparisons: {ShadowModeVerifier.TotalComparisons}");
            mobile.SendMessage($"  Discrepancies (>10ms): {ShadowModeVerifier.DiscrepancyCount}");
            mobile.SendMessage($"  Use [SphereShadowReport] for detailed comparison");
        }
    }

    private static AccuracyReport AnalyzeAccuracy(List<CombatLogEntry> entries)
    {
        var report = new AccuracyReport
        {
            TotalSwings = entries.Count,
            AvgVarianceMs = entries.Average(e => Math.Abs(e.VarianceMs)),
            MaxVarianceMs = entries.Max(e => Math.Abs(e.VarianceMs)),
            Within25Ms = entries.Count(e => Math.Abs(e.VarianceMs) <= 25),
            Within50Ms = entries.Count(e => Math.Abs(e.VarianceMs) <= 50),
            Outliers = entries.Count(e => Math.Abs(e.VarianceMs) > 50),
            OutlierThreshold = 50
        };

        report.AccuracyPercent = (report.Within25Ms / (double)report.TotalSwings) * 100.0;
        report.Within50MsPercent = (report.Within50Ms / (double)report.TotalSwings) * 100.0;

        // Per-weapon stats
        report.WeaponStats = entries
            .GroupBy(e => e.WeaponName ?? "Unknown")
            .Select(g => new WeaponAccuracyStats
            {
                WeaponName = g.Key,
                Count = g.Count(),
                AvgDelayMs = g.Average(e => e.ActualDelayMs),
                ExpectedDelayMs = g.Average(e => e.ExpectedDelayMs),
                AvgVarianceMs = g.Average(e => Math.Abs(e.VarianceMs)),
                AccuracyPercent = (g.Count(e => Math.Abs(e.VarianceMs) <= 25) / (double)g.Count()) * 100.0
            })
            .OrderByDescending(w => w.Count)
            .ToList();

        // Outlier entries
        report.OutlierEntries = entries
            .Where(e => Math.Abs(e.VarianceMs) > report.OutlierThreshold)
            .OrderByDescending(e => Math.Abs(e.VarianceMs))
            .ToList();

        return report;
    }

    private class AccuracyReport
    {
        public int TotalSwings { get; set; }
        public double AvgVarianceMs { get; set; }
        public double MaxVarianceMs { get; set; }
        public int Within25Ms { get; set; }
        public int Within50Ms { get; set; }
        public int Outliers { get; set; }
        public double AccuracyPercent { get; set; }
        public double Within50MsPercent { get; set; }
        public double OutlierThreshold { get; set; }
        public List<WeaponAccuracyStats> WeaponStats { get; set; }
        public List<CombatLogEntry> OutlierEntries { get; set; }
    }

    private class WeaponAccuracyStats
    {
        public string WeaponName { get; set; }
        public int Count { get; set; }
        public double AvgDelayMs { get; set; }
        public double ExpectedDelayMs { get; set; }
        public double AvgVarianceMs { get; set; }
        public double AccuracyPercent { get; set; }
    }
}
