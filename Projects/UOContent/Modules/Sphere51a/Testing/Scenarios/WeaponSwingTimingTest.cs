/*************************************************************************
 * ModernUO - Sphere 51a Weapon Swing Timing Test
 * File: WeaponSwingTimingTest.cs
 *
 * Description: Tests weapon swing timing accuracy across different weapon
 *              types and dexterity values. Validates ±25ms precision target.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Server.Items;
using Server.Modules.Sphere51a.Combat;
using Server.Modules.Sphere51a.Combat.Audit;
using Server.Modules.Sphere51a.Events;
using Server.Modules.Sphere51a.Testing.Reports;

namespace Server.Modules.Sphere51a.Testing.Scenarios;

/// <summary>
/// Tests weapon swing timing accuracy for Sphere51a combat system.
/// </summary>
public class WeaponSwingTimingTest : TestScenario
{
    public override string ScenarioId => "weapon_timing";
    public override string ScenarioName => "Weapon Swing Timing Test";

    private Mobile _attacker;
    private Mobile _defender;
    private WeaponTimingProvider _timingProvider;

    private Dictionary<string, List<TimingMeasurement>> _measurements = new();
    private int _totalSwings = 0;
    private long _testDurationMs;
    private long _testStartTick;

    protected override bool Setup()
    {
        try
        {
            logger.Information("Setting up weapon timing test...");

            // Get test duration from config
            var scenarioConfig = Config.Scenarios.WeaponTiming;
            _testDurationMs = (scenarioConfig?.DurationSeconds ?? 60) * 1000;

            // Create test mobiles
            _attacker = TestMobileFactory.CreateCombatant("TestAttacker",
                str: 100, dex: 100, intel: 50, hits: 100);
            _defender = TestMobileFactory.CreateDummy("TestDefender");

            // Reset combat timers to ensure SphereCanSwing returns true
            _attacker.NextCombatTime = 0;
            _defender.NextCombatTime = 0;

            TestMobiles.Add(_attacker);
            TestMobiles.Add(_defender);

            // Initialize timing provider
            _timingProvider = new WeaponTimingProvider();

            // Initialize measurement storage
            _measurements.Clear();

            logger.Information("Test setup complete. Duration: {Duration}s", _testDurationMs / 1000);
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to setup weapon timing test");
            return false;
        }
    }

    protected override void RunTest()
    {
        try
        {
            logger.Information("Starting weapon timing test execution...");

            _testStartTick = global::Server.Core.TickCount;
            var scenarioConfig = Config.Scenarios.WeaponTiming;

            if (scenarioConfig?.Weapons == null || scenarioConfig.Weapons.Count == 0)
            {
                logger.Warning("No weapons configured for testing, using defaults");
                RunDefaultWeaponTests();
                return;
            }

            // Test each configured weapon
            foreach (var weaponConfig in scenarioConfig.Weapons)
            {
                TestWeaponType(weaponConfig);
            }

            StopTest();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during weapon timing test execution");
            StopTest();
        }
    }

    private void RunDefaultWeaponTests()
    {
        var defaultWeapons = new[] { "Katana", "Longsword", "Broadsword", "Dagger", "WarAxe" };
        var defaultDexValues = new[] { 25, 50, 75, 100, 150 };

        foreach (var weaponType in defaultWeapons)
        {
            var weapon = TestMobileFactory.CreateWeapon(weaponType);
            _attacker.EquipItem(weapon); // Equip the weapon

            foreach (var dex in defaultDexValues)
            {
                _attacker.RawDex = dex;
                _attacker.Stam = dex;

                var minSwings = Config.Scenarios.WeaponTiming.MinSwingsPerWeapon;
                TestWeaponWithDex(weapon, dex, minSwings);
            }

            weapon.Delete();
        }
    }

    private void TestWeaponType(WeaponTestConfig weaponConfig)
    {
        LogVerbose("Testing weapon: {Weapon}", weaponConfig.Type);

        var weapon = TestMobileFactory.CreateWeapon(weaponConfig.Type);
        _attacker.EquipItem(weapon);

        foreach (var dex in weaponConfig.TestDexValues)
        {
            // Check if we've exceeded test duration
            if (global::Server.Core.TickCount - _testStartTick >= _testDurationMs)
            {
                LogVerbose("Test duration exceeded, stopping");
                break;
            }

            _attacker.RawDex = dex;
            _attacker.Stam = dex;

            var minSwings = Config.Scenarios.WeaponTiming.MinSwingsPerWeapon;
            TestWeaponWithDex(weapon, dex, minSwings);
        }

        weapon.Delete();
    }

    private void TestWeaponWithDex(BaseWeapon weapon, int dex, int minSwings)
    {
        var key = $"{weapon.GetType().Name}_Dex{dex}";

        if (!_measurements.ContainsKey(key))
        {
            _measurements[key] = new List<TimingMeasurement>();
        }

        LogVerbose("Testing {Weapon} with Dex {Dex}, target {Min} swings", weapon.GetType().Name, dex, minSwings);

        for (int i = 0; i < minSwings; i++)
        {
            // Get expected delay from timing provider
            var expectedDelayMs = _timingProvider.GetAttackIntervalMs(_attacker, weapon);

            try
            {
                // Perform weapon swing with event triggering
                PerformWeaponSwing(_attacker, _defender, weapon, expectedDelayMs);

                _totalSwings++;

                LogVerbose("Swing {Index}: Completed", i + 1);
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Error during swing test");
            }

            // Small delay between swings to allow system to stabilize
            System.Threading.Thread.Sleep(50);
        }
    }

    /// <summary>
    /// Performs a weapon swing by raising Sphere51a events for audit capture.
    /// </summary>
    private void PerformWeaponSwing(Mobile attacker, Mobile defender, BaseWeapon weapon, double expectedDelayMs)
    {
        // Ensure mobiles are in valid combat state
        attacker.Combatant = defender;
        // Mobiles are already created with Map and Region set in TestMobileFactory

        // Initialize combat state if not already done
        SphereEvents.RaiseCombatEnter(attacker);
        SphereEvents.RaiseCombatEnter(defender);

        // Record swing start time for test measurement
        var swingStartTime = DateTime.UtcNow;

        // Raise weapon swing start event (this triggers audit recording)
        var swingArgs = SphereEvents.RaiseWeaponSwing(attacker, defender, weapon);

        if (!swingArgs.Cancelled)
        {
            // Wait for the actual delay (use event-provided delay if modified by handlers)
            var actualDelay = swingArgs.Delay.TotalMilliseconds > 0 ? swingArgs.Delay : TimeSpan.FromMilliseconds(expectedDelayMs);
            System.Threading.Thread.Sleep((int)actualDelay.TotalMilliseconds);

            // Raise weapon swing complete event
            SphereEvents.RaiseWeaponSwingComplete(attacker, defender, weapon, actualDelay);

            // For test measurement (optional, since audit system handles this)
            var actualDelayMs = (DateTime.UtcNow - swingStartTime).TotalMilliseconds;
            var variance = actualDelayMs - expectedDelayMs;

            // Store in local measurements for backward compatibility
            var key = $"{weapon.GetType().Name}_Dex{attacker.Dex}";
            if (_measurements.ContainsKey(key))
            {
                var measurement = new TimingMeasurement
                {
                    WeaponType = weapon.GetType().Name,
                    Dexterity = attacker.Dex,
                    ExpectedDelayMs = expectedDelayMs,
                    ActualDelayMs = actualDelayMs,
                    VarianceMs = variance,
                    Timestamp = global::Server.Core.TickCount
                };

                _measurements[key].Add(measurement);

                // Incremental persistence - save every 10 measurements or when count reaches milestone
                if (_measurements[key].Count % 10 == 0 || _measurements[key].Count >= 50)
                {
                    SaveIncrementalResults();
                }
            }
        }
    }

    /// <summary>
    /// Saves measurement results incrementally to prevent data loss on interruption.
    /// </summary>
    private void SaveIncrementalResults()
    {
        try
        {
            var recoveryPath = Path.Combine(
                global::Server.Core.BaseDirectory,
                "Distribution",
                "AuditReports",
                "Recovery",
                $"incremental-results-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.json"
            );

            Directory.CreateDirectory(Path.GetDirectoryName(recoveryPath));

            // Convert measurements to serializable format
            var serializableResults = new
            {
                Timestamp = DateTime.UtcNow,
                TotalSwings = _totalSwings,
                Measurements = _measurements.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Select(m => new
                    {
                        m.WeaponType,
                        m.Dexterity,
                        m.ExpectedDelayMs,
                        m.ActualDelayMs,
                        m.VarianceMs,
                        m.Timestamp
                    }).ToList()
                )
            };

            var json = System.Text.Json.JsonSerializer.Serialize(serializableResults, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(recoveryPath, json);
            logger.Debug("Incremental results saved to: {Path}", recoveryPath);
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to save incremental results");
        }
    }

    protected override void AnalyzeResults()
    {
        try
        {
            logger.Information("Analyzing weapon timing results...");

            PopulateEnvironmentInfo();

            // Use local measurements collected during test execution
            // (Audit system buffer may be flushed during execution, so we use our own collection)
            var allMeasurements = _measurements.Values.SelectMany(m => m).ToList();

            if (allMeasurements.Count == 0)
            {
                logger.Warning("No timing measurements collected during test execution");
                Results.Passed = false;
                Results.FailureReasons.Add("No timing measurements were collected");
                return;
            }

            logger.Information("Found {Count} timing measurements for analysis", allMeasurements.Count);

            // Calculate overall summary metrics
            Results.Summary = new SummaryMetrics
            {
                TotalActions = allMeasurements.Count,
                AverageVarianceMs = allMeasurements.Average(m => Math.Abs(m.VarianceMs)),
                MaxVarianceMs = allMeasurements.Max(m => Math.Abs(m.VarianceMs)),
                MinVarianceMs = allMeasurements.Min(m => Math.Abs(m.VarianceMs)),
                WithinTargetCount = allMeasurements.Count(m => Math.Abs(m.VarianceMs) <= 25),
                OutlierCount = allMeasurements.Count(m => Math.Abs(m.VarianceMs) > 50)
            };

            Results.Summary.AccuracyPercent =
                (Results.Summary.WithinTargetCount / (double)Results.Summary.TotalActions) * 100.0;
            Results.Summary.OutlierPercent =
                (Results.Summary.OutlierCount / (double)Results.Summary.TotalActions) * 100.0;

            // Per-weapon breakdown from audit data
            Results.WeaponResults = allMeasurements
                .GroupBy(m => m.WeaponType)
                .Select(g => AnalyzeWeaponGroup(g.Key, g.ToList()))
                .ToList();

            // Compare against baseline
            var baseline = GetBaseline();
            if (baseline != null)
            {
                Results.BaselineComparison = Results.CompareToBaseline(baseline);
                Results.Passed = Results.DeterminePassStatus(baseline);
            }
            else
            {
                // No baseline - informational only
                Results.Passed = Results.Summary.AccuracyPercent >= 95.0; // Conservative default
                logger.Information("No baseline configured, using default pass threshold (95% accuracy)");
            }

            // Add observations
            if (Results.Summary.OutlierCount > 0)
            {
                Results.Observations.Add($"{Results.Summary.OutlierCount} outliers detected with variance >50ms");
            }

            if (Results.Summary.AccuracyPercent >= 98.0)
            {
                Results.Observations.Add("Excellent timing accuracy achieved");
            }
            else if (Results.Summary.AccuracyPercent >= 95.0)
            {
                Results.Observations.Add("Good timing accuracy, within acceptable range");
            }
            else
            {
                Results.Observations.Add("Timing accuracy below target, investigation recommended");
            }

            logger.Information("Analysis complete. Pass: {Passed}, Accuracy: {Accuracy:F1}%, Measurements: {Count}",
                Results.Passed, Results.Summary.AccuracyPercent, allMeasurements.Count);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during results analysis");
            Results.Passed = false;
            Results.FailureReasons.Add($"Analysis error: {ex.Message}");
        }
    }

    private WeaponTimingResult AnalyzeWeaponGroup(string weaponName, List<TimingMeasurement> measurements)
    {
        var result = new WeaponTimingResult
        {
            WeaponName = weaponName,
            SwingCount = measurements.Count,
            ExpectedDelayMs = measurements.Average(m => m.ExpectedDelayMs),
            ActualAvgDelayMs = measurements.Average(m => m.ActualDelayMs),
            VarianceMs = measurements.Average(m => Math.Abs(m.VarianceMs)),
            Passed = measurements.Count(m => Math.Abs(m.VarianceMs) <= 25) / (double)measurements.Count >= 0.95
        };

        // Per-dex breakdown
        result.DexVariations = measurements
            .GroupBy(m => m.Dexterity)
            .Select(g => new DexVariationResult
            {
                Dexterity = g.Key,
                SwingCount = g.Count(),
                ExpectedDelayMs = g.Average(m => m.ExpectedDelayMs),
                ActualAvgDelayMs = g.Average(m => m.ActualDelayMs),
                VarianceMs = g.Average(m => Math.Abs(m.VarianceMs)),
                Passed = g.Count(m => Math.Abs(m.VarianceMs) <= 25) / (double)g.Count() >= 0.95
            })
            .OrderBy(dv => dv.Dexterity)
            .ToList();

        return result;
    }

    private class TimingMeasurement
    {
        public string WeaponType { get; set; }
        public int Dexterity { get; set; }
        public double ExpectedDelayMs { get; set; }
        public double ActualDelayMs { get; set; }
        public double VarianceMs { get; set; }
        public long Timestamp { get; set; }
    }
}
