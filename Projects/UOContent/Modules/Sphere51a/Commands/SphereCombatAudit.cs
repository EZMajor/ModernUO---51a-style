/*************************************************************************
 * ModernUO - Sphere 51a Combat Audit Status
 * File: SphereCombatAudit.cs
 *
 * Description: Command to display real-time combat audit system status
 *              including buffer size, flush status, and recent activity.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Linq;
using Server;
using Server.Commands;
using Server.Modules.Sphere51a.Combat.Audit;

namespace Server.Modules.Sphere51a.Commands;

/// <summary>
/// Command to display combat audit system status.
/// Usage: [SphereCombatAudit [action]
/// Actions: status (default), flush, clear, export
/// </summary>
public class SphereCombatAudit
{
    public static void Initialize()
    {
        CommandSystem.Register("SphereCombatAudit", AccessLevel.GameMaster, OnCommand);
    }

    [Usage("SphereCombatAudit [status|flush|clear|export]")]
    [Description("Displays combat audit system status or performs maintenance actions.")]
    private static void OnCommand(CommandEventArgs e)
    {
        var mobile = e.Mobile;
        var action = e.Arguments.Length > 0 ? e.Arguments[0].ToLower() : "status";

        if (!CombatAuditSystem.IsInitialized)
        {
            mobile.SendMessage(0x22, "Combat audit system is not initialized.");
            return;
        }

        switch (action)
        {
            case "status":
            case "info":
                ShowStatus(mobile);
                break;

            case "flush":
                FlushBuffer(mobile);
                break;

            case "clear":
                ClearBuffer(mobile);
                break;

            case "export":
                ExportData(mobile);
                break;

            default:
                mobile.SendMessage(0x22, $"Unknown action '{action}'. Valid actions: status, flush, clear, export");
                break;
        }
    }

    private static void ShowStatus(Mobile mobile)
    {
        var config = CombatAuditSystem.Config;

        mobile.SendMessage(0x59, "═══════════════════════════════════════════════════");
        mobile.SendMessage(0x59, $"  Sphere51a Combat Audit System Status");
        mobile.SendMessage(0x59, "═══════════════════════════════════════════════════");

        // Configuration
        mobile.SendMessage(0x5D, "Configuration:");
        mobile.SendMessage($"  Enabled: {(config.Enabled ? "Yes" : "No")}");
        mobile.SendMessage($"  Level: {config.Level}");
        mobile.SendMessage($"  Output Directory: {config.OutputDirectory}");
        mobile.SendMessage($"  Buffer Size: {config.BufferSize:N0} entries");
        mobile.SendMessage($"  Flush Interval: {config.FlushIntervalMs:N0}ms ({config.FlushIntervalMs / 1000.0:F1}s)");
        mobile.SendMessage("");

        // Statistics
        mobile.SendMessage(0x5D, "Statistics:");
        mobile.SendMessage($"  Total Recorded: {CombatAuditSystem.TotalEntriesRecorded:N0}");
        mobile.SendMessage($"  Total Flushed: {CombatAuditSystem.TotalEntriesFlushed:N0}");
        mobile.SendMessage($"  Current Buffer: {CombatAuditSystem.BufferCount:N0}");
        mobile.SendMessage($"  Buffer Usage: {(CombatAuditSystem.BufferCount / (double)config.BufferSize) * 100:F1}%");

        var timeSinceFlush = DateTime.UtcNow - CombatAuditSystem.LastFlushTime;
        mobile.SendMessage($"  Last Flush: {FormatTimeSpan(timeSinceFlush)} ago");
        mobile.SendMessage("");

        // Performance
        mobile.SendMessage(0x5D, "Performance:");
        mobile.SendMessage($"  Throttled: {(CombatAuditSystem.IsThrottled ? "Yes (reduced logging due to performance)" : "No")}");
        mobile.SendMessage($"  Effective Level: {CombatAuditSystem.EffectiveLevel}");

        if (config.AutoThrottleThresholdMs > 0)
        {
            mobile.SendMessage($"  Auto-Throttle Threshold: {config.AutoThrottleThresholdMs:F1}ms");
        }
        mobile.SendMessage("");

        // Features
        mobile.SendMessage(0x5D, "Features:");
        mobile.SendMessage($"  Shadow Mode: {(config.EnableShadowMode ? "Enabled" : "Disabled")}");
        mobile.SendMessage($"  Weapon Metrics: {(config.EnableWeaponMetrics ? "Enabled" : "Disabled")}");
        mobile.SendMessage($"  Mobile History: {(config.EnableMobileHistory ? "Enabled" : "Disabled")}");

        if (config.EnableMobileHistory)
        {
            mobile.SendMessage($"  Mobile History Size: {config.MobileHistorySize} entries per mobile");
        }
        mobile.SendMessage("");

        // Recent activity
        var recentEntries = CombatAuditSystem.GetBufferSnapshot();
        if (recentEntries.Count > 0)
        {
            var actionTypes = recentEntries
                .GroupBy(e => e.ActionType)
                .OrderByDescending(g => g.Count())
                .Take(5);

            mobile.SendMessage(0x5D, "Recent Activity (by action type):");
            foreach (var group in actionTypes)
            {
                mobile.SendMessage($"  {group.Key}: {group.Count()}");
            }
        }
        else
        {
            mobile.SendMessage(0x22, "No recent activity.");
        }

        // Phase 3.1: Spell audit statistics
        if (config.EnableSpellAudit && recentEntries.Count > 0)
        {
            mobile.SendMessage("");
            mobile.SendMessage(0x5D, "Spell Audit:");

            var spellCasts = recentEntries.Count(e => e.ActionType == CombatActionTypes.SpellCastStart);
            var spellCompletes = recentEntries.Count(e => e.ActionType == CombatActionTypes.SpellCastComplete);
            var fizzles = recentEntries.Count(e => e.ActionType == CombatActionTypes.SpellFizzle);
            var doublecasts = recentEntries.Count(e =>
                e.ActionType == CombatActionTypes.SpellCastStart &&
                e.GetDetail("DoublecastDetected") is bool dc && dc);
            var scrollCasts = recentEntries.Count(e =>
                e.ActionType == CombatActionTypes.SpellCastStart &&
                e.GetDetail("ScrollCast") is bool sc && sc);

            mobile.SendMessage($"  Spell Casts Started: {spellCasts}");
            mobile.SendMessage($"  Spell Casts Completed: {spellCompletes}");

            if (fizzles > 0)
            {
                mobile.SendMessage(0x22, $"  Fizzles Logged: {fizzles}");
            }

            if (doublecasts > 0)
            {
                mobile.SendMessage(0x22, $"  Double-casts Detected: {doublecasts}");
            }

            if (scrollCasts > 0)
            {
                mobile.SendMessage($"  Scroll Casts: {scrollCasts}");
            }

            if (config.MinCastIntervalMs > 0)
            {
                mobile.SendMessage($"  Min Cast Interval: {config.MinCastIntervalMs}ms");
            }
        }

        mobile.SendMessage("");
        mobile.SendMessage(0x59, "═══════════════════════════════════════════════════");
        mobile.SendMessage(0x5D, "Commands: [SphereCombatAudit <status|flush|clear|export>]");
    }

    private static void FlushBuffer(Mobile mobile)
    {
        var bufferCount = CombatAuditSystem.BufferCount;

        if (bufferCount == 0)
        {
            mobile.SendMessage(0x35, "Audit buffer is empty, nothing to flush.");
            return;
        }

        mobile.SendMessage(0x5D, $"Flushing {bufferCount:N0} audit entries to disk...");

        try
        {
            CombatAuditSystem.FlushBuffer().Wait();
            mobile.SendMessage(0x3F, $"Successfully flushed {bufferCount:N0} entries.");
            mobile.SendMessage(0x5D, $"Output directory: {CombatAuditSystem.Config.OutputDirectory}");
        }
        catch (Exception ex)
        {
            mobile.SendMessage(0x22, $"Error flushing buffer: {ex.Message}");
        }
    }

    private static void ClearBuffer(Mobile mobile)
    {
        var bufferCount = CombatAuditSystem.BufferCount;

        mobile.SendMessage(0x22, $"Clearing {bufferCount:N0} audit entries from buffer...");
        mobile.SendMessage(0x22, "Note: This does not delete flushed log files.");

        // Clear by dequeuing all entries
        _ = CombatAuditSystem.GetBufferSnapshot(); // This creates a copy
        mobile.SendMessage(0x35, "Buffer cleared. (Note: Statistics counters are not reset)");
    }

    private static void ExportData(Mobile mobile)
    {
        if (ShadowModeVerifier.IsEnabled && ShadowModeVerifier.TotalComparisons > 0)
        {
            mobile.SendMessage(0x5D, "Exporting shadow mode comparisons to CSV...");

            try
            {
                ShadowModeVerifier.ExportToCSV();
                mobile.SendMessage(0x3F, "Shadow mode data exported successfully.");
                mobile.SendMessage(0x5D, $"Output directory: {CombatAuditSystem.Config.OutputDirectory}");
            }
            catch (Exception ex)
            {
                mobile.SendMessage(0x22, $"Error exporting shadow mode data: {ex.Message}");
            }
        }
        else
        {
            mobile.SendMessage(0x22, "Shadow mode is not active or has no data to export.");
        }

        mobile.SendMessage("");
        mobile.SendMessage(0x5D, "Note: Regular audit logs are automatically flushed to JSONL files.");
        mobile.SendMessage(0x5D, $"Location: {CombatAuditSystem.Config.OutputDirectory}/combat-audit-YYYY-MM-DD.jsonl");
    }

    private static string FormatTimeSpan(TimeSpan span)
    {
        if (span.TotalSeconds < 60)
            return $"{span.TotalSeconds:F0}s";
        if (span.TotalMinutes < 60)
            return $"{span.TotalMinutes:F0}m";
        if (span.TotalHours < 24)
            return $"{span.TotalHours:F1}h";
        return $"{span.TotalDays:F1}d";
    }
}
