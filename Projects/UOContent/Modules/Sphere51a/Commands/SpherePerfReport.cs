/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: SpherePerfReport.cs
 *
 * Description: Performance analysis and reporting command for Sphere51a.
 *              Generates comprehensive performance reports from collected metrics.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.IO;
using Server.Commands;
using Server.Modules.Sphere51a.Combat;

namespace Server.Modules.Sphere51a.Commands;

/// <summary>
/// Command to generate comprehensive performance reports for Sphere51a.
/// Usage: [SpherePerfReport] or [SpherePerfReport save]
/// </summary>
public class SpherePerfReport
{
    public static void Initialize()
    {
        CommandSystem.Register("SpherePerfReport", AccessLevel.Player, OnCommand);
        CommandSystem.Register("PerfReport", AccessLevel.Player, OnCommand);
    }

    [Usage("SpherePerfReport [save]")]
    [Description("Generates comprehensive performance report for Sphere51a combat system.")]
    private static void OnCommand(CommandEventArgs e)
    {
        var mobile = e.Mobile;
        var saveToFile = e.Length > 0 && e.GetString(0).ToLower() == "save";

        var report = GeneratePerformanceReport();

        if (saveToFile)
        {
            SaveReportToFile(report);
            mobile.SendMessage("Performance report saved to file.");
        }
        else
        {
            // Display report in chunks to avoid message limits
            var lines = report.Split('\n');
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    mobile.SendMessage(line);
                }
            }
        }
    }

    private static string GeneratePerformanceReport()
    {
        var report = new System.Text.StringBuilder();

        report.AppendLine("╔════════════════════════════════════════════════════════════════╗");
        report.AppendLine("║                 Sphere51a Performance Report                  ║");
        report.AppendLine("╚════════════════════════════════════════════════════════════════╝");
        report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();

        // System Status
        report.AppendLine("📊 SYSTEM STATUS");
        report.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        report.AppendLine($"Sphere51a Enabled: {Server.Modules.Sphere51a.Configuration.SphereConfiguration.Enabled}");
        report.AppendLine($"Global Pulse Active: {Server.Modules.Sphere51a.Configuration.SphereConfiguration.UseGlobalPulse}");
        report.AppendLine($"Combat Pulse Initialized: {CombatPulse.IsInitialized}");
        report.AppendLine($"Active Timing Provider: {Server.Modules.Sphere51a.SphereInitializer.ActiveTimingProvider?.ProviderName ?? "None"}");
        report.AppendLine();

        // Performance Metrics
        report.AppendLine("⚡ PERFORMANCE METRICS");
        report.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        if (CombatPulse.IsInitialized)
        {
            var activeCombatants = CombatPulse.ActiveCombatantCount;

            report.AppendLine($"Total Ticks Processed: {CombatPulse.PerformanceMetrics.TotalTicks:N0}");
            report.AppendLine($"Active Combatants: {activeCombatants}");
            report.AppendLine($"Average Tick Time: {CombatPulse.PerformanceMetrics.AverageTickTimeMs:F3}ms");
            report.AppendLine($"Maximum Tick Time: {CombatPulse.PerformanceMetrics.MaxTickTimeMs:F3}ms");
            report.AppendLine($"99th Percentile: {CombatPulse.PerformanceMetrics.P99TickTimeMs:F3}ms");
            report.AppendLine();

            // Performance Assessment
            report.AppendLine("🎯 PERFORMANCE ASSESSMENT");
            report.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            var avgTime = CombatPulse.PerformanceMetrics.AverageTickTimeMs;
            var p99Time = CombatPulse.PerformanceMetrics.P99TickTimeMs;

            // Overall Performance Rating
            string performanceRating;
            if (avgTime <= 1.0)
                performanceRating = "EXCELLENT (≤1ms target achieved)";
            else if (avgTime <= 5.0)
                performanceRating = "GOOD (≤5ms target achieved)";
            else if (avgTime <= 10.0)
                performanceRating = "ACCEPTABLE (monitor closely)";
            else
                performanceRating = "CONCERNING (investigate immediately)";

            report.AppendLine($"Overall Rating: {performanceRating}");

            // Detailed Analysis
            report.AppendLine();
            report.AppendLine("Detailed Analysis:");
            report.AppendLine($"- Target: ≤5ms average tick time");
            report.AppendLine($"- Current: {avgTime:F3}ms average");
            report.AppendLine($"- Status: {(avgTime <= 5.0 ? "✅ PASS" : "❌ FAIL")}");

            // 99th Percentile Analysis
            report.AppendLine();
            report.AppendLine("Latency Analysis:");
            report.AppendLine($"- 99th Percentile: {p99Time:F3}ms");
            report.AppendLine($"- Status: {(p99Time <= 10.0 ? "✅ GOOD" : "⚠️ HIGH")}");

            // Scaling Analysis
            report.AppendLine();
            report.AppendLine("Scaling Analysis:");
            if (activeCombatants > 0)
            {
                var timePerCombatant = avgTime / activeCombatants;
                report.AppendLine($"- Time per Combatant: {timePerCombatant:F6}ms");
                report.AppendLine($"- Scaling: O({activeCombatants}) as expected");
            }
            else
            {
                report.AppendLine("- No active combatants (idle system)");
            }

            // Combatant Load Assessment
            report.AppendLine();
            report.AppendLine("Load Assessment:");
            string loadRating;
            if (activeCombatants <= 50)
                loadRating = "VERY LIGHT";
            else if (activeCombatants <= 200)
                loadRating = "LIGHT";
            else if (activeCombatants <= 500)
                loadRating = "MODERATE";
            else if (activeCombatants <= 1000)
                loadRating = "HEAVY";
            else
                loadRating = "EXTREME";

            report.AppendLine($"Current Load: {loadRating} ({activeCombatants} combatants)");
        }
        else
        {
            report.AppendLine("❌ Combat Pulse not initialized - no metrics available");
        }

        // Recommendations
        report.AppendLine();
        report.AppendLine("💡 RECOMMENDATIONS");
        report.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        if (!CombatPulse.IsInitialized)
        {
            report.AppendLine("- Initialize Sphere51a combat system");
            report.AppendLine("- Enable global pulse timing");
        }
        else
        {
            var avgTime = CombatPulse.PerformanceMetrics.AverageTickTimeMs;
            var activeCount = CombatPulse.ActiveCombatantCount;

            if (avgTime > 10.0)
            {
                report.AppendLine("- CRITICAL: Investigate high tick times immediately");
                report.AppendLine("- Check for memory pressure or GC issues");
                report.AppendLine("- Review combatant registration/unregistration patterns");
            }
            else if (avgTime > 5.0)
            {
                report.AppendLine("- Monitor tick times closely");
                report.AppendLine("- Consider optimizing frequent allocations");
            }
            else
            {
                report.AppendLine("- Performance is excellent, continue monitoring");
            }

            if (activeCount > 500)
            {
                report.AppendLine("- High combatant count detected");
                report.AppendLine("- Ensure cleanup is working properly");
                report.AppendLine("- Monitor for scaling issues");
            }
        }

        // Technical Details
        report.AppendLine();
        report.AppendLine("🔧 TECHNICAL DETAILS");
        report.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        report.AppendLine($"Tick Interval: {Server.Modules.Sphere51a.Configuration.SphereConfiguration.GlobalTickMs}ms (20 Hz)");
        report.AppendLine($"Idle Timeout: {Server.Modules.Sphere51a.Configuration.SphereConfiguration.CombatIdleTimeoutMs}ms");
        report.AppendLine($"Weapon Config: {Server.Modules.Sphere51a.Configuration.SphereConfiguration.WeaponTimingConfigPath}");
        report.AppendLine($"Independent Timers: {Server.Modules.Sphere51a.Configuration.SphereConfiguration.IndependentTimers}");

        report.AppendLine();
        report.AppendLine("══════════════════════════════════════════════════════════════════");
        report.AppendLine("End of Sphere51a Performance Report");
        report.AppendLine("══════════════════════════════════════════════════════════════════");

        return report.ToString();
    }

    private static void SaveReportToFile(string report)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filename = $"sphere51a_perf_report_{timestamp}.txt";
            var path = Path.Combine("docs", "perf", filename);

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            File.WriteAllText(path, report);

            Console.WriteLine($"Sphere51a performance report saved to: {path}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save performance report: {ex.Message}");
        }
    }
}
