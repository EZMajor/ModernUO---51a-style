/*************************************************************************
 * ModernUO - Sphere 51a Combat Audit System
 * File: CombatAuditSystem.cs
 *
 * Description: Core audit orchestrator for combat verification and logging.
 *              Provides real-time monitoring, performance tracking, and
 *              persistent audit trail for Sphere 51a combat timing.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Server;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Modules.Sphere51a.Events;
using Server.Spells;

namespace Server.Modules.Sphere51a.Combat.Audit;

/// <summary>
/// Central audit system for Sphere51a combat verification.
/// Tracks all combat actions, validates timing accuracy, and provides
/// persistent logging for PvP fairness and debugging.
/// </summary>
public static class CombatAuditSystem
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(CombatAuditSystem));

    /// <summary>
    /// Whether the audit system has been initialized.
    /// </summary>
    public static bool IsInitialized { get; private set; }

    /// <summary>
    /// Current audit configuration.
    /// </summary>
    public static AuditConfig Config { get; private set; }

    /// <summary>
    /// Circular buffer for recent audit entries.
    /// Thread-safe for concurrent access from event handlers.
    /// </summary>
    private static readonly ConcurrentQueue<CombatLogEntry> _auditBuffer = new();

    /// <summary>
    /// Per-mobile combat action history.
    /// Uses ConditionalWeakTable for automatic GC cleanup.
    /// </summary>
    private static readonly ConditionalWeakTable<Mobile, List<CombatLogEntry>> _mobileHistory = new();

    /// <summary>
    /// Timer for periodic buffer flushing to disk.
    /// </summary>
    private static Timer _flushTimer;

    /// <summary>
    /// Total number of audit entries recorded since initialization.
    /// </summary>
    public static long TotalEntriesRecorded { get; private set; }

    /// <summary>
    /// Total number of audit entries flushed to disk.
    /// </summary>
    public static long TotalEntriesFlushed { get; private set; }

    /// <summary>
    /// Current buffer size (entries waiting to be flushed).
    /// </summary>
    public static int BufferCount => _auditBuffer.Count;

    /// <summary>
    /// Last time the buffer was flushed to disk.
    /// </summary>
    public static DateTime LastFlushTime { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Last audit action timestamp per mobile (for calculating actual delays).
    /// </summary>
    private static readonly ConditionalWeakTable<Mobile, ActionTimestamp> _lastActionTimestamps = new();

    /// <summary>
    /// Whether the audit system is currently throttled due to performance concerns.
    /// </summary>
    public static bool IsThrottled { get; private set; }

    /// <summary>
    /// Current effective audit level (may be reduced from Config.Level if throttled).
    /// </summary>
    public static AuditLevel EffectiveLevel => IsThrottled ? AuditLevel.Standard : Config?.Level ?? AuditLevel.None;

    /// <summary>
    /// Initializes the combat audit system with the specified configuration.
    /// </summary>
    public static void Initialize(AuditConfig config)
    {
        if (IsInitialized)
        {
            logger.Warning("Combat audit system already initialized");
            return;
        }

        if (config == null)
        {
            logger.Error("Cannot initialize combat audit system: config is null");
            return;
        }

        // Validate and store configuration
        config.Validate();
        Config = config;

        if (!config.Enabled)
        {
            logger.Information("Combat audit system disabled by configuration");
            return;
        }

        // Ensure output directory exists
        try
        {
            Directory.CreateDirectory(config.OutputDirectory);
            logger.Information($"Combat audit system output directory: {config.OutputDirectory}");
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Failed to create audit output directory: {config.OutputDirectory}");
            return;
        }

        // Subscribe to Sphere combat events
        RegisterEventHandlers();

        // Start periodic flush timer
        _flushTimer = Timer.DelayCall(
            TimeSpan.FromMilliseconds(config.FlushIntervalMs),
            TimeSpan.FromMilliseconds(config.FlushIntervalMs),
            OnFlushTimer
        );

        IsInitialized = true;
        logger.Information($"Combat audit system initialized (Level: {config.Level}, Buffer: {config.BufferSize}, Flush: {config.FlushIntervalMs}ms)");
    }

    /// <summary>
    /// Shuts down the audit system and flushes remaining entries.
    /// </summary>
    public static void Shutdown()
    {
        if (!IsInitialized)
            return;

        logger.Information("Combat audit system shutting down...");

        // Unsubscribe from events
        UnregisterEventHandlers();

        // Stop flush timer
        _flushTimer?.Stop();
        _flushTimer = null;

        // Final flush
        FlushBuffer().Wait();

        IsInitialized = false;
        logger.Information($"Combat audit system shutdown complete (Total entries recorded: {TotalEntriesRecorded}, flushed: {TotalEntriesFlushed})");
    }

    /// <summary>
    /// Registers event handlers for combat actions.
    /// </summary>
    private static void RegisterEventHandlers()
    {
        SphereEvents.OnWeaponSwing += OnWeaponSwing;
        SphereEvents.OnWeaponSwingComplete += OnWeaponSwingComplete;
        SphereEvents.OnSpellCastBegin += OnSpellCastBegin;
        SphereEvents.OnSpellCastComplete += OnSpellCastComplete;
        SphereEvents.OnBandageUse += OnBandageUse;
        SphereEvents.OnBandageUseComplete += OnBandageUseComplete;
        SphereEvents.OnWandUse += OnWandUse;
        SphereEvents.OnWandUseComplete += OnWandUseComplete;

        logger.Debug("Combat audit event handlers registered");
    }

    /// <summary>
    /// Unregisters event handlers.
    /// </summary>
    private static void UnregisterEventHandlers()
    {
        SphereEvents.OnWeaponSwing -= OnWeaponSwing;
        SphereEvents.OnWeaponSwingComplete -= OnWeaponSwingComplete;
        SphereEvents.OnSpellCastBegin -= OnSpellCastBegin;
        SphereEvents.OnSpellCastComplete -= OnSpellCastComplete;
        SphereEvents.OnBandageUse -= OnBandageUse;
        SphereEvents.OnBandageUseComplete -= OnBandageUseComplete;
        SphereEvents.OnWandUse -= OnWandUse;
        SphereEvents.OnWandUseComplete -= OnWandUseComplete;

        logger.Debug("Combat audit event handlers unregistered");
    }

    #region Event Handlers

    private static void OnWeaponSwing(object sender, WeaponSwingEventArgs e)
    {
        if (!ShouldRecord(AuditLevel.Standard))
            return;

        var entry = new CombatLogEntry(e.Attacker, CombatActionTypes.SwingStart, EffectiveLevel);

        if (e.Weapon != null)
        {
            entry.WeaponId = e.Weapon.ItemID;
            entry.WeaponName = e.Weapon.GetType().Name;
        }

        if (e.Attacker != null)
        {
            entry.Dexterity = e.Attacker.Dex;
            entry.ExpectedDelayMs = e.Delay.TotalMilliseconds;
        }

        if (e.Defender != null)
            entry.AddDetail("Defender", e.Defender.Serial.ToString());

        entry.AddDetail("Cancelled", e.Cancelled);

        RecordEntry(entry, e.Attacker);
        UpdateActionTimestamp(e.Attacker, CombatActionTypes.SwingStart);
    }

    private static void OnWeaponSwingComplete(object sender, WeaponSwingEventArgs e)
    {
        if (!ShouldRecord(AuditLevel.Standard))
            return;

        var entry = new CombatLogEntry(e.Attacker, CombatActionTypes.SwingComplete, EffectiveLevel);

        if (e.Weapon != null)
        {
            entry.WeaponId = e.Weapon.ItemID;
            entry.WeaponName = e.Weapon.GetType().Name;
        }

        if (e.Attacker != null)
        {
            entry.Dexterity = e.Attacker.Dex;
            entry.ExpectedDelayMs = e.Delay.TotalMilliseconds;

            // Calculate actual delay from last swing start
            var actualDelayMs = GetTimeSinceLastAction(e.Attacker, CombatActionTypes.SwingStart);
            if (actualDelayMs > 0)
            {
                entry.ActualDelayMs = actualDelayMs;
                entry.VarianceMs = actualDelayMs - entry.ExpectedDelayMs;
            }
        }

        if (e.Defender != null)
            entry.AddDetail("Defender", e.Defender.Serial.ToString());

        RecordEntry(entry, e.Attacker);
        UpdateActionTimestamp(e.Attacker, CombatActionTypes.SwingComplete);
    }

    private static void OnSpellCastBegin(object sender, SpellCastEventArgs e)
    {
        if (!ShouldRecord(AuditLevel.Standard) || !Config.EnableSpellAudit)
            return;

        var entry = new CombatLogEntry(e.Caster, CombatActionTypes.SpellCastStart, EffectiveLevel);

        if (e.Spell != null)
        {
            entry.AddDetail("SpellId", SpellRegistry.GetRegistryNumber(e.Spell));
            entry.AddDetail("SpellName", e.Spell.Name);
            entry.ExpectedDelayMs = e.Spell.CastDelayBase.TotalMilliseconds;

            // Phase 3.1: Scroll detection
            if (Config.ValidateScrollUsage && e.Spell.Scroll != null)
            {
                entry.AddDetail("ScrollCast", true);
                entry.AddDetail("ScrollType", e.Spell.Scroll.GetType().Name);
            }

            // Phase 3.1: Double-cast detection
            if (Config.DetectDoublecast)
            {
                var lastCastTime = GetTimeSinceLastAction(e.Caster, CombatActionTypes.SpellCastStart);
                var threshold = Config.MinCastIntervalMs > 0 ? Config.MinCastIntervalMs : 400;

                if (lastCastTime > 0 && lastCastTime < threshold)
                {
                    entry.AddDetail("DoublecastDetected", true);
                    entry.AddDetail("TimeSinceLastCast", lastCastTime);
                    entry.AddDetail("ThresholdMs", threshold);

                    if (EffectiveLevel >= AuditLevel.Detailed)
                    {
                        logger.Warning(
                            "[SpellAudit] Double-cast detected: {Name} - {Spell}, interval {Interval:F1}ms (threshold {Threshold}ms)",
                            e.Caster.Name,
                            e.Spell.Name,
                            lastCastTime,
                            threshold
                        );
                    }
                }
            }
        }

        entry.AddDetail("Cancelled", e.Cancelled);

        RecordEntry(entry, e.Caster);
        UpdateActionTimestamp(e.Caster, CombatActionTypes.SpellCastStart);
    }

    private static void OnSpellCastComplete(object sender, SpellCastEventArgs e)
    {
        if (!ShouldRecord(AuditLevel.Standard) || !Config.EnableSpellAudit)
            return;

        var entry = new CombatLogEntry(e.Caster, CombatActionTypes.SpellCastComplete, EffectiveLevel);

        if (e.Spell != null)
        {
            entry.AddDetail("SpellId", SpellRegistry.GetRegistryNumber(e.Spell));
            entry.AddDetail("SpellName", e.Spell.Name);
            entry.ExpectedDelayMs = e.Spell.CastDelayBase.TotalMilliseconds;

            // Calculate actual delay from cast start
            var actualDelayMs = GetTimeSinceLastAction(e.Caster, CombatActionTypes.SpellCastStart);
            if (actualDelayMs > 0)
            {
                entry.ActualDelayMs = actualDelayMs;
                entry.VarianceMs = actualDelayMs - entry.ExpectedDelayMs;
            }
        }

        RecordEntry(entry, e.Caster);
        UpdateActionTimestamp(e.Caster, CombatActionTypes.SpellCastComplete);
    }

    private static void OnBandageUse(object sender, BandageUseEventArgs e)
    {
        if (!ShouldRecord(AuditLevel.Detailed))
            return;

        var entry = new CombatLogEntry(e.Healer, CombatActionTypes.BandageStart, EffectiveLevel);
        entry.ExpectedDelayMs = e.Delay.TotalMilliseconds;

        if (e.Patient != null)
            entry.AddDetail("Patient", e.Patient.Serial.ToString());

        entry.AddDetail("Cancelled", e.Cancelled);

        RecordEntry(entry, e.Healer);
        UpdateActionTimestamp(e.Healer, CombatActionTypes.BandageStart);
    }

    private static void OnBandageUseComplete(object sender, BandageUseEventArgs e)
    {
        if (!ShouldRecord(AuditLevel.Detailed))
            return;

        var entry = new CombatLogEntry(e.Healer, CombatActionTypes.BandageComplete, EffectiveLevel);
        entry.ExpectedDelayMs = e.Delay.TotalMilliseconds;

        // Calculate actual delay from bandage start
        var actualDelayMs = GetTimeSinceLastAction(e.Healer, CombatActionTypes.BandageStart);
        if (actualDelayMs > 0)
        {
            entry.ActualDelayMs = actualDelayMs;
            entry.VarianceMs = actualDelayMs - entry.ExpectedDelayMs;
        }

        if (e.Patient != null)
            entry.AddDetail("Patient", e.Patient.Serial.ToString());

        RecordEntry(entry, e.Healer);
        UpdateActionTimestamp(e.Healer, CombatActionTypes.BandageComplete);
    }

    private static void OnWandUse(object sender, WandUseEventArgs e)
    {
        if (!ShouldRecord(AuditLevel.Detailed))
            return;

        var entry = new CombatLogEntry(e.User, CombatActionTypes.WandStart, EffectiveLevel);
        entry.ExpectedDelayMs = e.Delay.TotalMilliseconds;

        if (e.Wand != null)
        {
            entry.WeaponId = e.Wand.ItemID;
            entry.WeaponName = e.Wand.GetType().Name;
        }

        if (e.Spell != null)
        {
            entry.AddDetail("SpellId", SpellRegistry.GetRegistryNumber(e.Spell));
            entry.AddDetail("SpellName", e.Spell.Name);
        }

        entry.AddDetail("Cancelled", e.Cancelled);

        RecordEntry(entry, e.User);
        UpdateActionTimestamp(e.User, CombatActionTypes.WandStart);
    }

    private static void OnWandUseComplete(object sender, WandUseEventArgs e)
    {
        if (!ShouldRecord(AuditLevel.Detailed))
            return;

        var entry = new CombatLogEntry(e.User, CombatActionTypes.WandComplete, EffectiveLevel);
        entry.ExpectedDelayMs = e.Delay.TotalMilliseconds;

        // Calculate actual delay from wand start
        var actualDelayMs = GetTimeSinceLastAction(e.User, CombatActionTypes.WandStart);
        if (actualDelayMs > 0)
        {
            entry.ActualDelayMs = actualDelayMs;
            entry.VarianceMs = actualDelayMs - entry.ExpectedDelayMs;
        }

        if (e.Wand != null)
        {
            entry.WeaponId = e.Wand.ItemID;
            entry.WeaponName = e.Wand.GetType().Name;
        }

        if (e.Spell != null)
        {
            entry.AddDetail("SpellId", SpellRegistry.GetRegistryNumber(e.Spell));
            entry.AddDetail("SpellName", e.Spell.Name);
        }

        RecordEntry(entry, e.User);
        UpdateActionTimestamp(e.User, CombatActionTypes.WandComplete);
    }

    #endregion

    #region Core Recording Logic

    /// <summary>
    /// Determines if an entry should be recorded based on current audit level.
    /// </summary>
    private static bool ShouldRecord(AuditLevel requiredLevel)
    {
        if (!IsInitialized || Config == null || !Config.Enabled)
            return false;

        return EffectiveLevel >= requiredLevel;
    }

    /// <summary>
    /// Records an audit entry to the buffer and optionally to mobile history.
    /// </summary>
    private static void RecordEntry(CombatLogEntry entry, Mobile mobile)
    {
        if (entry == null)
            return;

        // Add to main buffer
        _auditBuffer.Enqueue(entry);
        TotalEntriesRecorded++;

        // Trim buffer if it exceeds max size
        while (_auditBuffer.Count > Config.BufferSize)
        {
            _auditBuffer.TryDequeue(out _);
        }

        // Add to per-mobile history if enabled
        if (Config.EnableMobileHistory && mobile != null)
        {
            var history = _mobileHistory.GetOrCreateValue(mobile);
            lock (history)
            {
                history.Add(entry);

                // Trim history if it exceeds max size
                while (history.Count > Config.MobileHistorySize)
                {
                    history.RemoveAt(0);
                }
            }
        }

        // Check for anomalies
        if (entry.IsAnomaly() && EffectiveLevel >= AuditLevel.Detailed)
        {
            logger.Warning($"[CombatAudit] Timing anomaly detected: {entry}");
        }
    }

    /// <summary>
    /// Updates the last action timestamp for a mobile.
    /// </summary>
    private static void UpdateActionTimestamp(Mobile mobile, string actionType)
    {
        if (mobile == null)
            return;

        var timestamp = _lastActionTimestamps.GetOrCreateValue(mobile);
        timestamp.Update(actionType, global::Server.Core.TickCount);
    }

    /// <summary>
    /// Gets the time in milliseconds since the last action of a specific type.
    /// Returns 0 if no previous action found.
    /// </summary>
    private static double GetTimeSinceLastAction(Mobile mobile, string actionType)
    {
        if (mobile == null)
            return 0;

        if (!_lastActionTimestamps.TryGetValue(mobile, out var timestamp))
            return 0;

        return timestamp.GetTimeSince(actionType);
    }

    #endregion

    #region Buffer Flushing

    /// <summary>
    /// Timer callback for periodic buffer flushing.
    /// </summary>
    private static void OnFlushTimer()
    {
        if (!IsInitialized || _auditBuffer.IsEmpty)
            return;

        // Async flush to avoid blocking main thread
        Task.Run(async () =>
        {
            try
            {
                await FlushBuffer();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error during audit buffer flush");
            }
        });
    }

    /// <summary>
    /// Flushes the audit buffer to disk asynchronously.
    /// </summary>
    public static async Task FlushBuffer()
    {
        if (_auditBuffer.IsEmpty)
            return;

        var entries = new List<CombatLogEntry>();

        // Dequeue all entries
        while (_auditBuffer.TryDequeue(out var entry))
        {
            entries.Add(entry);
        }

        if (entries.Count == 0)
            return;

        try
        {
            var filename = Path.Combine(
                Config.OutputDirectory,
                $"combat-audit-{DateTime.UtcNow:yyyy-MM-dd}.jsonl"
            );

            // Append entries as JSON lines (JSONL format)
            await using var writer = new StreamWriter(filename, append: true);

            foreach (var entry in entries)
            {
                var json = JsonSerializer.Serialize(entry);
                await writer.WriteLineAsync(json);
            }

            TotalEntriesFlushed += entries.Count;
            LastFlushTime = DateTime.UtcNow;

            if (EffectiveLevel >= AuditLevel.Debug)
            {
                logger.Debug($"[CombatAudit] Flushed {entries.Count} entries to {filename}");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Failed to flush {entries.Count} audit entries");
        }
    }

    #endregion

    #region Public Query API

    /// <summary>
    /// Gets recent combat history for a specific mobile.
    /// </summary>
    public static List<CombatLogEntry> GetMobileHistory(Mobile mobile, TimeSpan? window = null)
    {
        if (mobile == null || !Config.EnableMobileHistory)
            return new List<CombatLogEntry>();

        if (!_mobileHistory.TryGetValue(mobile, out var history))
            return new List<CombatLogEntry>();

        lock (history)
        {
            if (window == null)
                return new List<CombatLogEntry>(history);

            var cutoffTick = global::Server.Core.TickCount - (long)window.Value.TotalMilliseconds;
            return history.Where(e => e.Timestamp >= cutoffTick).ToList();
        }
    }

    /// <summary>
    /// Gets all entries currently in the buffer.
    /// </summary>
    public static List<CombatLogEntry> GetBufferSnapshot()
    {
        return _auditBuffer.ToList();
    }

    /// <summary>
    /// Checks performance metrics and enables throttling if needed.
    /// Should be called by CombatPulse or other performance monitoring systems.
    /// </summary>
    public static void CheckPerformanceThrottle(double tickTimeMs)
    {
        if (Config == null || Config.AutoThrottleThresholdMs <= 0)
            return;

        if (tickTimeMs > Config.AutoThrottleThresholdMs && !IsThrottled)
        {
            IsThrottled = true;
            logger.Warning($"[CombatAudit] Performance throttle enabled (tick time: {tickTimeMs:F2}ms > threshold: {Config.AutoThrottleThresholdMs}ms)");
        }
        else if (tickTimeMs < Config.AutoThrottleThresholdMs * 0.8 && IsThrottled)
        {
            IsThrottled = false;
            logger.Information($"[CombatAudit] Performance throttle disabled (tick time: {tickTimeMs:F2}ms)");
        }
    }

    #endregion

    #region Helper Classes

    /// <summary>
    /// Tracks timestamps for different action types per mobile.
    /// </summary>
    private class ActionTimestamp
    {
        private readonly Dictionary<string, long> _timestamps = new();

        public void Update(string actionType, long tickCount)
        {
            _timestamps[actionType] = tickCount;
        }

        public double GetTimeSince(string actionType)
        {
            if (!_timestamps.TryGetValue(actionType, out var lastTick))
                return 0;

            return global::Server.Core.TickCount - lastTick;
        }
    }

    #endregion
}
