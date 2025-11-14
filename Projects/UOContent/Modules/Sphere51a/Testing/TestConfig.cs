/*************************************************************************
 * ModernUO - Sphere 51a Test Configuration Model
 * File: TestConfig.cs
 *
 * Description: Configuration model for headless testing framework.
 *              Loads test-config.json with baseline tracking and test scenarios.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Server.Logging;

namespace Server.Modules.Sphere51a.Testing;

/// <summary>
/// Root configuration for Sphere51a headless testing framework.
/// </summary>
public class TestConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(TestConfig));

    [JsonPropertyName("testSettings")]
    public TestSettings TestSettings { get; set; } = new TestSettings();

    [JsonPropertyName("baselines")]
    public Dictionary<string, BaselineMetrics> Baselines { get; set; } = new Dictionary<string, BaselineMetrics>();

    [JsonPropertyName("scenarios")]
    public TestScenarios Scenarios { get; set; } = new TestScenarios();

    [JsonPropertyName("reportSettings")]
    public ReportSettings ReportSettings { get; set; } = new ReportSettings();

    [JsonPropertyName("thresholds")]
    public ThresholdSettings Thresholds { get; set; } = new ThresholdSettings();

    /// <summary>
    /// Loads test configuration from JSON file.
    /// </summary>
    public static TestConfig Load(string configPath = null)
    {
        configPath ??= Path.Combine(global::Server.Core.BaseDirectory, "Projects/UOContent/Modules/Sphere51a/Configuration/test-config.json");

        if (!File.Exists(configPath))
        {
            logger.Warning("Test config not found at {Path}, using defaults", configPath);
            return new TestConfig();
        }

        try
        {
            var json = File.ReadAllText(configPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            var config = JsonSerializer.Deserialize<TestConfig>(json, options);
            logger.Information("Test config loaded from {Path}", configPath);
            return config;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to load test config from {Path}", configPath);
            return new TestConfig();
        }
    }

    /// <summary>
    /// Saves current configuration back to file (for baseline updates).
    /// </summary>
    public void Save(string configPath = null)
    {
        configPath ??= Path.Combine(global::Server.Core.BaseDirectory, "Projects/UOContent/Modules/Sphere51a/Configuration/test-config.json");

        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(configPath, json);
            logger.Information("Test config saved to {Path}", configPath);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to save test config to {Path}", configPath);
        }
    }
}

/// <summary>
/// General test execution settings.
/// </summary>
public class TestSettings
{
    [JsonPropertyName("defaultDurationSeconds")]
    public int DefaultDurationSeconds { get; set; } = 60;

    [JsonPropertyName("warmupSeconds")]
    public int WarmupSeconds { get; set; } = 5;

    [JsonPropertyName("enableDetailedLogging")]
    public bool EnableDetailedLogging { get; set; } = false;

    [JsonPropertyName("exitOnFailure")]
    public bool ExitOnFailure { get; set; } = true;
}

/// <summary>
/// Baseline metrics for regression detection.
/// </summary>
public class BaselineMetrics
{
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("accuracy_percent")]
    public double AccuracyPercent { get; set; }

    [JsonPropertyName("target_precision_ms")]
    public double TargetPrecisionMs { get; set; }

    [JsonPropertyName("max_variance_ms")]
    public double MaxVarianceMs { get; set; }

    [JsonPropertyName("max_outliers_percent")]
    public double MaxOutliersPercent { get; set; }

    [JsonPropertyName("max_double_casts")]
    public int MaxDoubleCasts { get; set; }

    [JsonPropertyName("max_fizzle_rate_percent")]
    public double MaxFizzleRatePercent { get; set; }

    [JsonPropertyName("max_tick_time_ms")]
    public double MaxTickTimeMs { get; set; }

    [JsonPropertyName("max_throttle_events")]
    public int MaxThrottleEvents { get; set; }

    [JsonPropertyName("min_throughput_actions_per_sec")]
    public int MinThroughputActionsPerSec { get; set; }

    [JsonPropertyName("max_memory_mb")]
    public int MaxMemoryMb { get; set; }

    [JsonPropertyName("last_updated")]
    public string LastUpdated { get; set; }

    [JsonPropertyName("last_build")]
    public string LastBuild { get; set; }
}

/// <summary>
/// Test scenario configurations.
/// </summary>
public class TestScenarios
{
    [JsonPropertyName("weapon_timing")]
    public WeaponTimingScenarioConfig WeaponTiming { get; set; } = new WeaponTimingScenarioConfig();

    [JsonPropertyName("spell_timing")]
    public SpellTimingScenarioConfig SpellTiming { get; set; } = new SpellTimingScenarioConfig();

    [JsonPropertyName("stress_test")]
    public StressTestScenarioConfig StressTest { get; set; } = new StressTestScenarioConfig();
}

public class WeaponTimingScenarioConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("duration_seconds")]
    public int DurationSeconds { get; set; } = 60;

    [JsonPropertyName("weapons")]
    public List<WeaponTestConfig> Weapons { get; set; } = new List<WeaponTestConfig>();

    [JsonPropertyName("min_swings_per_weapon")]
    public int MinSwingsPerWeapon { get; set; } = 20;
}

public class WeaponTestConfig
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("speed")]
    public int Speed { get; set; }

    [JsonPropertyName("test_dex_values")]
    public List<int> TestDexValues { get; set; } = new List<int>();
}

public class SpellTimingScenarioConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("duration_seconds")]
    public int DurationSeconds { get; set; } = 60;

    [JsonPropertyName("spells")]
    public List<SpellTestConfig> Spells { get; set; } = new List<SpellTestConfig>();

    [JsonPropertyName("test_int_values")]
    public List<int> TestIntValues { get; set; } = new List<int>();

    [JsonPropertyName("test_double_cast_detection")]
    public bool TestDoubleCastDetection { get; set; } = true;

    [JsonPropertyName("min_cast_interval_ms")]
    public int MinCastIntervalMs { get; set; } = 400;
}

public class SpellTestConfig
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("circle")]
    public int Circle { get; set; }

    [JsonPropertyName("min_casts")]
    public int MinCasts { get; set; }
}

public class StressTestScenarioConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("duration_seconds")]
    public int DurationSeconds { get; set; } = 120;

    [JsonPropertyName("concurrent_combatants")]
    public int ConcurrentCombatants { get; set; } = 20;

    [JsonPropertyName("weapon_mix")]
    public List<string> WeaponMix { get; set; } = new List<string>();

    [JsonPropertyName("spell_mix")]
    public List<string> SpellMix { get; set; } = new List<string>();

    [JsonPropertyName("measure_tick_performance")]
    public bool MeasureTickPerformance { get; set; } = true;
}

/// <summary>
/// Report generation settings.
/// </summary>
public class ReportSettings
{
    [JsonPropertyName("output_directory")]
    public string OutputDirectory { get; set; } = "AuditReports";

    [JsonPropertyName("archive_directory")]
    public string ArchiveDirectory { get; set; } = "AuditReports/Archive";

    [JsonPropertyName("retention_days_local")]
    public int RetentionDaysLocal { get; set; } = 7;

    [JsonPropertyName("retention_days_ci")]
    public int RetentionDaysCi { get; set; } = 30;

    [JsonPropertyName("generate_latest_summary")]
    public bool GenerateLatestSummary { get; set; } = true;

    [JsonPropertyName("include_detailed_entries")]
    public bool IncludeDetailedEntries { get; set; } = false;

    [JsonPropertyName("export_raw_jsonl")]
    public bool ExportRawJsonl { get; set; } = true;
}

/// <summary>
/// Variance thresholds for pass/fail determination.
/// </summary>
public class ThresholdSettings
{
    [JsonPropertyName("warning_variance_ms")]
    public double WarningVarianceMs { get; set; } = 25;

    [JsonPropertyName("error_variance_ms")]
    public double ErrorVarianceMs { get; set; } = 50;

    [JsonPropertyName("critical_variance_ms")]
    public double CriticalVarianceMs { get; set; } = 100;

    [JsonPropertyName("max_acceptable_outliers")]
    public int MaxAcceptableOutliers { get; set; } = 5;
}
