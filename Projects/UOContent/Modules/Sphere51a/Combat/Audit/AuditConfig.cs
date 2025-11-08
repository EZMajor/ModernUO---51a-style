using System.Text.Json.Serialization;

namespace Server.Modules.Sphere51a.Combat.Audit;

/// <summary>
/// Configuration settings for the Sphere51a combat audit system.
/// Controls audit logging behavior, performance, and retention policies.
/// </summary>
public class AuditConfig
{
    /// <summary>
    /// Enable or disable the entire audit system.
    /// When disabled, no audit logging occurs and audit-related processing is skipped.
    /// Default: true
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The granularity level for audit logging.
    /// Higher levels include more detailed information but may impact performance.
    /// Default: Standard
    /// </summary>
    [JsonPropertyName("level")]
    public AuditLevel Level { get; set; } = AuditLevel.Standard;

    /// <summary>
    /// Directory path for audit log files, relative to the server root.
    /// Default: "Logs/CombatAudit"
    /// </summary>
    [JsonPropertyName("outputDirectory")]
    public string OutputDirectory { get; set; } = "Logs/CombatAudit";

    /// <summary>
    /// Maximum number of audit entries to keep in memory before flushing to disk.
    /// Larger buffers reduce disk I/O but increase memory usage.
    /// Default: 10000
    /// </summary>
    [JsonPropertyName("bufferSize")]
    public int BufferSize { get; set; } = 10000;

    /// <summary>
    /// Interval in milliseconds between automatic flushes of the audit buffer to disk.
    /// Lower values provide more real-time logging but increase disk I/O.
    /// Default: 5000 (5 seconds)
    /// </summary>
    [JsonPropertyName("flushIntervalMs")]
    public int FlushIntervalMs { get; set; } = 5000;

    /// <summary>
    /// Enable shadow mode: execute both current and legacy timing providers in parallel
    /// to compare results without affecting gameplay. Useful for verification and testing.
    /// Default: false
    /// </summary>
    [JsonPropertyName("enableShadowMode")]
    public bool EnableShadowMode { get; set; } = false;

    /// <summary>
    /// Maximum age in days for audit log files before they are archived or deleted.
    /// Set to 0 to disable automatic cleanup.
    /// Default: 7 days
    /// </summary>
    [JsonPropertyName("retentionDays")]
    public int RetentionDays { get; set; } = 7;

    /// <summary>
    /// Maximum size in megabytes for a single audit log file before rotation.
    /// Set to 0 to disable size-based rotation.
    /// Default: 100 MB
    /// </summary>
    [JsonPropertyName("maxFileSizeMB")]
    public int MaxFileSizeMB { get; set; } = 100;

    /// <summary>
    /// Maximum number of audit entries to process per CombatPulse tick.
    /// Limits performance impact by throttling audit processing under heavy load.
    /// Default: 250
    /// </summary>
    [JsonPropertyName("maxEntriesPerTick")]
    public int MaxEntriesPerTick { get; set; } = 250;

    /// <summary>
    /// If CombatPulse tick time exceeds this threshold (in milliseconds),
    /// automatically reduce audit level to preserve server performance.
    /// Set to 0 to disable auto-throttling.
    /// Default: 10 ms
    /// </summary>
    [JsonPropertyName("autoThrottleThresholdMs")]
    public double AutoThrottleThresholdMs { get; set; } = 10.0;

    /// <summary>
    /// Enable detailed performance metrics collection for weapons.
    /// Tracks per-weapon statistics (hit count, miss count, avg delay, variance).
    /// Default: true
    /// </summary>
    [JsonPropertyName("enableWeaponMetrics")]
    public bool EnableWeaponMetrics { get; set; } = true;

    /// <summary>
    /// Enable per-mobile audit history tracking.
    /// Allows querying recent combat actions for specific mobiles.
    /// Uses ConditionalWeakTable for automatic GC cleanup.
    /// Default: true
    /// </summary>
    [JsonPropertyName("enableMobileHistory")]
    public bool EnableMobileHistory { get; set; } = true;

    /// <summary>
    /// Maximum number of recent actions to keep in per-mobile history.
    /// Only applies if EnableMobileHistory is true.
    /// Default: 100
    /// </summary>
    [JsonPropertyName("mobileHistorySize")]
    public int MobileHistorySize { get; set; } = 100;

    /// <summary>
    /// Validates configuration values and applies corrections if needed.
    /// Called automatically during deserialization.
    /// </summary>
    public void Validate()
    {
        // Ensure buffer size is reasonable
        if (BufferSize < 100)
            BufferSize = 100;
        else if (BufferSize > 100000)
            BufferSize = 100000;

        // Ensure flush interval is reasonable
        if (FlushIntervalMs < 1000)
            FlushIntervalMs = 1000;
        else if (FlushIntervalMs > 60000)
            FlushIntervalMs = 60000;

        // Ensure output directory is not empty
        if (string.IsNullOrWhiteSpace(OutputDirectory))
            OutputDirectory = "Logs/CombatAudit";

        // Ensure retention days is non-negative
        if (RetentionDays < 0)
            RetentionDays = 0;

        // Ensure max file size is reasonable
        if (MaxFileSizeMB < 0)
            MaxFileSizeMB = 0;
        else if (MaxFileSizeMB > 1000)
            MaxFileSizeMB = 1000;

        // Ensure entries per tick is reasonable
        if (MaxEntriesPerTick < 10)
            MaxEntriesPerTick = 10;
        else if (MaxEntriesPerTick > 1000)
            MaxEntriesPerTick = 1000;

        // Ensure auto-throttle threshold is reasonable
        if (AutoThrottleThresholdMs < 0)
            AutoThrottleThresholdMs = 0;
        else if (AutoThrottleThresholdMs > 100)
            AutoThrottleThresholdMs = 100;

        // Ensure mobile history size is reasonable
        if (MobileHistorySize < 10)
            MobileHistorySize = 10;
        else if (MobileHistorySize > 1000)
            MobileHistorySize = 1000;
    }
}
