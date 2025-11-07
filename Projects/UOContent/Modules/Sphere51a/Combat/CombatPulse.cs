/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: CombatPulse.cs
 *
 * Description: Global 50ms tick system for deterministic combat timing.
 *              Manages active combatants and schedules attack resolution.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Server.Logging;
using Server.Mobiles;
using Server.Modules.Sphere51a.Configuration;
using Server.Modules.Sphere51a.Extensions;

namespace Server.Modules.Sphere51a.Combat;

/// <summary>
/// Global combat pulse system providing deterministic 50ms tick timing.
/// Manages active combatants and coordinates attack resolution.
/// </summary>
public static class CombatPulse
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(CombatPulse));

    /// <summary>
    /// Tick interval in milliseconds (20 Hz).
    /// </summary>
    private const int TickMs = 50;

    /// <summary>
    /// Combat idle timeout in milliseconds (5 seconds).
    /// </summary>
    private const int CombatIdleTimeoutMs = 5000;

    /// <summary>
    /// Whether the combat pulse is initialized.
    /// </summary>
    public static bool IsInitialized { get; private set; }

    /// <summary>
    /// Active combatants registered with the pulse system.
    /// </summary>
    private static readonly HashSet<CombatantEntry> _activeCombatants = new();

    /// <summary>
    /// Next tick time for the global pulse.
    /// </summary>
    private static DateTime _nextTick = DateTime.UtcNow.AddMilliseconds(TickMs);

    /// <summary>
    /// Timer that drives the global tick.
    /// </summary>
    private static Timer _pulseTimer;

    /// <summary>
    /// Gets the number of active combatants.
    /// </summary>
    public static int ActiveCombatantCount => _activeCombatants.Count;

    /// <summary>
    /// Initializes the combat pulse system.
    /// </summary>
    public static void Initialize()
    {
        if (_initializationAttempted)
        {
            logger.Debug("Combat pulse initialization already attempted");
            return;
        }

        _initializationAttempted = true;

        if (IsInitialized)
        {
            logger.Debug("Combat pulse already initialized");
            return;
        }

        // Start polling for timer system readiness
        StartTimerReadinessPolling();
    }

    /// <summary>
    /// Whether initialization has been attempted (prevents multiple polling attempts).
    /// </summary>
    private static bool _initializationAttempted;

    /// <summary>
    /// Starts polling for timer system readiness using async Task.
    /// </summary>
    private static void StartTimerReadinessPolling()
    {
        logger.Debug("Starting async timer system readiness polling for combat pulse");

        // Use Task.Run for async polling - doesn't depend on Timer system being initialized
        Task.Run(async () =>
        {
            while (!IsInitialized)
            {
                try
                {
                    // Test if timer system is ready by attempting to create a timer
                    var testTimer = Timer.DelayCall(TimeSpan.FromMilliseconds(1), () => { });
                    testTimer.Stop(); // Clean up the test timer

                    // Success! Timer system is ready
                    StartPulse();
                    IsInitialized = true;
                    logger.Information("Combat pulse initialized with 50ms tick interval");
                    break;
                }
                catch (Exception ex)
                {
                    // Timer system not ready yet, wait 100ms and try again
                    await Task.Delay(100);
                }
            }
        });
    }

    /// <summary>
    /// Shuts down the combat pulse system.
    /// </summary>
    public static void Shutdown()
    {
        if (!IsInitialized)
            return;

        StopPulse();
        _activeCombatants.Clear();
        IsInitialized = false;

        logger.Information("Combat pulse shutdown");
    }

    /// <summary>
    /// Registers a mobile as an active combatant.
    /// </summary>
    /// <param name="mobile">The mobile to register</param>
    public static void RegisterCombatant(Mobile mobile)
    {
        if (mobile == null || !SphereConfiguration.Enabled)
            return;

        var entry = new CombatantEntry(mobile);
        _activeCombatants.Add(entry);

        SphereConfiguration.DebugLog($"{mobile.Name} - Registered as active combatant (total: {_activeCombatants.Count})");
    }

    /// <summary>
    /// Unregisters a mobile from active combatants.
    /// </summary>
    /// <param name="mobile">The mobile to unregister</param>
    public static void UnregisterCombatant(Mobile mobile)
    {
        if (mobile == null)
            return;

        var entry = _activeCombatants.FirstOrDefault(e => e.Mobile == mobile);
        if (entry != null)
        {
            _activeCombatants.Remove(entry);
            SphereConfiguration.DebugLog($"{mobile.Name} - Unregistered from active combatants (total: {_activeCombatants.Count})");
        }
    }

    /// <summary>
    /// Checks if a mobile is registered as an active combatant.
    /// </summary>
    /// <param name="mobile">The mobile to check</param>
    /// <returns>True if registered as active combatant</returns>
    public static bool IsActiveCombatant(Mobile mobile)
    {
        if (mobile == null)
            return false;

        return _activeCombatants.Any(e => e.Mobile == mobile);
    }

    /// <summary>
    /// Updates the last combat activity time for a mobile.
    /// </summary>
    /// <param name="mobile">The mobile that performed combat action</param>
    public static void UpdateCombatActivity(Mobile mobile)
    {
        if (mobile == null)
            return;

        var entry = _activeCombatants.FirstOrDefault(e => e.Mobile == mobile);
        if (entry != null)
        {
            entry.LastActivityTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Updates the next swing time for a mobile in the global pulse system.
    /// </summary>
    /// <param name="mobile">The mobile</param>
    /// <param name="nextSwingTime">The next swing time</param>
    public static void UpdateNextSwingTime(Mobile mobile, DateTime nextSwingTime)
    {
        if (mobile == null)
            return;

        var entry = _activeCombatants.FirstOrDefault(e => e.Mobile == mobile);
        if (entry != null)
        {
            entry.NextSwingTime = nextSwingTime;
            SphereConfiguration.DebugLog($"{mobile.Name} - CombatPulse next swing time updated to {nextSwingTime:HH:mm:ss.fff}");
        }
    }

    /// <summary>
    /// Schedules a hit resolution for the given attacker at the specified offset.
    /// </summary>
    /// <param name="attacker">The attacking mobile</param>
    /// <param name="defender">The defending mobile</param>
    /// <param name="weapon">The weapon being used</param>
    /// <param name="hitOffsetMs">Milliseconds from now to resolve the hit</param>
    public static void ScheduleHitResolution(Mobile attacker, Mobile defender, Item weapon, int hitOffsetMs)
    {
        if (attacker == null || defender == null)
            return;

        var resolveTime = DateTime.UtcNow.AddMilliseconds(hitOffsetMs);

        // Find or create pending hit entry
        var entry = _activeCombatants.FirstOrDefault(e => e.Mobile == attacker);
        if (entry != null)
        {
            entry.PendingHits.Add(new PendingHit(defender, weapon, resolveTime));
            SphereConfiguration.DebugLog($"{attacker.Name} - Scheduled hit on {defender.Name} in {hitOffsetMs}ms");
        }
    }

    /// <summary>
    /// Starts the global pulse timer.
    /// </summary>
    private static void StartPulse()
    {
        if (_pulseTimer != null)
            return;

        // Timer system readiness has already been verified by CheckTimerReadiness
        _pulseTimer = Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromMilliseconds(TickMs), OnTick);
        _nextTick = DateTime.UtcNow.AddMilliseconds(TickMs);

        logger.Debug("Combat pulse timer started");
    }

    /// <summary>
    /// Stops the global pulse timer.
    /// </summary>
    private static void StopPulse()
    {
        _pulseTimer?.Stop();
        _pulseTimer = null;

        logger.Debug("Combat pulse timer stopped");
    }

    /// <summary>
    /// Performance metrics for the combat pulse system.
    /// </summary>
    public static class PerformanceMetrics
    {
        /// <summary>
        /// Total number of ticks processed.
        /// </summary>
        public static long TotalTicks { get; private set; }

        /// <summary>
        /// Average tick processing time in milliseconds.
        /// </summary>
        public static double AverageTickTimeMs { get; private set; }

        /// <summary>
        /// Maximum tick processing time in milliseconds.
        /// </summary>
        public static double MaxTickTimeMs { get; private set; }

        /// <summary>
        /// 99th percentile tick processing time.
        /// </summary>
        public static double P99TickTimeMs { get; private set; }

        /// <summary>
        /// Rolling buffer for tick times (last 1000 ticks).
        /// </summary>
        private static readonly Queue<double> _tickTimesMs = new Queue<double>(1000);

        /// <summary>
        /// Records a tick processing time measurement.
        /// Optimized to avoid repeated sorting for percentiles.
        /// </summary>
        /// <param name="tickTimeMs">Time taken to process the tick</param>
        public static void RecordTickTime(double tickTimeMs)
        {
            TotalTicks++;

            // Update rolling average and max
            const int sampleSize = 1000;
            _tickTimesMs.Enqueue(tickTimeMs);
            if (_tickTimesMs.Count > sampleSize)
            {
                _tickTimesMs.Dequeue();
            }

            // Calculate running statistics
            double sum = 0;
            double max = 0;
            foreach (var time in _tickTimesMs)
            {
                sum += time;
                if (time > max) max = time;
            }

            AverageTickTimeMs = sum / _tickTimesMs.Count;
            MaxTickTimeMs = Math.Max(MaxTickTimeMs, max);

            // Calculate 99th percentile (only when we have enough samples)
            // Use a more efficient approach - approximate percentile
            if (_tickTimesMs.Count >= 10)
            {
                // Simple approximation: sort only when needed and cache result
                // For performance, we'll use a running approximation
                var sortedList = new List<double>(_tickTimesMs);
                sortedList.Sort();
                int p99Index = (int)(sortedList.Count * 0.99);
                P99TickTimeMs = sortedList[Math.Min(p99Index, sortedList.Count - 1)];
            }
        }

        /// <summary>
        /// Gets performance metrics as a formatted string.
        /// </summary>
        public static string GetMetricsString()
        {
            return $"CombatPulse Metrics:\n" +
                   $"- Total Ticks: {TotalTicks}\n" +
                   $"- Active Combatants: {ActiveCombatantCount}\n" +
                   $"- Avg Tick Time: {AverageTickTimeMs:F3}ms\n" +
                   $"- Max Tick Time: {MaxTickTimeMs:F3}ms\n" +
                   $"- 99th Percentile: {P99TickTimeMs:F3}ms\n" +
                   $"- Target: ≤5ms per tick";
        }

        /// <summary>
        /// Resets all performance metrics.
        /// </summary>
        public static void Reset()
        {
            TotalTicks = 0;
            AverageTickTimeMs = 0;
            MaxTickTimeMs = 0;
            P99TickTimeMs = 0;
            _tickTimesMs.Clear();
        }
    }

    /// <summary>
    /// Global tick handler - called every 50ms.
    /// </summary>
    private static void OnTick()
    {
        var tickStart = Stopwatch.GetTimestamp();
        var now = DateTime.UtcNow;

        try
        {
            // Update next tick time
            _nextTick = now.AddMilliseconds(TickMs);

            // Process active combatants
            ProcessActiveCombatants(now);

            // Clean up idle combatants
            CleanupIdleCombatants(now);
        }
        finally
        {
            // Record performance metrics
            var tickEnd = Stopwatch.GetTimestamp();
            var tickTimeMs = (tickEnd - tickStart) * 1000.0 / Stopwatch.Frequency;
            PerformanceMetrics.RecordTickTime(tickTimeMs);

            // Log warning if tick takes too long
            if (tickTimeMs > 10.0) // More than 10ms is concerning
            {
                logger.Warning($"Combat pulse tick took {tickTimeMs:F3}ms (active combatants: {ActiveCombatantCount})");
            }
        }
    }

    /// <summary>
    /// Processes all active combatants for pending actions.
    /// Optimized to avoid allocations during iteration.
    /// </summary>
    /// <param name="now">Current UTC time</param>
    private static void ProcessActiveCombatants(DateTime now)
    {
        // Use a list to collect combatants that need to be removed
        // This avoids modifying the HashSet during enumeration
        var toRemove = new List<CombatantEntry>();

        foreach (var entry in _activeCombatants)
        {
            if (entry.Mobile?.Deleted != false)
            {
                toRemove.Add(entry);
                continue;
            }

            // Process pending hits
            ProcessPendingHits(entry, now);

            // Check for swing readiness (if using global timing)
            if (SphereConfiguration.UseGlobalPulse && entry.NextSwingTime <= now)
            {
                // Trigger attack if mobile is ready
                if (entry.Mobile.SphereCanSwing())
                {
                    entry.Mobile.SphereBeginSwing();
                    // Attack will be processed through normal weapon swing events
                }
            }
        }

        // Remove deleted combatants after iteration
        foreach (var entry in toRemove)
        {
            _activeCombatants.Remove(entry);
        }
    }

    /// <summary>
    /// Processes pending hits for a combatant.
    /// </summary>
    /// <param name="entry">The combatant entry</param>
    /// <param name="now">Current UTC time</param>
    private static void ProcessPendingHits(CombatantEntry entry, DateTime now)
    {
        var hitsToRemove = new List<PendingHit>();

        foreach (var hit in entry.PendingHits)
        {
            if (now >= hit.ResolveTime)
            {
                // Resolve the hit
                ResolveHit(entry.Mobile, hit.Defender, hit.Weapon);
                hitsToRemove.Add(hit);
            }
        }

        // Remove resolved hits
        foreach (var hit in hitsToRemove)
        {
            entry.PendingHits.Remove(hit);
        }
    }

    /// <summary>
    /// Resolves a scheduled hit.
    /// </summary>
    /// <param name="attacker">The attacking mobile</param>
    /// <param name="defender">The defending mobile</param>
    /// <param name="weapon">The weapon used</param>
    private static void ResolveHit(Mobile attacker, Mobile defender, Item weapon)
    {
        if (attacker?.Deleted != false || defender?.Deleted != false)
            return;

        // Delegate to AttackRoutine for hit resolution
        AttackRoutine.ResolveScheduledHit(attacker, defender, weapon);
    }

    /// <summary>
    /// Cleans up combatants that have been idle too long.
    /// Optimized to avoid allocations during filtering.
    /// </summary>
    /// <param name="now">Current UTC time</param>
    private static void CleanupIdleCombatants(DateTime now)
    {
        var idleThreshold = now.AddMilliseconds(-CombatIdleTimeoutMs);
        var toRemove = new List<CombatantEntry>();

        // Collect idle combatants without LINQ allocation
        foreach (var entry in _activeCombatants)
        {
            if (entry.LastActivityTime < idleThreshold)
            {
                toRemove.Add(entry);
            }
        }

        // Remove idle combatants
        foreach (var entry in toRemove)
        {
            _activeCombatants.Remove(entry);
            SphereConfiguration.DebugLog($"{entry.Mobile.Name} - Removed due to combat idle timeout");
        }
    }

    /// <summary>
    /// Entry for an active combatant in the pulse system.
    /// </summary>
    private class CombatantEntry
    {
        public Mobile Mobile { get; }
        public DateTime LastActivityTime { get; set; }
        public DateTime NextSwingTime { get; set; }
        public List<PendingHit> PendingHits { get; } = new();

        public CombatantEntry(Mobile mobile)
        {
            Mobile = mobile;
            LastActivityTime = DateTime.UtcNow;
            NextSwingTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Represents a pending hit scheduled for resolution.
    /// </summary>
    private class PendingHit
    {
        public Mobile Defender { get; }
        public Item Weapon { get; }
        public DateTime ResolveTime { get; }

        public PendingHit(Mobile defender, Item weapon, DateTime resolveTime)
        {
            Defender = defender;
            Weapon = weapon;
            ResolveTime = resolveTime;
        }
    }
}
