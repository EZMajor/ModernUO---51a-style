/*************************************************************************
 * ModernUO - Sphere 51a Spell Timing Test
 * File: SpellTimingTest.cs
 *
 * Description: Tests spell cast timing accuracy and double-cast detection.
 *              Validates spell timing system against configured baselines.
 *
 * STATUS: REQUIRES SPELL INTEGRATION - This test actively casts spells and
 *         measures results. It will FAIL if Spell.cs does not have Sphere51a
 *         integration hooks to raise spell events.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using Server.Modules.Sphere51a.Combat;
using Server.Modules.Sphere51a.Combat.Audit;
using Server.Modules.Sphere51a.Events;
using Server.Modules.Sphere51a.Testing.Reports;
using Server.Spells;
using Server.Spells.First;
using Server.Spells.Second;
using Server.Spells.Third;
using Server.Spells.Fourth;
using Server.Spells.Fifth;
using Server.Items;

namespace Server.Modules.Sphere51a.Testing.Scenarios;

/// <summary>
/// Tests spell cast timing accuracy for Sphere51a magic system.
/// REQUIRES: Spell integration hooks in Spell.cs to raise Sphere events.
/// </summary>
public class SpellTimingTest : TestScenario
{
    public override string ScenarioId => "spell_timing";
    public override string ScenarioName => "Spell Timing Test";

    private Mobile _caster;
    private Mobile _target;
    private Dictionary<string, List<SpellCastMeasurement>> _measurements = new();
    private List<SpellCastMeasurement> _eventMeasurements = new();
    private int _totalCasts = 0;
    private int _doubleCasts = 0;
    private long _testDurationMs;
    private long _testStartTick;
    private bool _integrationVerified = false;
    private Dictionary<Spell, long> _castStartTimes = new();
    private Dictionary<Spell, int> _manaBeforeCast = new();
    private Dictionary<Spell, Dictionary<Type, int>> _reagentsBeforeCast = new();

    protected override bool Setup()
    {
        try
        {
            logger.Information("Setting up spell timing test...");

            // CRITICAL: Verify spell integration exists before running test
            logger.Information("Verifying spell system integration...");
            try
            {
                IntegrationVerifier.RequireSpellIntegration();
                _integrationVerified = true;
                logger.Information("Spell integration verification: PASSED");
            }
            catch (IntegrationVerifier.IntegrationMissingException ex)
            {
                logger.Error("Spell integration verification: FAILED");
                logger.Error(ex.Message);
                Results.Passed = false;
                Results.FailureReasons.Add("CRITICAL: Spell integration not implemented");
                Results.Observations.Add(ex.Message);
                Results.Observations.Add("This test cannot run until Spell.cs has Sphere51a integration hooks");
                return false; // Block test execution
            }

            // Get test duration from config
            var scenarioConfig = Config.Scenarios.SpellTiming;
            _testDurationMs = (scenarioConfig?.DurationSeconds ?? 60) * 1000;

            // Create test mobiles
            _caster = TestMobileFactory.CreateSpellcaster("TestCaster", intel: 100);
            _target = TestMobileFactory.CreateDummy("SpellTarget");

            // Ensure caster has mana and reagents
            _caster.Mana = _caster.ManaMax;
            GiveReagents(_caster);

            TestMobiles.Add(_caster);
            TestMobiles.Add(_target);

            // Initialize measurement storage
            _measurements.Clear();
            _eventMeasurements.Clear();

            // Subscribe to spell events for active measurement
            SphereEvents.OnSpellCastBegin += OnSpellCastBegin;
            SphereEvents.OnSpellCastComplete += OnSpellCastComplete;

            logger.Information("Spell timing test setup complete. Duration: {Duration}s", _testDurationMs / 1000);
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to setup spell timing test");
            Results.Passed = false;
            Results.FailureReasons.Add($"Setup failed: {ex.Message}");
            return false;
        }
    }

    protected override void Teardown()
    {
        try
        {
            // Unsubscribe from events
            SphereEvents.OnSpellCastBegin -= OnSpellCastBegin;
            SphereEvents.OnSpellCastComplete -= OnSpellCastComplete;

            logger.Information("Spell timing test cleanup complete");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during spell timing test cleanup");
        }
    }

    protected override void RunTest()
    {
        try
        {
            if (!_integrationVerified)
            {
                logger.Error("Cannot run test - integration verification failed");
                StopTest();
                return;
            }

            logger.Information("Starting spell timing test execution...");

            _testStartTick = global::Server.Core.TickCount;
            var scenarioConfig = Config.Scenarios.SpellTiming;

            if (scenarioConfig?.Spells == null || scenarioConfig.Spells.Count == 0)
            {
                logger.Warning("No spells configured for testing");
                Results.Passed = false;
                Results.FailureReasons.Add("No spells configured in test-config.json");
                StopTest();
                return;
            }

            // ACTIVE TESTING: Actually cast spells and measure results
            logger.Information("Actively casting {Count} spell types...", scenarioConfig.Spells.Count);

            foreach (var spellConfig in scenarioConfig.Spells)
            {
                var minCasts = spellConfig.MinCasts > 0 ? spellConfig.MinCasts : 10;
                logger.Information("Testing {Spell}: Casting {Count} times", spellConfig.Name, minCasts);

                for (int i = 0; i < minCasts; i++)
                {
                    CastSpell(spellConfig.Name, spellConfig.Circle);

                    // Small delay between casts to ensure timing accuracy
                    System.Threading.Thread.Sleep(100);
                }
            }

            // Also analyze audit logs if available (for comparison)
            AnalyzeAuditLogs();

            logger.Information("Spell casting complete. Total casts: {Count}", _eventMeasurements.Count);
            StopTest();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during spell timing test execution");
            Results.Passed = false;
            Results.FailureReasons.Add($"Test execution error: {ex.Message}");
            StopTest();
        }
    }

    /// <summary>
    /// Actively casts a spell and measures the timing through events.
    /// </summary>
    private void CastSpell(string spellName, int circle)
    {
        try
        {
            // Ensure caster has mana
            if (_caster.Mana < 50)
            {
                _caster.Mana = _caster.ManaMax;
            }

            Spell spell = null;

            // Create spell based on name
            switch (spellName.ToLower())
            {
                case "magicarrow":
                    spell = new MagicArrowSpell(_caster, null);
                    break;
                case "fireball":
                    spell = new FireballSpell(_caster, null);
                    break;
                case "clumsy":
                    spell = new ClumsySpell(_caster, null);
                    break;
                case "heal":
                    spell = new HealSpell(_caster, null);
                    break;
                case "lightning":
                    spell = new LightningSpell(_caster, null);
                    break;
                case "poison":
                    spell = new PoisonSpell(_caster, null);
                    break;
                case "teleport":
                    spell = new TeleportSpell(_caster, null);
                    break;
                case "bless":
                    spell = new BlessSpell(_caster, null);
                    break;
                default:
                    logger.Warning("Unknown spell: {Spell}", spellName);
                    return;
            }

            if (spell != null)
            {
                var beforeCast = global::Server.Core.TickCount;

                // Cast the spell
                spell.Cast();

                // Wait for spell to complete
                System.Threading.Thread.Sleep(500);

                var afterCast = global::Server.Core.TickCount;

                LogVerbose("Cast {Spell} - Duration: {Duration}ms", spellName, afterCast - beforeCast);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error casting spell {Spell}", spellName);
        }
    }

    /// <summary>
    /// Event handler for spell cast begin - captures timing start.
    /// </summary>
    private void OnSpellCastBegin(object sender, SpellCastEventArgs e)
    {
        if (e.Caster != _caster || e.Spell == null)
            return;

        // Record cast start time and mana before cast
        _castStartTimes[e.Spell] = global::Server.Core.TickCount;
        _manaBeforeCast[e.Spell] = e.Caster.Mana;

        // Record reagent counts before cast
        var reagentCounts = new Dictionary<Type, int>();
        var backpack = e.Caster.Backpack;
        if (backpack != null && e.Spell.Reagents != null)
        {
            foreach (var reagentType in e.Spell.Reagents)
            {
                var totalCount = backpack.GetAmount(reagentType);
                reagentCounts[reagentType] = totalCount;
            }
        }
        _reagentsBeforeCast[e.Spell] = reagentCounts;

        LogVerbose("Event: {Caster} began casting {Spell} at {Time}ms (Mana: {Mana})",
            e.Caster.Name, e.Spell.GetType().Name, global::Server.Core.TickCount, e.Caster.Mana);
    }

    /// <summary>
    /// Event handler for spell cast complete - captures timing and results.
    /// </summary>
    private void OnSpellCastComplete(object sender, SpellCastEventArgs e)
    {
        if (e.Caster != _caster || e.Spell == null)
            return;

        var spellName = e.Spell.GetType().Name.Replace("Spell", "");
        var currentTime = global::Server.Core.TickCount;

        // Calculate timing metrics using reflection to avoid namespace issues
        var spellTimingProviderType = Type.GetType("Server.Modules.Sphere51a.Spells.SpellTimingProvider, Server");
        var getCastDelayMethod = spellTimingProviderType?.GetMethod("GetCastDelay", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        var expectedDelayMs = (int)(getCastDelayMethod?.Invoke(null, new object[] {
            e.Spell, e.Caster.Skills[SkillName.Magery].Value, e.Spell.Scroll == null }) ?? 0);

        var actualDelayMs = _castStartTimes.TryGetValue(e.Spell, out var startTime)
            ? currentTime - startTime
            : 0;

        var varianceMs = actualDelayMs - expectedDelayMs;

        // Calculate mana usage
        var manaBefore = _manaBeforeCast.TryGetValue(e.Spell, out var beforeMana) ? beforeMana : e.Caster.Mana;
        var manaUsed = manaBefore - e.Caster.Mana;

        // Detect fizzle (spell completed but no mana was consumed)
        var fizzled = manaUsed == 0 && actualDelayMs < expectedDelayMs * 0.5;

        // Detect double-cast (unusually fast completion)
        var doubleCast = actualDelayMs < expectedDelayMs * 0.3 && !fizzled;

        var measurement = new SpellCastMeasurement
        {
            SpellName = spellName,
            ExpectedDelayMs = expectedDelayMs,
            ActualDelayMs = actualDelayMs,
            VarianceMs = varianceMs,
            ManaUsed = manaUsed,
            Fizzled = fizzled,
            DoubleCast = doubleCast,
            Timestamp = currentTime
        };

        _eventMeasurements.Add(measurement);
        _totalCasts++;

        // Calculate reagent consumption
        var reagentsConsumed = new Dictionary<Type, int>();
        if (_reagentsBeforeCast.TryGetValue(e.Spell, out var reagentsBefore))
        {
            var backpack = e.Caster.Backpack;
            if (backpack != null && e.Spell.Reagents != null)
            {
                foreach (var reagentType in e.Spell.Reagents)
                {
                    var beforeCount = reagentsBefore.TryGetValue(reagentType, out var count) ? count : 0;
                    var afterCount = backpack.GetAmount(reagentType);
                    var consumed = beforeCount - afterCount;
                    if (consumed > 0)
                    {
                        reagentsConsumed[reagentType] = consumed;
                    }
                }
            }
        }

        // Update measurement with reagent data
        measurement.ReagentsConsumed = reagentsConsumed;

        // Clean up tracking dictionaries
        _castStartTimes.Remove(e.Spell);
        _manaBeforeCast.Remove(e.Spell);
        _reagentsBeforeCast.Remove(e.Spell);

        var reagentSummary = reagentsConsumed.Count > 0
            ? string.Join(", ", reagentsConsumed.Select(kvp => $"{kvp.Value}x{kvp.Key.Name}"))
            : "None";

        LogVerbose("Event: {Caster} completed {Spell} - Expected: {Expected}ms, Actual: {Actual}ms, Variance: {Variance}ms, Mana: {Mana}, Reagents: {Reagents}, Fizzled: {Fizzled}",
            e.Caster.Name, spellName, expectedDelayMs, actualDelayMs, varianceMs, manaUsed, reagentSummary, fizzled);
    }

    /// <summary>
    /// Gives the caster all reagents needed for testing.
    /// </summary>
    private void GiveReagents(Mobile mobile)
    {
        var backpack = mobile.Backpack;
        if (backpack == null)
        {
            backpack = new Backpack();
            mobile.AddItem(backpack);
        }

        // Give 100 of each reagent
        backpack.DropItem(new BlackPearl(100));
        backpack.DropItem(new Bloodmoss(100));
        backpack.DropItem(new Garlic(100));
        backpack.DropItem(new Ginseng(100));
        backpack.DropItem(new MandrakeRoot(100));
        backpack.DropItem(new Nightshade(100));
        backpack.DropItem(new SulfurousAsh(100));
        backpack.DropItem(new SpidersSilk(100));
    }

    private void AnalyzeAuditLogs()
    {
        // Get recent spell cast entries from audit system (supplementary data)
        if (!CombatAuditSystem.IsInitialized || !CombatAuditSystem.Config.EnableSpellAudit)
        {
            logger.Information("Spell audit system not enabled - using event data only");
            return;
        }

        var auditEntries = CombatAuditSystem.GetBufferSnapshot();
        var spellEntries = auditEntries
            .Where(e => e.ActionType == CombatActionTypes.SpellCastComplete)
            .ToList();

        if (spellEntries.Count == 0)
        {
            logger.Information("No spell cast entries in audit logs - using event data only");
            return;
        }

        foreach (var entry in spellEntries)
        {
            var spellName = entry.GetDetail("SpellName")?.ToString() ?? "Unknown";

            if (!_measurements.ContainsKey(spellName))
            {
                _measurements[spellName] = new List<SpellCastMeasurement>();
            }

            _measurements[spellName].Add(new SpellCastMeasurement
            {
                SpellName = spellName,
                ExpectedDelayMs = entry.ExpectedDelayMs,
                ActualDelayMs = entry.ActualDelayMs,
                VarianceMs = entry.VarianceMs,
                ManaUsed = entry.GetDetail("ManaUsed") as int? ?? 0,
                Fizzled = entry.GetDetail("Fizzled") as bool? ?? false,
                DoubleCast = entry.GetDetail("DoublecastDetected") as bool? ?? false,
                Timestamp = entry.Timestamp
            });

            if (entry.GetDetail("DoublecastDetected") as bool? == true)
            {
                _doubleCasts++;
            }
        }

        logger.Information("Analyzed {Count} spell casts from audit logs (supplementary)", spellEntries.Count);
    }

    protected override void AnalyzeResults()
    {
        try
        {
            logger.Information("Analyzing spell timing results...");

            PopulateEnvironmentInfo();

            // CRITICAL CHECK: If we cast spells but got no events, integration is broken
            if (_totalCasts > 0 && _eventMeasurements.Count == 0)
            {
                logger.Error("CRITICAL: Cast {Count} spells but received ZERO events!", _totalCasts);
                Results.Passed = false;
                Results.FailureReasons.Add("CRITICAL: Spell integration broken - events not raised");
                Results.Observations.Add($"Attempted to cast spells but no Sphere events were raised");
                Results.Observations.Add("This indicates Spell.cs does not have integration hooks");
                Results.Observations.Add("Integration verification should have caught this - verify IntegrationVerifier is working");
                return;
            }

            // Use event measurements as primary data source
            var allMeasurements = _eventMeasurements;

            // Merge in audit log data if available
            foreach (var kvp in _measurements)
            {
                allMeasurements.AddRange(kvp.Value);
            }

            if (allMeasurements.Count == 0)
            {
                logger.Error("CRITICAL: No spell measurements collected - zero events raised");
                Results.Passed = false;
                Results.FailureReasons.Add("CRITICAL: No spell events detected - integration missing or broken");
                Results.Observations.Add("Test ran but no spell events were raised");
                Results.Observations.Add("Verify Spell.cs has Sphere51a integration hooks");
                return;
            }

            // Calculate overall summary metrics
            Results.Summary = new SummaryMetrics
            {
                TotalActions = allMeasurements.Count,
                AverageVarianceMs = allMeasurements.Average(m => Math.Abs(m.VarianceMs)),
                MaxVarianceMs = allMeasurements.Max(m => Math.Abs(m.VarianceMs)),
                MinVarianceMs = allMeasurements.Min(m => Math.Abs(m.VarianceMs)),
                WithinTargetCount = allMeasurements.Count(m => Math.Abs(m.VarianceMs) <= 50),
                OutlierCount = allMeasurements.Count(m => Math.Abs(m.VarianceMs) > 100),
                DoubleCastCount = allMeasurements.Count(m => m.DoubleCast),
                FizzleCount = allMeasurements.Count(m => m.Fizzled)
            };

            Results.Summary.AccuracyPercent =
                (Results.Summary.WithinTargetCount / (double)Results.Summary.TotalActions) * 100.0;
            Results.Summary.OutlierPercent =
                (Results.Summary.OutlierCount / (double)Results.Summary.TotalActions) * 100.0;
            Results.Summary.FizzleRatePercent =
                (Results.Summary.FizzleCount / (double)Results.Summary.TotalActions) * 100.0;

            // Per-spell breakdown
            Results.SpellResults = _measurements
                .Select(kvp => AnalyzeSpellGroup(kvp.Key, kvp.Value))
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
                // No baseline - use conservative defaults
                Results.Passed = Results.Summary.AccuracyPercent >= 95.0 &&
                                 Results.Summary.DoubleCastCount == 0;
                logger.Information("No baseline configured, using default pass criteria");
            }

            // Add observations
            if (Results.Summary.DoubleCastCount > 0)
            {
                Results.Observations.Add($"{Results.Summary.DoubleCastCount} double-casts detected - requires investigation");
            }

            if (Results.Summary.FizzleRatePercent > 5.0)
            {
                Results.Observations.Add($"Fizzle rate {Results.Summary.FizzleRatePercent:F1}% exceeds 5% threshold");
            }

            if (Results.Summary.AccuracyPercent >= 97.0)
            {
                Results.Observations.Add("Excellent spell timing accuracy achieved");
            }

            logger.Information("Spell timing analysis complete. Pass: {Passed}, Accuracy: {Accuracy:F1}%",
                Results.Passed, Results.Summary.AccuracyPercent);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during spell timing results analysis");
            Results.Passed = false;
            Results.FailureReasons.Add($"Analysis error: {ex.Message}");
        }
    }

    private SpellTimingResult AnalyzeSpellGroup(string spellName, List<SpellCastMeasurement> measurements)
    {
        return new SpellTimingResult
        {
            SpellName = spellName,
            Circle = 0, // TODO: Extract circle from spell data
            CastCount = measurements.Count,
            ExpectedDelayMs = measurements.Average(m => m.ExpectedDelayMs),
            ActualAvgDelayMs = measurements.Average(m => m.ActualDelayMs),
            VarianceMs = measurements.Average(m => Math.Abs(m.VarianceMs)),
            FizzleCount = measurements.Count(m => m.Fizzled),
            FizzleRatePercent = (measurements.Count(m => m.Fizzled) / (double)measurements.Count) * 100.0,
            DoubleCastCount = measurements.Count(m => m.DoubleCast),
            AvgManaUsed = measurements.Average(m => m.ManaUsed),
            ExpectedManaUsed = 0, // TODO: Get expected mana from spell definition
            Passed = measurements.Count(m => Math.Abs(m.VarianceMs) <= 50) / (double)measurements.Count >= 0.95
        };
    }

    private class SpellCastMeasurement
    {
        public string SpellName { get; set; }
        public double ExpectedDelayMs { get; set; }
        public double ActualDelayMs { get; set; }
        public double VarianceMs { get; set; }
        public int ManaUsed { get; set; }
        public Dictionary<Type, int> ReagentsConsumed { get; set; } = new();
        public bool Fizzled { get; set; }
        public bool DoubleCast { get; set; }
        public long Timestamp { get; set; }
    }
}
