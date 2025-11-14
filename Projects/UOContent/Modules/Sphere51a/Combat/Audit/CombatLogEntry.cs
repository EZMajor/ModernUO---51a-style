using System.Collections.Generic;
using System.Text.Json.Serialization;
using Server;

namespace Server.Modules.Sphere51a.Combat.Audit;

/// <summary>
/// Represents a single combat action recorded by the audit system.
/// Captures timing information, mobile details, and action-specific metadata.
/// Designed for efficient serialization to JSON for persistent logging.
/// </summary>
public class CombatLogEntry
{
    /// <summary>
    /// High-resolution timestamp (Core.TickCount) when the action occurred.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Serial number of the mobile performing the action.
    /// Stored as string for JSON compatibility and null safety.
    /// </summary>
    [JsonPropertyName("serial")]
    public string Serial { get; set; }

    /// <summary>
    /// Display name of the mobile at the time of the action.
    /// Useful for human-readable logs without requiring serial lookup.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// Type of combat action (e.g., "SwingStart", "SwingComplete", "HitResolution", "SpellCast").
    /// </summary>
    [JsonPropertyName("actionType")]
    public string ActionType { get; set; }

    /// <summary>
    /// Name of the timing provider used for this action (e.g., "WeaponTimingProvider", "LegacySphereTimingAdapter").
    /// </summary>
    [JsonPropertyName("timingProvider")]
    public string TimingProvider { get; set; }

    /// <summary>
    /// Expected delay in milliseconds calculated by the timing provider.
    /// For swing actions, this is the weapon speed; for spells, the cast time.
    /// </summary>
    [JsonPropertyName("expectedDelayMs")]
    public double ExpectedDelayMs { get; set; }

    /// <summary>
    /// Actual delay in milliseconds that occurred in practice.
    /// Calculated from timestamp differences between related actions.
    /// </summary>
    [JsonPropertyName("actualDelayMs")]
    public double ActualDelayMs { get; set; }

    /// <summary>
    /// Variance in milliseconds (ActualDelayMs - ExpectedDelayMs).
    /// Positive values indicate slower than expected; negative values indicate faster.
    /// </summary>
    [JsonPropertyName("varianceMs")]
    public double VarianceMs { get; set; }

    /// <summary>
    /// Item ID of the weapon involved in the action (0 if not applicable).
    /// </summary>
    [JsonPropertyName("weaponId")]
    public int WeaponId { get; set; }

    /// <summary>
    /// Name of the weapon involved in the action (null if not applicable).
    /// </summary>
    [JsonPropertyName("weaponName")]
    public string WeaponName { get; set; }

    /// <summary>
    /// Dexterity stat of the mobile at the time of the action.
    /// Affects weapon speed calculations.
    /// </summary>
    [JsonPropertyName("dexterity")]
    public int Dexterity { get; set; }

    /// <summary>
    /// Additional action-specific details stored as key-value pairs.
    /// Examples: hit/miss result, damage dealt, spell ID, cancellation reason.
    /// </summary>
    [JsonPropertyName("details")]
    public Dictionary<string, object> Details { get; set; }

    /// <summary>
    /// Audit level at which this entry was recorded.
    /// Allows filtering logs by detail level during analysis.
    /// </summary>
    [JsonPropertyName("auditLevel")]
    public AuditLevel AuditLevel { get; set; }

    /// <summary>
    /// Creates a new combat log entry with default values.
    /// </summary>
    public CombatLogEntry()
    {
        Details = new Dictionary<string, object>();
    }

    /// <summary>
    /// Creates a new combat log entry with core information.
    /// </summary>
    public CombatLogEntry(Mobile mobile, string actionType, AuditLevel auditLevel)
    {
        Timestamp = global::Server.Core.TickCount;
        Serial = mobile?.Serial.ToString() ?? "0";
        Name = mobile?.Name ?? "Unknown";
        ActionType = actionType;
        AuditLevel = auditLevel;
        Details = new Dictionary<string, object>();
    }

    /// <summary>
    /// Adds a detail entry to the Details dictionary.
    /// </summary>
    public void AddDetail(string key, object value)
    {
        Details[key] = value;
    }

    /// <summary>
    /// Retrieves a detail entry from the Details dictionary.
    /// </summary>
    public object GetDetail(string key)
    {
        return Details.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Checks if this entry represents a timing anomaly (variance exceeds threshold).
    /// </summary>
    public bool IsAnomaly(double thresholdMs = 50.0)
    {
        return System.Math.Abs(VarianceMs) > thresholdMs;
    }

    /// <summary>
    /// Returns a human-readable summary of this log entry.
    /// </summary>
    public override string ToString()
    {
        var variance = VarianceMs >= 0 ? $"+{VarianceMs:F1}ms" : $"{VarianceMs:F1}ms";
        return $"[{Timestamp}] {Name} ({Serial}) - {ActionType}: {ActualDelayMs:F1}ms (expected {ExpectedDelayMs:F1}ms, {variance})";
    }
}

/// <summary>
/// Common action type constants for combat log entries.
/// </summary>
public static class CombatActionTypes
{
    public const string SwingStart = "SwingStart";
    public const string SwingComplete = "SwingComplete";
    public const string HitResolution = "HitResolution";
    public const string SwingCancelled = "SwingCancelled";

    public const string SpellCastStart = "SpellCastStart";
    public const string SpellCastComplete = "SpellCastComplete";
    public const string SpellCastCancelled = "SpellCastCancelled";

    public const string BandageStart = "BandageStart";
    public const string BandageComplete = "BandageComplete";
    public const string BandageCancelled = "BandageCancelled";

    public const string WandStart = "WandStart";
    public const string WandComplete = "WandComplete";
    public const string WandCancelled = "WandCancelled";

    public const string CombatStateChange = "CombatStateChange";
    public const string TimerStateChange = "TimerStateChange";

    public const string ShadowComparison = "ShadowComparison";
    public const string PerformanceMetric = "PerformanceMetric";

    // Magic audit action types (Phase 3)
    public const string SpellFizzle = "SpellFizzle";
    public const string SpellResourceCheck = "SpellResourceCheck";
    public const string SpellInterrupt = "SpellInterrupt";
    public const string SpellDoublecast = "SpellDoublecast";
    public const string SpellManaDrain = "SpellManaDrain";
    public const string SpellReagentConsume = "SpellReagentConsume";
    public const string CrossSystemConflict = "CrossSystemConflict";
}
