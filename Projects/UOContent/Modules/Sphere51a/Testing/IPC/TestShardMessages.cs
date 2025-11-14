using System;
using System.Text.Json.Serialization;

namespace Server.Modules.Sphere51a.Testing.IPC;

/// <summary>
/// Message types for test shard IPC communication.
/// </summary>
public enum MessageType
{
    // Handshake messages
    Ready,
    Connected,

    // Test control messages
    RunTest,
    TestProgress,
    TestComplete,
    TestFailed,

    // Control messages
    Shutdown,
    Heartbeat,

    // Error messages
    Error
}

/// <summary>
/// Base message structure for IPC communication.
/// </summary>
public class TestShardMessage
{
    /// <summary>
    /// Type of message.
    /// </summary>
    [JsonPropertyName("type")]
    public MessageType Type { get; set; }

    /// <summary>
    /// Optional payload data (JSON string).
    /// </summary>
    [JsonPropertyName("payload")]
    public string Payload { get; set; }

    /// <summary>
    /// Timestamp when message was created.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional correlation ID for request/response pairing.
    /// </summary>
    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; set; }

    /// <summary>
    /// Creates a simple message with no payload.
    /// </summary>
    public static TestShardMessage Create(MessageType type)
    {
        return new TestShardMessage { Type = type };
    }

    /// <summary>
    /// Creates a message with a string payload.
    /// </summary>
    public static TestShardMessage Create(MessageType type, string payload)
    {
        return new TestShardMessage { Type = type, Payload = payload };
    }

    /// <summary>
    /// Creates a message with an object payload (serialized to JSON).
    /// </summary>
    public static TestShardMessage Create<T>(MessageType type, T payload)
    {
        return new TestShardMessage
        {
            Type = type,
            Payload = System.Text.Json.JsonSerializer.Serialize(payload)
        };
    }
}

/// <summary>
/// Payload for test execution requests.
/// </summary>
public class RunTestPayload
{
    /// <summary>
    /// ID of the test scenario to run.
    /// </summary>
    [JsonPropertyName("testId")]
    public string TestId { get; set; }

    /// <summary>
    /// Test duration in seconds.
    /// </summary>
    [JsonPropertyName("durationSeconds")]
    public int DurationSeconds { get; set; }

    /// <summary>
    /// Additional test parameters.
    /// </summary>
    [JsonPropertyName("parameters")]
    public TestParameters Parameters { get; set; }
}

/// <summary>
/// Test execution parameters.
/// </summary>
public class TestParameters
{
    /// <summary>
    /// Enable verbose logging.
    /// </summary>
    [JsonPropertyName("verbose")]
    public bool Verbose { get; set; }

    /// <summary>
    /// Enable performance profiling.
    /// </summary>
    [JsonPropertyName("profile")]
    public bool Profile { get; set; }

    /// <summary>
    /// Custom test configuration.
    /// </summary>
    [JsonPropertyName("config")]
    public object Config { get; set; }
}

/// <summary>
/// Payload for test progress updates.
/// </summary>
public class TestProgressPayload
{
    /// <summary>
    /// Current test phase.
    /// </summary>
    [JsonPropertyName("phase")]
    public string Phase { get; set; }

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    [JsonPropertyName("progressPercent")]
    public int ProgressPercent { get; set; }

    /// <summary>
    /// Current status message.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// Elapsed time in seconds.
    /// </summary>
    [JsonPropertyName("elapsedSeconds")]
    public int ElapsedSeconds { get; set; }
}

/// <summary>
/// Payload for test completion results.
/// </summary>
public class TestCompletePayload
{
    /// <summary>
    /// Test scenario ID.
    /// </summary>
    [JsonPropertyName("testId")]
    public string TestId { get; set; }

    /// <summary>
    /// Whether the test passed.
    /// </summary>
    [JsonPropertyName("passed")]
    public bool Passed { get; set; }

    /// <summary>
    /// Total execution time in seconds.
    /// </summary>
    [JsonPropertyName("executionTimeSeconds")]
    public double ExecutionTimeSeconds { get; set; }

    /// <summary>
    /// Test results data.
    /// </summary>
    [JsonPropertyName("results")]
    public TestResults Results { get; set; }

    /// <summary>
    /// Any failure reasons.
    /// </summary>
    [JsonPropertyName("failureReasons")]
    public string[] FailureReasons { get; set; }

    /// <summary>
    /// Test observations/notes.
    /// </summary>
    [JsonPropertyName("observations")]
    public string[] Observations { get; set; }
}

/// <summary>
/// Test results data structure.
/// </summary>
public class TestResults
{
    /// <summary>
    /// Summary metrics.
    /// </summary>
    [JsonPropertyName("summary")]
    public SummaryMetrics Summary { get; set; }

    /// <summary>
    /// Weapon-specific results.
    /// </summary>
    [JsonPropertyName("weaponResults")]
    public WeaponTimingResult[] WeaponResults { get; set; }

    /// <summary>
    /// Baseline comparison data.
    /// </summary>
    [JsonPropertyName("baselineComparison")]
    public BaselineComparison BaselineComparison { get; set; }
}

/// <summary>
/// Summary metrics for test results.
/// </summary>
public class SummaryMetrics
{
    /// <summary>
    /// Total number of actions measured.
    /// </summary>
    [JsonPropertyName("totalActions")]
    public int TotalActions { get; set; }

    /// <summary>
    /// Average variance in milliseconds.
    /// </summary>
    [JsonPropertyName("averageVarianceMs")]
    public double AverageVarianceMs { get; set; }

    /// <summary>
    /// Maximum variance in milliseconds.
    /// </summary>
    [JsonPropertyName("maxVarianceMs")]
    public double MaxVarianceMs { get; set; }

    /// <summary>
    /// Minimum variance in milliseconds.
    /// </summary>
    [JsonPropertyName("minVarianceMs")]
    public double MinVarianceMs { get; set; }

    /// <summary>
    /// Number of measurements within target variance (±25ms).
    /// </summary>
    [JsonPropertyName("withinTargetCount")]
    public int WithinTargetCount { get; set; }

    /// <summary>
    /// Number of outliers with high variance (>50ms).
    /// </summary>
    [JsonPropertyName("outlierCount")]
    public int OutlierCount { get; set; }

    /// <summary>
    /// Accuracy percentage.
    /// </summary>
    [JsonPropertyName("accuracyPercent")]
    public double AccuracyPercent { get; set; }

    /// <summary>
    /// Outlier percentage.
    /// </summary>
    [JsonPropertyName("outlierPercent")]
    public double OutlierPercent { get; set; }
}

/// <summary>
/// Weapon-specific timing results.
/// </summary>
public class WeaponTimingResult
{
    /// <summary>
    /// Weapon name.
    /// </summary>
    [JsonPropertyName("weaponName")]
    public string WeaponName { get; set; }

    /// <summary>
    /// Number of swings measured.
    /// </summary>
    [JsonPropertyName("swingCount")]
    public int SwingCount { get; set; }

    /// <summary>
    /// Expected delay in milliseconds.
    /// </summary>
    [JsonPropertyName("expectedDelayMs")]
    public double ExpectedDelayMs { get; set; }

    /// <summary>
    /// Actual average delay in milliseconds.
    /// </summary>
    [JsonPropertyName("actualAvgDelayMs")]
    public double ActualAvgDelayMs { get; set; }

    /// <summary>
    /// Average variance in milliseconds.
    /// </summary>
    [JsonPropertyName("varianceMs")]
    public double VarianceMs { get; set; }

    /// <summary>
    /// Whether this weapon passed the test.
    /// </summary>
    [JsonPropertyName("passed")]
    public bool Passed { get; set; }

    /// <summary>
    /// Per-dexterity variation results.
    /// </summary>
    [JsonPropertyName("dexVariations")]
    public DexVariationResult[] DexVariations { get; set; }
}

/// <summary>
/// Dexterity-specific variation results.
/// </summary>
public class DexVariationResult
{
    /// <summary>
    /// Dexterity value.
    /// </summary>
    [JsonPropertyName("dexterity")]
    public int Dexterity { get; set; }

    /// <summary>
    /// Number of swings at this dexterity.
    /// </summary>
    [JsonPropertyName("swingCount")]
    public int SwingCount { get; set; }

    /// <summary>
    /// Expected delay in milliseconds.
    /// </summary>
    [JsonPropertyName("expectedDelayMs")]
    public double ExpectedDelayMs { get; set; }

    /// <summary>
    /// Actual average delay in milliseconds.
    /// </summary>
    [JsonPropertyName("actualAvgDelayMs")]
    public double ActualAvgDelayMs { get; set; }

    /// <summary>
    /// Variance in milliseconds.
    /// </summary>
    [JsonPropertyName("varianceMs")]
    public double VarianceMs { get; set; }

    /// <summary>
    /// Whether this dexterity variation passed.
    /// </summary>
    [JsonPropertyName("passed")]
    public bool Passed { get; set; }
}

/// <summary>
/// Baseline comparison data.
/// </summary>
public class BaselineComparison
{
    /// <summary>
    /// Whether results meet baseline expectations.
    /// </summary>
    [JsonPropertyName("meetsBaseline")]
    public bool MeetsBaseline { get; set; }

    /// <summary>
    /// Baseline accuracy percentage.
    /// </summary>
    [JsonPropertyName("baselineAccuracyPercent")]
    public double BaselineAccuracyPercent { get; set; }

    /// <summary>
    /// Difference from baseline.
    /// </summary>
    [JsonPropertyName("differencePercent")]
    public double DifferencePercent { get; set; }

    /// <summary>
    /// Performance trend.
    /// </summary>
    [JsonPropertyName("trend")]
    public string Trend { get; set; }
}

/// <summary>
/// Error payload for failed operations.
/// </summary>
public class ErrorPayload
{
    /// <summary>
    /// Error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; }

    /// <summary>
    /// Error code/category.
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; }

    /// <summary>
    /// Additional error details.
    /// </summary>
    [JsonPropertyName("details")]
    public object Details { get; set; }
}
