/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: SpherePerformance.cs
 *
 * Description: Command to display Sphere51a performance metrics.
 *              Shows tick timing, GC pressure, and system health.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using Server.Commands;
using Server.Modules.Sphere51a.Combat;

namespace Server.Modules.Sphere51a.Commands;

/// <summary>
/// Command to display Sphere51a performance metrics.
/// Usage: [SpherePerformance] or [Perf]
/// </summary>
public class SpherePerformance
{
    public static void Initialize()
    {
        CommandSystem.Register("SpherePerformance", AccessLevel.Player, OnCommand);
        CommandSystem.Register("Perf", AccessLevel.Player, OnCommand);
    }

    [Usage("SpherePerformance")]
    [Description("Displays Sphere51a combat system performance metrics.")]
    private static void OnCommand(CommandEventArgs e)
    {
        var mobile = e.Mobile;

        mobile.SendMessage($"=== Sphere51a Performance Metrics ===");

        // Combat Pulse Metrics
        if (CombatPulse.IsInitialized)
        {
            mobile.SendMessage(CombatPulse.PerformanceMetrics.GetMetricsString());

            // Performance assessment
            var avgTime = CombatPulse.PerformanceMetrics.AverageTickTimeMs;
            var p99Time = CombatPulse.PerformanceMetrics.P99TickTimeMs;
            var activeCount = CombatPulse.ActiveCombatantCount;

            mobile.SendMessage($"Assessment:");

            if (avgTime <= 1.0)
            {
                mobile.SendMessage($"- Excellent: Avg tick time ({avgTime:F3}ms) well under 5ms target");
            }
            else if (avgTime <= 5.0)
            {
                mobile.SendMessage($"- Good: Avg tick time ({avgTime:F3}ms) meets ≤5ms target");
            }
            else if (avgTime <= 10.0)
            {
                mobile.SendMessage($"- Warning: Avg tick time ({avgTime:F3}ms) exceeds target, monitor closely");
            }
            else
            {
                mobile.SendMessage($"- Critical: Avg tick time ({avgTime:F3}ms) severely over target - investigate immediately");
            }

            // Active combatant assessment
            if (activeCount > 500)
            {
                mobile.SendMessage($"- Critical: {activeCount} active combatants may cause performance issues");
            }
            else if (activeCount > 200)
            {
                mobile.SendMessage($"- Warning: {activeCount} active combatants - monitor scaling");
            }
            else if (activeCount > 50)
            {
                mobile.SendMessage($"- Normal: {activeCount} active combatants");
            }
            else
            {
                mobile.SendMessage($"- Low: {activeCount} active combatants");
            }
        }
        else
        {
            mobile.SendMessage("Combat Pulse: Not initialized");
        }

        // System status
        mobile.SendMessage($"System Status:");
        mobile.SendMessage($"- Sphere Enabled: {Server.Modules.Sphere51a.Configuration.SphereConfiguration.Enabled}");
        mobile.SendMessage($"- Global Pulse: {Server.Modules.Sphere51a.Configuration.SphereConfiguration.UseGlobalPulse}");
        mobile.SendMessage($"- Timing Provider: {Server.Modules.Sphere51a.SphereInitializer.ActiveTimingProvider?.ProviderName ?? "None"}");

        // Recommendations
        mobile.SendMessage($"Recommendations:");
        if (!CombatPulse.IsInitialized)
        {
            mobile.SendMessage("- Initialize combat pulse system");
        }
        else if (CombatPulse.PerformanceMetrics.TotalTicks < 100)
        {
            mobile.SendMessage("- Allow more ticks to accumulate for accurate metrics");
        }
        else
        {
            mobile.SendMessage("- Monitor metrics during peak combat activity");
        }

        mobile.SendMessage($"Use [VerifyCombatTick] for system status details");
    }
}
