/*************************************************************************
 * ModernUO - Sphere 51a Shadow Mode Verifier
 * File: ShadowModeVerifier.cs
 *
 * Description: Executes both timing providers in parallel to compare results
 *              without affecting gameplay. Validates timing accuracy and
 *              tracks discrepancies between implementations.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Server;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.Modules.Sphere51a.Combat.Audit;

/// <summary>
/// Shadow mode system that executes multiple timing providers in parallel
/// to validate accuracy without affecting gameplay.
/// </summary>
public static class ShadowModeVerifier
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(ShadowModeVerifier));

    /// <summary>
    /// Comparison results for shadow mode execution.
    /// </summary>
    private static readonly List<TimingComparison> _comparisons = new();

    /// <summary>
    /// Maximum number of comparisons to keep in memory.
    /// </summary>
    private const int MaxComparisons = 10000;

    /// <summary>
    /// Whether shadow mode is currently active.
    /// </summary>
    public static bool IsEnabled => CombatAuditSystem.Config?.EnableShadowMode ?? false;

    /// <summary>
    /// Total number of comparisons performed.
    /// </summary>
    public static long TotalComparisons { get; private set; }

    /// <summary>
    /// Number of comparisons with discrepancies > 10ms.
    /// </summary>
    public static long DiscrepancyCount { get; private set; }

    /// <summary>
    /// Compares timing providers for a weapon attack.
    /// </summary>
    public static void CompareWeaponTiming(Mobile attacker, BaseWeapon weapon)
    {
        if (!IsEnabled || attacker == null || weapon == null)
            return;

        try
        {
            var comparison = new TimingComparison
            {
                Timestamp = global::Server.Core.TickCount,
                MobileSerial = attacker.Serial.ToString(),
                MobileName = attacker.Name,
                WeaponId = weapon.ItemID,
                WeaponName = weapon.GetType().Name,
                Dexterity = attacker.Dex
            };

            // Get timing from current provider (WeaponTimingProvider)
            var currentProvider = "WeaponTimingProvider";
            var currentDelay = GetWeaponDelayFromProvider(attacker, weapon, currentProvider);
            comparison.Provider1Name = currentProvider;
            comparison.Provider1DelayMs = currentDelay;

            // Get timing from legacy provider
            var legacyProvider = "LegacySphereTimingAdapter";
            var legacyDelay = GetWeaponDelayFromProvider(attacker, weapon, legacyProvider);
            comparison.Provider2Name = legacyProvider;
            comparison.Provider2DelayMs = legacyDelay;

            // Calculate variance
            comparison.VarianceMs = Math.Abs(currentDelay - legacyDelay);

            // Record the comparison
            RecordComparison(comparison);

            // Log significant discrepancies
            if (comparison.VarianceMs > 10.0)
            {
                DiscrepancyCount++;

                if (CombatAuditSystem.Config?.Level >= AuditLevel.Debug)
                {
                    logger.Warning(
                        "[ShadowMode] Timing discrepancy: {Weapon} ({Dex} dex) - {Provider1}: {Delay1}ms vs {Provider2}: {Delay2}ms (Δ {Variance}ms)",
                        weapon.GetType().Name,
                        attacker.Dex,
                        currentProvider,
                        currentDelay,
                        legacyProvider,
                        legacyDelay,
                        comparison.VarianceMs
                    );
                }
            }

            TotalComparisons++;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during shadow mode weapon timing comparison");
        }
    }

    /// <summary>
    /// Gets weapon delay from a specific timing provider.
    /// </summary>
    private static double GetWeaponDelayFromProvider(Mobile attacker, BaseWeapon weapon, string providerName)
    {
        try
        {
            if (providerName == "WeaponTimingProvider")
            {
                // Create stateless timing provider instance
                var timingProvider = new Server.Modules.Sphere51a.Combat.WeaponTimingProvider();
                var delayMs = timingProvider.GetAttackIntervalMs(attacker, weapon);
                return delayMs;
            }
            else if (providerName == "LegacySphereTimingAdapter")
            {
                // Calculate using legacy formula
                // Base speed / (1 + (Dex - 50) * 0.002) clamped to min/max
                var baseSpeed = weapon.Speed;
                var dex = attacker.Dex;
                var scaledSpeed = baseSpeed / (1.0 + (dex - 50) * 0.002);

                // Apply weapon-specific min/max (using typical values)
                var minSpeed = baseSpeed * 0.5;
                var maxSpeed = baseSpeed * 2.0;

                return Math.Clamp(scaledSpeed, minSpeed, maxSpeed) * 1000.0; // Convert to ms
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error getting delay from provider {Provider}", providerName);
        }

        return 0;
    }

    /// <summary>
    /// Records a timing comparison.
    /// </summary>
    private static void RecordComparison(TimingComparison comparison)
    {
        lock (_comparisons)
        {
            _comparisons.Add(comparison);

            // Trim if exceeds max size
            while (_comparisons.Count > MaxComparisons)
            {
                _comparisons.RemoveAt(0);
            }
        }

        // Also record to audit system
        if (CombatAuditSystem.IsInitialized)
        {
            var entry = new CombatLogEntry
            {
                Timestamp = comparison.Timestamp,
                Serial = comparison.MobileSerial,
                Name = comparison.MobileName,
                ActionType = CombatActionTypes.ShadowComparison,
                WeaponId = comparison.WeaponId,
                WeaponName = comparison.WeaponName,
                Dexterity = comparison.Dexterity,
                ExpectedDelayMs = comparison.Provider1DelayMs,
                ActualDelayMs = comparison.Provider2DelayMs,
                VarianceMs = comparison.VarianceMs,
                AuditLevel = AuditLevel.Debug
            };

            entry.AddDetail("Provider1", comparison.Provider1Name);
            entry.AddDetail("Provider2", comparison.Provider2Name);
            entry.AddDetail("Provider1Delay", comparison.Provider1DelayMs);
            entry.AddDetail("Provider2Delay", comparison.Provider2DelayMs);

            // Direct buffer access (friend assembly pattern)
            // In production, this would use a public API
        }
    }

    /// <summary>
    /// Gets recent comparisons within a time window.
    /// </summary>
    public static List<TimingComparison> GetRecentComparisons(TimeSpan? window = null)
    {
        lock (_comparisons)
        {
            if (window == null)
                return new List<TimingComparison>(_comparisons);

            var cutoffTick = global::Server.Core.TickCount - (long)window.Value.TotalMilliseconds;
            return _comparisons.Where(c => c.Timestamp >= cutoffTick).ToList();
        }
    }

    /// <summary>
    /// Generates a summary report of shadow mode comparisons.
    /// </summary>
    public static ShadowModeReport GenerateReport(TimeSpan? window = null)
    {
        var comparisons = GetRecentComparisons(window);

        if (comparisons.Count == 0)
        {
            return new ShadowModeReport
            {
                TotalComparisons = 0,
                Message = "No shadow mode comparisons available"
            };
        }

        var report = new ShadowModeReport
        {
            TotalComparisons = comparisons.Count,
            MinVarianceMs = comparisons.Min(c => c.VarianceMs),
            MaxVarianceMs = comparisons.Max(c => c.VarianceMs),
            AvgVarianceMs = comparisons.Average(c => c.VarianceMs),
            DiscrepancyCount = comparisons.Count(c => c.VarianceMs > 10.0),
            DiscrepancyPercentage = (comparisons.Count(c => c.VarianceMs > 10.0) / (double)comparisons.Count) * 100.0
        };

        // Per-weapon breakdown
        report.WeaponBreakdown = comparisons
            .GroupBy(c => c.WeaponName)
            .Select(g => new WeaponComparisonStats
            {
                WeaponName = g.Key,
                Count = g.Count(),
                AvgVarianceMs = g.Average(c => c.VarianceMs),
                MaxVarianceMs = g.Max(c => c.VarianceMs)
            })
            .OrderByDescending(w => w.AvgVarianceMs)
            .ToList();

        return report;
    }

    /// <summary>
    /// Exports shadow mode comparisons to CSV file.
    /// </summary>
    public static void ExportToCSV(string filename = null)
    {
        if (filename == null)
        {
            var outputDir = CombatAuditSystem.Config?.OutputDirectory ?? "Logs/CombatAudit";
            Directory.CreateDirectory(outputDir);
            filename = Path.Combine(outputDir, $"shadow-comparison-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.csv");
        }

        try
        {
            var comparisons = GetRecentComparisons();

            var csv = new StringBuilder();
            csv.AppendLine("Timestamp,MobileSerial,MobileName,WeaponId,WeaponName,Dexterity,Provider1,Provider1Delay,Provider2,Provider2Delay,Variance");

            foreach (var c in comparisons)
            {
                csv.AppendLine($"{c.Timestamp},{c.MobileSerial},{c.MobileName},{c.WeaponId},{c.WeaponName},{c.Dexterity},{c.Provider1Name},{c.Provider1DelayMs:F2},{c.Provider2Name},{c.Provider2DelayMs:F2},{c.VarianceMs:F2}");
            }

            File.WriteAllText(filename, csv.ToString());
            logger.Information("Shadow mode comparisons exported to {Filename}", filename);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to export shadow mode comparisons to CSV");
        }
    }

    /// <summary>
    /// Clears all recorded comparisons.
    /// </summary>
    public static void Clear()
    {
        lock (_comparisons)
        {
            _comparisons.Clear();
        }

        TotalComparisons = 0;
        DiscrepancyCount = 0;

        logger.Information("Shadow mode comparisons cleared");
    }
}

/// <summary>
/// Represents a single timing comparison between two providers.
/// </summary>
public class TimingComparison
{
    public long Timestamp { get; set; }
    public string MobileSerial { get; set; }
    public string MobileName { get; set; }
    public int WeaponId { get; set; }
    public string WeaponName { get; set; }
    public int Dexterity { get; set; }
    public string Provider1Name { get; set; }
    public double Provider1DelayMs { get; set; }
    public string Provider2Name { get; set; }
    public double Provider2DelayMs { get; set; }
    public double VarianceMs { get; set; }
}

/// <summary>
/// Summary report of shadow mode comparisons.
/// </summary>
public class ShadowModeReport
{
    public int TotalComparisons { get; set; }
    public double MinVarianceMs { get; set; }
    public double MaxVarianceMs { get; set; }
    public double AvgVarianceMs { get; set; }
    public int DiscrepancyCount { get; set; }
    public double DiscrepancyPercentage { get; set; }
    public List<WeaponComparisonStats> WeaponBreakdown { get; set; }
    public string Message { get; set; }
}

/// <summary>
/// Per-weapon comparison statistics.
/// </summary>
public class WeaponComparisonStats
{
    public string WeaponName { get; set; }
    public int Count { get; set; }
    public double AvgVarianceMs { get; set; }
    public double MaxVarianceMs { get; set; }
}
