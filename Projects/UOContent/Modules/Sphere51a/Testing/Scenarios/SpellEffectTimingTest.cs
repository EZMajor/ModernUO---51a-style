/*************************************************************************
 * ModernUO - Sphere 51a Spell Effect Timing Test
 * File: SpellEffectTimingTest.cs
 *
 * Description: Tests instant vs delayed spell effects and timing validation.
 *              Validates Sphere51a spell effect application timing.
 *
 * STATUS: Tests the difference between instant effects (beneficial spells)
 *         and delayed effects (offensive spells with travel time).
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
/// Tests instant vs delayed spell effects and timing validation.
/// Validates Sphere51a spell effect application timing.
/// </summary>
public class SpellEffectTimingTest : TestScenario
{
    public override string ScenarioId => "spell_effect_timing";
    public override string ScenarioName => "Spell Effect Timing Test";

    private Mobile _caster;
    private Mobile _target;
    private List<SpellEffectResult> _effectResults = new();
    private int _totalEffectTests = 0;
    private bool _integrationVerified = false;

    protected override bool Setup()
    {
        try
        {
            logger.Information("Setting up spell effect timing test...");

            // CRITICAL: Verify spell integration exists
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
                return false;
            }

            // Create test mobiles
            _caster = TestMobileFactory.CreateSpellcaster("EffectCaster", intel: 100);
            _target = TestMobileFactory.CreateDummy("EffectTarget");

            // Ensure caster has mana and reagents
            _caster.Mana = _caster.ManaMax;
            GiveReagents(_caster);

            TestMobiles.Add(_caster);
            TestMobiles.Add(_target);

            // Initialize results storage
            _effectResults.Clear();

            // Subscribe to spell events
            SphereEvents.OnSpellCastBegin += OnSpellCastBegin;
            SphereEvents.OnSpellCastComplete += OnSpellCastComplete;

            logger.Information("Spell effect timing test setup complete");
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to setup spell effect timing test");
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

            logger.Information("Spell effect timing test cleanup complete");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during spell effect timing test cleanup");
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

            logger.Information("Starting spell effect timing test...");

            // Test 1: Instant effect spells (beneficial)
            TestInstantEffectSpells();

            // Test 2: Delayed effect spells (offensive)
            TestDelayedEffectSpells();

            // Test 3: Mixed instant/delayed spell sequences
            TestMixedEffectSequences();

            // Test 4: Effect timing consistency
            TestEffectTimingConsistency();

            logger.Information("Spell effect timing testing complete. Total tests: {Count}", _totalEffectTests);
            StopTest();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during spell effect timing test execution");
            Results.Passed = false;
            Results.FailureReasons.Add($"Test execution error: {ex.Message}");
            StopTest();
        }
    }

    /// <summary>
    /// Tests instant effect spells (beneficial spells that apply immediately).
    /// </summary>
    private void TestInstantEffectSpells()
    {
        logger.Information("Testing instant effect spells...");

        var instantSpells = new[] { "Heal", "Clumsy", "Bless" };

        foreach (var spellName in instantSpells)
        {
            TestInstantSpellEffect(spellName);
            _totalEffectTests++;
        }
    }

    /// <summary>
    /// Tests a single instant effect spell.
    /// </summary>
    private void TestInstantSpellEffect(string spellName)
    {
        try
        {
            // Ensure caster has mana
            if (_caster.Mana < 50)
                _caster.Mana = _caster.ManaMax;

            // Record target state before spell
            var targetHitsBefore = _target.Hits;
            var targetManaBefore = _target.Mana;
            var targetStrBefore = _target.RawStr;
            var targetDexBefore = _target.RawDex;
            var targetIntBefore = _target.RawInt;

            var castStart = global::Server.Core.TickCount;

            // Cast the instant spell
            Spell spell = CreateSpell(spellName);
            if (spell == null) return;

            spell.Cast();

            // Wait for effect to apply (should be immediate for instant spells)
            System.Threading.Thread.Sleep(100);

            var castEnd = global::Server.Core.TickCount;
            var effectDelay = castEnd - castStart;

            // Record target state after spell
            var targetHitsAfter = _target.Hits;
            var targetManaAfter = _target.Mana;
            var targetStrAfter = _target.RawStr;
            var targetDexAfter = _target.RawDex;
            var targetIntAfter = _target.RawInt;

            // Determine if effect was applied
            var effectApplied = false;
            var effectType = "None";

            switch (spellName.ToLower())
            {
                case "heal":
                    effectApplied = targetHitsAfter > targetHitsBefore;
                    effectType = "Healing";
                    break;
                case "clumsy":
                    effectApplied = targetDexAfter < targetDexBefore;
                    effectType = "Debuff";
                    break;
                case "bless":
                    effectApplied = targetStrAfter > targetStrBefore ||
                                   targetDexAfter > targetDexBefore ||
                                   targetIntAfter > targetIntBefore;
                    effectType = "Buff";
                    break;
            }

            var result = new SpellEffectResult
            {
                SpellName = spellName,
                EffectType = "Instant",
                ExpectedTiming = "Immediate",
                ActualDelayMs = effectDelay,
                EffectApplied = effectApplied,
                EffectCategory = effectType,
                TargetStateBefore = $"{targetHitsBefore}/{targetManaBefore}",
                TargetStateAfter = $"{targetHitsAfter}/{targetManaAfter}",
                Timestamp = castEnd
            };

            _effectResults.Add(result);

            LogVerbose("Instant effect {Spell}: Applied={Applied}, Delay={Delay}ms, Type={Type}",
                spellName, effectApplied, effectDelay, effectType);

        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error testing instant effect for {Spell}", spellName);
        }
    }

    /// <summary>
    /// Tests delayed effect spells (offensive spells with travel time).
    /// </summary>
    private void TestDelayedEffectSpells()
    {
        logger.Information("Testing delayed effect spells...");

        var delayedSpells = new[] { "MagicArrow", "Fireball", "Lightning" };

        foreach (var spellName in delayedSpells)
        {
            TestDelayedSpellEffect(spellName);
            _totalEffectTests++;
        }
    }

    /// <summary>
    /// Tests a single delayed effect spell.
    /// </summary>
    private void TestDelayedSpellEffect(string spellName)
    {
        try
        {
            // Ensure caster has mana
            if (_caster.Mana < 50)
                _caster.Mana = _caster.ManaMax;

            // Record target state before spell
            var targetHitsBefore = _target.Hits;
            var castStart = global::Server.Core.TickCount;

            // Cast the delayed spell
            Spell spell = CreateSpell(spellName);
            if (spell == null) return;

            spell.Cast();

            // Wait for spell to travel and apply effect
            System.Threading.Thread.Sleep(500); // Allow time for projectile travel

            var effectStart = global::Server.Core.TickCount;

            // Wait a bit more for effect to fully apply
            System.Threading.Thread.Sleep(200);

            var effectEnd = global::Server.Core.TickCount;
            var totalDelay = effectEnd - castStart;
            var effectDelay = effectEnd - effectStart;

            // Record target state after spell
            var targetHitsAfter = _target.Hits;

            // Determine if effect was applied (damage for offensive spells)
            var effectApplied = targetHitsAfter < targetHitsBefore;
            var damageDealt = Math.Max(0, targetHitsBefore - targetHitsAfter);

            var result = new SpellEffectResult
            {
                SpellName = spellName,
                EffectType = "Delayed",
                ExpectedTiming = "Projectile",
                ActualDelayMs = totalDelay,
                EffectApplied = effectApplied,
                EffectCategory = "Damage",
                TargetStateBefore = $"{targetHitsBefore}",
                TargetStateAfter = $"{targetHitsAfter}",
                DamageDealt = damageDealt,
                Timestamp = effectEnd
            };

            _effectResults.Add(result);

            LogVerbose("Delayed effect {Spell}: Applied={Applied}, TotalDelay={Total}ms, Damage={Damage}",
                spellName, effectApplied, totalDelay, damageDealt);

        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error testing delayed effect for {Spell}", spellName);
        }
    }

    /// <summary>
    /// Tests mixed sequences of instant and delayed spells.
    /// </summary>
    private void TestMixedEffectSequences()
    {
        logger.Information("Testing mixed instant/delayed spell sequences...");

        // Test sequence: Instant -> Delayed -> Instant
        var sequence = new[] { "Heal", "MagicArrow", "Clumsy" };

        foreach (var spellName in sequence)
        {
            if (spellName == "Heal" || spellName == "Clumsy")
                TestInstantSpellEffect(spellName);
            else
                TestDelayedSpellEffect(spellName);

            _totalEffectTests++;

            // Small delay between spells
            System.Threading.Thread.Sleep(200);
        }
    }

    /// <summary>
    /// Tests effect timing consistency across multiple casts.
    /// </summary>
    private void TestEffectTimingConsistency()
    {
        logger.Information("Testing effect timing consistency...");

        var testSpell = "MagicArrow";

        // Cast the same spell multiple times to check consistency
        for (int i = 0; i < 5; i++)
        {
            TestDelayedSpellEffect(testSpell);
            _totalEffectTests++;

            // Brief delay between casts
            System.Threading.Thread.Sleep(100);
        }
    }

    /// <summary>
    /// Creates a spell instance for testing.
    /// </summary>
    private Spell CreateSpell(string spellName)
    {
        return spellName.ToLower() switch
        {
            "magicarrow" => new MagicArrowSpell(_caster, null),
            "fireball" => new FireballSpell(_caster, null),
            "lightning" => new LightningSpell(_caster, null),
            "heal" => new HealSpell(_caster, null),
            "clumsy" => new ClumsySpell(_caster, null),
            "bless" => new BlessSpell(_caster, null),
            _ => null
        };
    }

    /// <summary>
    /// Event handlers for tracking spell effects.
    /// </summary>
    private void OnSpellCastBegin(object sender, SpellCastEventArgs e)
    {
        if (e.Caster != _caster)
            return;

        LogVerbose("Spell cast begin: {Caster} casting {Spell}",
            e.Caster.Name, e.Spell?.GetType().Name ?? "Unknown");
    }

    private void OnSpellCastComplete(object sender, SpellCastEventArgs e)
    {
        if (e.Caster != _caster)
            return;

        LogVerbose("Spell cast complete: {Caster} finished {Spell}",
            e.Caster.Name, e.Spell?.GetType().Name ?? "Unknown");
    }



    /// <summary>
    /// Gives reagents to the caster.
    /// </summary>
    private void GiveReagents(Mobile mobile)
    {
        var backpack = mobile.Backpack;
        if (backpack == null)
        {
            backpack = new Backpack();
            mobile.AddItem(backpack);
        }

        // Give reagents
        backpack.DropItem(new BlackPearl(100));
        backpack.DropItem(new Bloodmoss(100));
        backpack.DropItem(new Garlic(100));
        backpack.DropItem(new Ginseng(100));
        backpack.DropItem(new MandrakeRoot(100));
        backpack.DropItem(new Nightshade(100));
        backpack.DropItem(new SulfurousAsh(100));
        backpack.DropItem(new SpidersSilk(100));
    }

    protected override void AnalyzeResults()
    {
        try
        {
            logger.Information("Analyzing spell effect timing test results...");

            PopulateEnvironmentInfo();

            if (_effectResults.Count == 0)
            {
                logger.Error("CRITICAL: No effect timing test results collected");
                Results.Passed = false;
                Results.FailureReasons.Add("CRITICAL: No spell effect timing tests completed");
                return;
            }

            // Analyze instant vs delayed effects
            var instantResults = _effectResults.Where(r => r.EffectType == "Instant").ToList();
            var delayedResults = _effectResults.Where(r => r.EffectType == "Delayed").ToList();

            // Calculate success rates
            var instantEffectRate = instantResults.Count > 0
                ? (instantResults.Count(r => r.EffectApplied) / (double)instantResults.Count) * 100.0
                : 0.0;

            var delayedEffectRate = delayedResults.Count > 0
                ? (delayedResults.Count(r => r.EffectApplied) / (double)delayedResults.Count) * 100.0
                : 0.0;

            // Analyze timing consistency for delayed spells
            var delayedDelays = delayedResults.Select(r => r.ActualDelayMs).ToList();
            var avgDelayedDelay = delayedDelays.Count > 0 ? delayedDelays.Average() : 0;
            var delayedDelayVariance = delayedDelays.Count > 0
                ? delayedDelays.Select(d => Math.Abs(d - avgDelayedDelay)).Average()
                : 0;

            // Analyze instant timing (should be very fast)
            var instantDelays = instantResults.Select(r => r.ActualDelayMs).ToList();
            var avgInstantDelay = instantDelays.Count > 0 ? instantDelays.Average() : 0;

            // Set summary metrics
            Results.Summary = new SummaryMetrics
            {
                TotalActions = _effectResults.Count,
                WithinTargetCount = _effectResults.Count(r => r.EffectApplied),
                OutlierCount = _effectResults.Count(r => !r.EffectApplied),
                DoubleCastCount = 0,
                FizzleCount = 0
            };

            Results.Summary.AccuracyPercent = (Results.Summary.WithinTargetCount / (double)Results.Summary.TotalActions) * 100.0;

            // Sphere51a specific pass criteria
            Results.Passed = instantEffectRate >= 90.0 && // Instant effects should apply reliably
                             delayedEffectRate >= 80.0 && // Delayed effects should apply most of the time
                             avgInstantDelay <= 200 && // Instant effects should be very fast
                             delayedDelayVariance <= 100; // Delayed effects should be reasonably consistent

            // Add observations
            if (instantEffectRate < 90.0)
            {
                Results.Observations.Add($"Instant effect success rate {instantEffectRate:F1}% below 90% threshold");
            }

            if (delayedEffectRate < 80.0)
            {
                Results.Observations.Add($"Delayed effect success rate {delayedEffectRate:F1}% below 80% threshold");
            }

            if (avgInstantDelay > 200)
            {
                Results.Observations.Add($"Instant effects taking too long: {avgInstantDelay:F0}ms average");
            }

            if (delayedDelayVariance > 100)
            {
                Results.Observations.Add($"Delayed effect timing inconsistent: {delayedDelayVariance:F0}ms average variance");
            }

            if (Results.Passed)
            {
                Results.Observations.Add("Spell effect timing working correctly per Sphere51a specifications");
                Results.Observations.Add($"Instant effects: {instantEffectRate:F1}% success rate, {avgInstantDelay:F0}ms avg delay");
                Results.Observations.Add($"Delayed effects: {delayedEffectRate:F1}% success rate, {avgDelayedDelay:F0}ms avg delay");
            }

            logger.Information("Spell effect timing analysis complete. Pass: {Passed}, Instant: {Instant:F1}%, Delayed: {Delayed:F1}%",
                Results.Passed, instantEffectRate, delayedEffectRate);

        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during spell effect timing results analysis");
            Results.Passed = false;
            Results.FailureReasons.Add($"Analysis error: {ex.Message}");
        }
    }

    private class SpellEffectResult
    {
        public string SpellName { get; set; }
        public string EffectType { get; set; }
        public string ExpectedTiming { get; set; }
        public long ActualDelayMs { get; set; }
        public bool EffectApplied { get; set; }
        public string EffectCategory { get; set; }
        public string TargetStateBefore { get; set; }
        public string TargetStateAfter { get; set; }
        public int DamageDealt { get; set; }
        public long Timestamp { get; set; }
    }
}
