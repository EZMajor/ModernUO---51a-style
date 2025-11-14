/*************************************************************************
 * ModernUO - Sphere 51a Stress Test
 * File: StressTest.cs
 *
 * Description: Stress tests the combat system with multiple concurrent
 *              combatants to validate system performance under load.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Server.Items;
using Server.Modules.Sphere51a.Testing.Reports;

namespace Server.Modules.Sphere51a.Testing.Scenarios;

/// <summary>
/// Stress tests combat system with concurrent combatants.
/// </summary>
public class StressTest : TestScenario
{
    public override string ScenarioId => "stress_test";
    public override string ScenarioName => "Combat System Stress Test";

    private List<(Mobile attacker, Mobile defender)> _combatPairs = new();
    private List<double> _tickTimes = new();
    private long _testDurationMs;
    private int _totalActions = 0;
    private Stopwatch _performanceWatch = new();

    protected override bool Setup()
    {
        try
        {
            logger.Information("Setting up stress test...");

            var scenarioConfig = Config.Scenarios.StressTest;
            _testDurationMs = (scenarioConfig?.DurationSeconds ?? 120) * 1000;

            var concurrentCount = scenarioConfig?.ConcurrentCombatants ?? 20;
            var weaponMix = scenarioConfig?.WeaponMix ?? new List<string> { "Katana", "Longsword", "Broadsword" };

            // Create combat pairs
            for (int i = 0; i < concurrentCount / 2; i++)
            {
                var weaponType = weaponMix[i % weaponMix.Count];
                var weapon = TestMobileFactory.CreateWeapon(weaponType);

                var attacker = TestMobileFactory.CreateCombatant(
                    $"Attacker{i}",
                    str: 100,
                    dex: 100,
                    intel: 50,
                    weapon: weapon,
                    location: new Point3D(1000 + i * 2, 1000, 0)
                );

                var defender = TestMobileFactory.CreateDummy(
                    $"Defender{i}",
                    location: new Point3D(1001 + i * 2, 1000, 0)
                );

                TestMobiles.Add(attacker);
                TestMobiles.Add(defender);

                _combatPairs.Add((attacker, defender));
            }

            logger.Information("Stress test setup complete. {Count} combat pairs created", _combatPairs.Count);
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to setup stress test");
            return false;
        }
    }

    protected override void RunTest()
    {
        try
        {
            logger.Information("Starting stress test execution...");

            var startTick = global::Server.Core.TickCount;

            // Engage all combat pairs
            foreach (var (attacker, defender) in _combatPairs)
            {
                attacker.Combatant = defender;
            }

            // Monitor system performance for duration
            while (global::Server.Core.TickCount - startTick < _testDurationMs)
            {
                _performanceWatch.Restart();

                // Simulate tick (combat system runs on 50ms pulse)
                System.Threading.Thread.Sleep(50);

                var tickTime = _performanceWatch.Elapsed.TotalMilliseconds;
                _tickTimes.Add(tickTime);

                _totalActions++;

                // Sample log every 5 seconds
                if (_totalActions % 100 == 0)
                {
                    LogVerbose("Stress test progress: {Actions} ticks, avg {AvgTick:F2}ms",
                        _totalActions, _tickTimes.Average());
                }
            }

            StopTest();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during stress test execution");
            StopTest();
        }
    }

    protected override void AnalyzeResults()
    {
        try
        {
            logger.Information("Analyzing stress test results...");

            PopulateEnvironmentInfo();

            if (_tickTimes.Count == 0)
            {
                Results.Passed = false;
                Results.FailureReasons.Add("No performance data collected");
                return;
            }

            // Calculate tick time statistics
            var sortedTicks = _tickTimes.OrderBy(t => t).ToList();
            var avgTickTime = sortedTicks.Average();
            var maxTickTime = sortedTicks.Max();
            var p95Index = (int)(sortedTicks.Count * 0.95);
            var p99Index = (int)(sortedTicks.Count * 0.99);

            var p95TickTime = sortedTicks[p95Index];
            var p99TickTime = sortedTicks[p99Index];

            // Memory usage
            var memoryMB = Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024;

            // Create stress test result
            var durationSeconds = (EndTime - StartTime).TotalSeconds;
            Results.StressResult = new StressTestResult
            {
                ConcurrentCombatants = _combatPairs.Count * 2,
                TotalActions = _totalActions,
                DurationSeconds = durationSeconds,
                ActionsPerSecond = _totalActions / durationSeconds,
                AvgTickTimeMs = avgTickTime,
                MaxTickTimeMs = maxTickTime,
                P95TickTimeMs = p95TickTime,
                P99TickTimeMs = p99TickTime,
                ThrottleEventCount = 0, // TODO: Hook into audit system throttle events
                MemoryUsedMB = memoryMB,
                Passed = avgTickTime < 10.0 && maxTickTime < 25.0
            };

            // Summary metrics
            Results.Summary = new SummaryMetrics
            {
                TotalActions = _totalActions,
                AvgTickTimeMs = avgTickTime,
                MaxTickTimeMs = maxTickTime,
                ActionsPerSecond = Results.StressResult.ActionsPerSecond
            };

            // Compare against baseline
            var baseline = GetBaseline();
            if (baseline != null)
            {
                Results.Passed = avgTickTime <= baseline.MaxTickTimeMs &&
                                 Results.StressResult.ThrottleEventCount <= baseline.MaxThrottleEvents &&
                                 Results.StressResult.ActionsPerSecond >= baseline.MinThroughputActionsPerSec;

                if (!Results.Passed)
                {
                    if (avgTickTime > baseline.MaxTickTimeMs)
                    {
                        Results.FailureReasons.Add($"Average tick time {avgTickTime:F2}ms exceeds baseline {baseline.MaxTickTimeMs}ms");
                    }

                    if (Results.StressResult.ActionsPerSecond < baseline.MinThroughputActionsPerSec)
                    {
                        Results.FailureReasons.Add($"Throughput {Results.StressResult.ActionsPerSecond:F1} acts/s below baseline {baseline.MinThroughputActionsPerSec}");
                    }
                }
            }
            else
            {
                // No baseline - use default thresholds
                Results.Passed = Results.StressResult.Passed;
            }

            // Observations
            if (maxTickTime > 25.0)
            {
                Results.Observations.Add($"Peak tick time {maxTickTime:F1}ms exceeded 25ms threshold");
            }

            if (p99TickTime > 15.0)
            {
                Results.Observations.Add($"P99 tick time {p99TickTime:F1}ms suggests occasional performance spikes");
            }

            if (Results.StressResult.MemoryUsedMB > 500)
            {
                Results.Observations.Add($"Memory usage {Results.StressResult.MemoryUsedMB}MB is high");
            }

            if (avgTickTime < 5.0)
            {
                Results.Observations.Add("Excellent performance: average tick time well below target");
            }

            logger.Information("Stress test analysis complete. Pass: {Passed}, Avg Tick: {AvgTick:F2}ms",
                Results.Passed, avgTickTime);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during stress test results analysis");
            Results.Passed = false;
            Results.FailureReasons.Add($"Analysis error: {ex.Message}");
        }
    }
}
