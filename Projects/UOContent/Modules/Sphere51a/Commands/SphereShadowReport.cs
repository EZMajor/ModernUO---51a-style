/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: SphereShadowReport.cs
 *
 * Description: Command to generate shadow mode reports comparing new vs legacy timing.
 *              Used during migration to validate timing accuracy.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Modules.Sphere51a;
using Server.Modules.Sphere51a.Combat;

namespace Server.Modules.Sphere51a.Commands;

/// <summary>
/// Command to generate shadow mode reports comparing new vs legacy timing.
/// Usage: [SphereShadowReport
/// </summary>
public class SphereShadowReport
{
    // Static storage for shadow mode data
    private static readonly Dictionary<Mobile, List<ShadowEntry>> _shadowData = new();
    private static DateTime _shadowStartTime = DateTime.MinValue;
    private static bool _shadowModeActive = false;

    public static void Initialize()
    {
        CommandSystem.Register("SphereShadowReport", AccessLevel.Administrator, OnCommand);
    }

    [Usage("SphereShadowReport [start|stop|generate|clear|status]")]
    [Description("Manages shadow mode reporting for timing validation.")]
    private static void OnCommand(CommandEventArgs e)
    {
        var mobile = e.Mobile;
        var args = e.Arguments;

        if (args.Length == 0)
        {
            ShowUsage(mobile);
            return;
        }

        var command = args[0].ToLower();

        switch (command)
        {
            case "start":
                StartShadowMode(mobile);
                break;
            case "stop":
                StopShadowMode(mobile);
                break;
            case "generate":
                GenerateReport(mobile);
                break;
            case "clear":
                ClearShadowData(mobile);
                break;
            case "status":
                ShowStatus(mobile);
                break;
            default:
                ShowUsage(mobile);
                break;
        }
    }

    private static void ShowUsage(Mobile mobile)
    {
        mobile.SendMessage("Usage: [SphereShadowReport <command>");
        mobile.SendMessage("Commands:");
        mobile.SendMessage("  start - Begin shadow mode data collection");
        mobile.SendMessage("  stop - End shadow mode data collection");
        mobile.SendMessage("  generate - Generate timing comparison report");
        mobile.SendMessage("  clear - Clear all collected shadow data");
        mobile.SendMessage("  status - Show current shadow mode status");
    }

    private static void StartShadowMode(Mobile mobile)
    {
        if (_shadowModeActive)
        {
            mobile.SendMessage("Shadow mode is already active.");
            return;
        }

        _shadowModeActive = true;
        _shadowStartTime = DateTime.UtcNow;
        _shadowData.Clear();

        mobile.SendMessage("Shadow mode started. Collecting timing comparison data...");
        mobile.SendMessage("Use [SphereShadowReport stop] to end collection and generate report.");
    }

    private static void StopShadowMode(Mobile mobile)
    {
        if (!_shadowModeActive)
        {
            mobile.SendMessage("Shadow mode is not active.");
            return;
        }

        _shadowModeActive = false;
        var duration = DateTime.UtcNow - _shadowStartTime;

        mobile.SendMessage($"Shadow mode stopped after {duration.TotalMinutes:F1} minutes.");
        mobile.SendMessage($"Collected data for {_shadowData.Count} mobiles.");

        // Auto-generate report
        GenerateReport(mobile);
    }

    private static void GenerateReport(Mobile mobile)
    {
        if (_shadowData.Count == 0)
        {
            mobile.SendMessage("No shadow data available. Start shadow mode first.");
            return;
        }

        try
        {
            var report = BuildReport();
            var fileName = $"sphere_shadow_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";
            var filePath = Path.Combine("Logs", fileName);

            // Ensure Logs directory exists
            Directory.CreateDirectory("Logs");

            File.WriteAllText(filePath, report);

            mobile.SendMessage($"Shadow report generated: {filePath}");
            mobile.SendMessage($"Total entries analyzed: {_shadowData.Sum(kvp => kvp.Value.Count)}");

            // Show summary in chat
            ShowReportSummary(mobile, report);
        }
        catch (Exception ex)
        {
            mobile.SendMessage($"Error generating report: {ex.Message}");
        }
    }

    private static string BuildReport()
    {
        var sb = new StringBuilder();

        sb.AppendLine("=== Sphere 51a Shadow Mode Report ===");
        sb.AppendLine($"Generated: {DateTime.UtcNow}");
        sb.AppendLine($"Duration: {DateTime.UtcNow - _shadowStartTime}");
        sb.AppendLine($"Active Provider: {SphereInitializer.ActiveTimingProvider?.ProviderName ?? "None"}");
        sb.AppendLine();

        // Summary statistics
        var allEntries = _shadowData.SelectMany(kvp => kvp.Value).ToList();
        var totalComparisons = allEntries.Count;

        if (totalComparisons == 0)
        {
            sb.AppendLine("No timing comparisons recorded.");
            return sb.ToString();
        }

        var differences = allEntries.Select(e => Math.Abs(e.NewTiming - e.LegacyTiming)).ToList();
        var avgDifference = differences.Average();
        var maxDifference = differences.Max();
        var minDifference = differences.Min();
        var stdDev = CalculateStdDev(differences.Select(d => (double)d).ToList(), avgDifference);

        sb.AppendLine("=== Summary Statistics ===");
        sb.AppendLine($"Total Comparisons: {totalComparisons}");
        sb.AppendLine($"Average Difference: {avgDifference:F2}ms");
        sb.AppendLine($"Maximum Difference: {maxDifference:F2}ms");
        sb.AppendLine($"Minimum Difference: {minDifference:F2}ms");
        sb.AppendLine($"Standard Deviation: {stdDev:F2}ms");
        sb.AppendLine();

        // Difference distribution
        sb.AppendLine("=== Difference Distribution ===");
        var ranges = new[] { 0, 10, 25, 50, 100, 250, int.MaxValue };
        var rangeLabels = new[] { "0-10ms", "10-25ms", "25-50ms", "50-100ms", "100-250ms", "250ms+" };

        for (int i = 0; i < ranges.Length - 1; i++)
        {
            var count = differences.Count(d => d >= ranges[i] && d < ranges[i + 1]);
            var percentage = (double)count / totalComparisons * 100;
            sb.AppendLine($"{rangeLabels[i]}: {count} ({percentage:F1}%)");
        }
        sb.AppendLine();

        // Top 10 largest differences
        sb.AppendLine("=== Top 10 Largest Differences ===");
        var topDifferences = allEntries
            .OrderByDescending(e => Math.Abs(e.NewTiming - e.LegacyTiming))
            .Take(10);

        foreach (var entry in topDifferences)
        {
            var diff = Math.Abs(entry.NewTiming - entry.LegacyTiming);
            sb.AppendLine($"{entry.MobileName}: New={entry.NewTiming:F2}ms, Legacy={entry.LegacyTiming:F2}ms, Diff={diff:F2}ms");
        }
        sb.AppendLine();

        // Per-weapon analysis
        sb.AppendLine("=== Per-Weapon Analysis ===");
        var weaponGroups = allEntries.GroupBy(e => e.WeaponName);

        foreach (var group in weaponGroups.OrderBy(g => g.Key))
        {
            var weaponEntries = group.ToList();
            var weaponAvgDiff = weaponEntries.Select(e => Math.Abs(e.NewTiming - e.LegacyTiming)).Average();
            sb.AppendLine($"{group.Key}: {weaponEntries.Count} comparisons, Avg Diff={weaponAvgDiff:F2}ms");
        }

        return sb.ToString();
    }

    private static void ShowReportSummary(Mobile mobile, string report)
    {
        var lines = report.Split('\n');
        foreach (var line in lines.Take(20)) // Show first 20 lines
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                mobile.SendMessage(line.Trim());
            }
        }

        if (lines.Length > 20)
        {
            mobile.SendMessage("... (see full report in Logs directory)");
        }
    }

    private static void ClearShadowData(Mobile mobile)
    {
        _shadowData.Clear();
        _shadowStartTime = DateTime.MinValue;
        _shadowModeActive = false;

        mobile.SendMessage("Shadow data cleared.");
    }

    private static void ShowStatus(Mobile mobile)
    {
        mobile.SendMessage($"Shadow Mode Active: {_shadowModeActive}");

        if (_shadowModeActive)
        {
            var duration = DateTime.UtcNow - _shadowStartTime;
            mobile.SendMessage($"Running for: {duration.TotalMinutes:F1} minutes");
        }

        mobile.SendMessage($"Data Points: {_shadowData.Sum(kvp => kvp.Value.Count)}");
        mobile.SendMessage($"Active Mobiles: {_shadowData.Count}");
    }

    /// <summary>
    /// Records a timing comparison for shadow mode analysis.
    /// </summary>
    public static void RecordTimingComparison(Mobile mobile, Item weapon, int newTiming, int legacyTiming)
    {
        if (!_shadowModeActive || mobile == null)
            return;

        if (!_shadowData.TryGetValue(mobile, out var entries))
        {
            entries = new List<ShadowEntry>();
            _shadowData[mobile] = entries;
        }

        entries.Add(new ShadowEntry
        {
            MobileName = mobile.Name,
            WeaponName = weapon?.Name ?? weapon?.GetType().Name ?? "Unknown",
            NewTiming = newTiming,
            LegacyTiming = legacyTiming,
            Timestamp = DateTime.UtcNow
        });
    }

    private static double CalculateStdDev(List<double> values, double mean)
    {
        if (values.Count <= 1)
            return 0;

        var sumSquares = values.Sum(v => Math.Pow(v - mean, 2));
        return Math.Sqrt(sumSquares / (values.Count - 1));
    }

    /// <summary>
    /// Data structure for shadow mode entries.
    /// </summary>
    private class ShadowEntry
    {
        public string MobileName { get; set; }
        public string WeaponName { get; set; }
        public int NewTiming { get; set; }
        public int LegacyTiming { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
