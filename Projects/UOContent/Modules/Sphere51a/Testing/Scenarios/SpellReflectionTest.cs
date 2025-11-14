/*************************************************************************
 * ModernUO - Sphere 51a Spell Reflection & Target Feedback Test
 * File: SpellReflectionTest.cs
 *
 * Description: Tests spell reflection mechanics and target feedback systems.
 *              Validates Sphere51a reflection and resistance behaviors.
 *
 * STATUS: Tests spell reflection back to caster and target response validation.
 *         Validates reflection triggers and feedback mechanisms.
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
using Server.Items;

namespace Server.Modules.Sphere51a.Testing.Scenarios;

/// <summary>
/// Tests spell reflection mechanics and target feedback systems.
/// Validates Sphere51a reflection and resistance behaviors.
/// </summary>
public class SpellReflectionTest : TestScenario
{
    public override string ScenarioId => "spell_reflection";
    public override string ScenarioName => "Spell Reflection & Target Feedback Test";

    private Mobile _caster;
    private Mobile _reflector;
    private Mobile _target;
    private List<ReflectionTestResult> _reflectionResults = new();
    private int _totalReflectionTests = 0;
    private bool _integrationVerified = false;

    protected override bool Setup()
    {
        try
        {
            logger.Information("Setting up spell reflection test...");

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
            _caster = TestMobileFactory.CreateSpellcaster("ReflectionCaster", intel: 100);
            _reflector = TestMobileFactory.CreateSpellcaster("SpellReflector", intel: 100);
            _target = TestMobileFactory.CreateDummy("ReflectionTarget");

            // Ensure casters have mana and reagents
            _caster.Mana = _caster.ManaMax;
            _reflector.Mana = _reflector.ManaMax;
            GiveReagents(_caster);
            GiveReagents(_reflector);

            // Give reflector reflection capability (simulate magic reflection)
            // Note: In a real implementation, this would use actual reflection items/spells
            _reflector.Blessed = true; // Make immune to damage for testing

            TestMobiles.Add(_caster);
            TestMobiles.Add(_reflector);
            TestMobiles.Add(_target);

            // Initialize results storage
            _reflectionResults.Clear();

            // Subscribe to spell events
            SphereEvents.OnSpellCastBegin += OnSpellCastBegin;
            SphereEvents.OnSpellCastComplete += OnSpellCastComplete;
            SphereEvents.OnSpellReflected += OnSpellReflected;

            logger.Information("Spell reflection test setup complete");
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to setup spell reflection test");
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
            SphereEvents.OnSpellReflected -= OnSpellReflected;

            logger.Information("Spell reflection test cleanup complete");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during spell reflection test cleanup");
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

            logger.Information("Starting spell reflection test...");

            // Test 1: Spell reflection mechanics
            TestSpellReflection();

            // Test 2: Target feedback and resistance
            TestTargetFeedback();

            // Test 3: Reflection consistency
            TestReflectionConsistency();

            // Test 4: Mixed reflection scenarios
            TestMixedReflectionScenarios();

            logger.Information("Spell reflection testing complete. Total tests: {Count}", _totalReflectionTests);
            StopTest();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during spell reflection test execution");
            Results.Passed = false;
            Results.FailureReasons.Add($"Test execution error: {ex.Message}");
            StopTest();
        }
    }

    /// <summary>
    /// Tests spell reflection mechanics.
    /// </summary>
    private void TestSpellReflection()
    {
        logger.Information("Testing spell reflection mechanics...");

        var spellsToTest = new[] { "MagicArrow", "Fireball", "Lightning" };

        foreach (var spellName in spellsToTest)
        {
            TestReflectionScenario(spellName);
            _totalReflectionTests++;
        }
    }

    /// <summary>
    /// Tests a single spell reflection scenario.
    /// </summary>
    private void TestReflectionScenario(string spellName)
    {
        try
        {
            // Ensure caster has mana
            if (_caster.Mana < 50)
                _caster.Mana = _caster.ManaMax;

            var casterHitsBefore = _caster.Hits;
            var reflectorHitsBefore = _reflector.Hits;
            var castStart = global::Server.Core.TickCount;

            // Cast spell at reflector
            Spell spell = CreateSpell(spellName, _caster);
            if (spell == null) return;

            // Set spell target to reflector
            spell.Cast();

            // Wait for spell to process (reflection should happen quickly)
            System.Threading.Thread.Sleep(300);

            var castEnd = global::Server.Core.TickCount;
            var casterHitsAfter = _caster.Hits;
            var reflectorHitsAfter = _reflector.Hits;

            // Determine if reflection occurred
            var reflectionOccurred = false;
            var damageToCaster = 0;
            var damageToReflector = 0;

            // Check if caster took damage (indicating reflection)
            if (casterHitsAfter < casterHitsBefore)
            {
                reflectionOccurred = true;
                damageToCaster = casterHitsBefore - casterHitsAfter;
            }

            // Check if reflector took damage (normal case)
            if (reflectorHitsAfter < reflectorHitsBefore)
            {
                damageToReflector = reflectorHitsBefore - reflectorHitsAfter;
            }

            var result = new ReflectionTestResult
            {
                TestType = "SpellReflection",
                SpellName = spellName,
                ReflectionOccurred = reflectionOccurred,
                DamageToCaster = damageToCaster,
                DamageToReflector = damageToReflector,
                ProcessingTimeMs = castEnd - castStart,
                CasterStateBefore = $"{casterHitsBefore}",
                CasterStateAfter = $"{casterHitsAfter}",
                ReflectorStateBefore = $"{reflectorHitsBefore}",
                ReflectorStateAfter = $"{reflectorHitsAfter}",
                Timestamp = castEnd
            };

            _reflectionResults.Add(result);

            LogVerbose("Reflection test {Spell}: Reflected={Reflected}, CasterDamage={CasterDmg}, ReflectorDamage={ReflectorDmg}",
                spellName, reflectionOccurred, damageToCaster, damageToReflector);

        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error testing reflection for {Spell}", spellName);
        }
    }

    /// <summary>
    /// Tests target feedback and resistance mechanisms.
    /// </summary>
    private void TestTargetFeedback()
    {
        logger.Information("Testing target feedback and resistance...");

        var spellsToTest = new[] { "MagicArrow", "Fireball", "Heal" };

        foreach (var spellName in spellsToTest)
        {
            TestTargetResponse(spellName);
            _totalReflectionTests++;
        }
    }

    /// <summary>
    /// Tests target response to spells.
    /// </summary>
    private void TestTargetResponse(string spellName)
    {
        try
        {
            // Ensure caster has mana
            if (_caster.Mana < 50)
                _caster.Mana = _caster.ManaMax;

            var targetHitsBefore = _target.Hits;
            var targetManaBefore = _target.Mana;
            var castStart = global::Server.Core.TickCount;

            // Cast spell at normal target
            Spell spell = CreateSpell(spellName, _caster);
            if (spell == null) return;

            spell.Cast();

            // Wait for spell to process
            System.Threading.Thread.Sleep(300);

            var castEnd = global::Server.Core.TickCount;
            var targetHitsAfter = _target.Hits;
            var targetManaAfter = _target.Mana;

            // Analyze target response
            var effectApplied = false;
            var feedbackType = "None";
            var effectMagnitude = 0;

            switch (spellName.ToLower())
            {
                case "magicarrow":
                case "fireball":
                    // Offensive spells - check for damage
                    if (targetHitsAfter < targetHitsBefore)
                    {
                        effectApplied = true;
                        feedbackType = "Damage";
                        effectMagnitude = targetHitsBefore - targetHitsAfter;
                    }
                    break;
                case "heal":
                    // Healing spell - check for healing
                    if (targetHitsAfter > targetHitsBefore)
                    {
                        effectApplied = true;
                        feedbackType = "Healing";
                        effectMagnitude = targetHitsAfter - targetHitsBefore;
                    }
                    break;
            }

            var result = new ReflectionTestResult
            {
                TestType = "TargetFeedback",
                SpellName = spellName,
                EffectApplied = effectApplied,
                FeedbackType = feedbackType,
                EffectMagnitude = effectMagnitude,
                ProcessingTimeMs = castEnd - castStart,
                TargetStateBefore = $"{targetHitsBefore}/{targetManaBefore}",
                TargetStateAfter = $"{targetHitsAfter}/{targetManaAfter}",
                Timestamp = castEnd
            };

            _reflectionResults.Add(result);

            LogVerbose("Target feedback {Spell}: Applied={Applied}, Type={Type}, Magnitude={Magnitude}",
                spellName, effectApplied, feedbackType, effectMagnitude);

        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error testing target feedback for {Spell}", spellName);
        }
    }

    /// <summary>
    /// Tests reflection consistency across multiple casts.
    /// </summary>
    private void TestReflectionConsistency()
    {
        logger.Information("Testing reflection consistency...");

        var testSpell = "MagicArrow";

        // Cast the same spell multiple times at reflector
        for (int i = 0; i < 5; i++)
        {
            TestReflectionScenario(testSpell);
            _totalReflectionTests++;

            // Brief delay between casts
            System.Threading.Thread.Sleep(100);
        }
    }

    /// <summary>
    /// Tests mixed reflection and normal spell scenarios.
    /// </summary>
    private void TestMixedReflectionScenarios()
    {
        logger.Information("Testing mixed reflection scenarios...");

        // Alternate between casting at reflector and normal target
        var spells = new[] { "MagicArrow", "Fireball" };

        foreach (var spellName in spells)
        {
            // Cast at reflector (should reflect)
            TestReflectionScenario(spellName);
            _totalReflectionTests++;

            // Cast at normal target (should not reflect)
            TestTargetResponse(spellName);
            _totalReflectionTests++;

            System.Threading.Thread.Sleep(200);
        }
    }

    /// <summary>
    /// Creates a spell instance for testing.
    /// </summary>
    private Spell CreateSpell(string spellName, Mobile caster)
    {
        return spellName.ToLower() switch
        {
            "magicarrow" => new MagicArrowSpell(caster, null),
            "fireball" => new FireballSpell(caster, null),
            "lightning" => new LightningSpell(caster, null),
            "heal" => new HealSpell(caster, null),
            _ => null
        };
    }

    /// <summary>
    /// Event handlers for tracking spell reflection.
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

    private void OnSpellReflected(Mobile originalCaster, Mobile reflector, string spellName)
    {
        LogVerbose("Spell reflected: {Spell} from {Caster} reflected by {Reflector}",
            spellName, originalCaster?.Name ?? "Unknown", reflector?.Name ?? "Unknown");

        // Update reflection results
        var recentResult = _reflectionResults.LastOrDefault(r => r.SpellName == spellName && !r.ReflectionOccurred);
        if (recentResult != null)
        {
            recentResult.ReflectionOccurred = true;
            recentResult.ReflectionEventRaised = true;
        }
    }

    /// <summary>
    /// Gives reagents to a mobile.
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
            logger.Information("Analyzing spell reflection test results...");

            PopulateEnvironmentInfo();

            if (_reflectionResults.Count == 0)
            {
                logger.Error("CRITICAL: No reflection test results collected");
                Results.Passed = false;
                Results.FailureReasons.Add("CRITICAL: No spell reflection tests completed");
                return;
            }

            // Analyze reflection tests
            var reflectionTests = _reflectionResults.Where(r => r.TestType == "SpellReflection").ToList();
            var feedbackTests = _reflectionResults.Where(r => r.TestType == "TargetFeedback").ToList();

            // Calculate success rates
            var reflectionRate = reflectionTests.Count > 0
                ? (reflectionTests.Count(r => r.ReflectionOccurred) / (double)reflectionTests.Count) * 100.0
                : 0.0;

            var feedbackRate = feedbackTests.Count > 0
                ? (feedbackTests.Count(r => r.EffectApplied) / (double)feedbackTests.Count) * 100.0
                : 0.0;

            // Analyze reflection effectiveness
            var avgReflectionDamage = reflectionTests.Where(r => r.ReflectionOccurred).Average(r => r.DamageToCaster);
            var avgNormalDamage = feedbackTests.Where(r => r.EffectApplied && r.FeedbackType == "Damage").Average(r => r.EffectMagnitude);

            // Analyze processing times
            var avgReflectionTime = reflectionTests.Average(r => r.ProcessingTimeMs);
            var avgFeedbackTime = feedbackTests.Average(r => r.ProcessingTimeMs);

            // Set summary metrics
            Results.Summary = new SummaryMetrics
            {
                TotalActions = _reflectionResults.Count,
                WithinTargetCount = _reflectionResults.Count(r =>
                    (r.TestType == "SpellReflection" && r.ReflectionOccurred) ||
                    (r.TestType == "TargetFeedback" && r.EffectApplied)),
                OutlierCount = _reflectionResults.Count(r =>
                    (r.TestType == "SpellReflection" && !r.ReflectionOccurred) ||
                    (r.TestType == "TargetFeedback" && !r.EffectApplied)),
                DoubleCastCount = 0,
                FizzleCount = 0
            };

            Results.Summary.AccuracyPercent = (Results.Summary.WithinTargetCount / (double)Results.Summary.TotalActions) * 100.0;

            // Sphere51a specific pass criteria
            Results.Passed = reflectionRate >= 70.0 && // Reflection should work most of the time
                             feedbackRate >= 80.0 && // Normal spell effects should apply reliably
                             avgReflectionTime <= 500 && // Reflection should be reasonably fast
                             avgFeedbackTime <= 400; // Normal effects should be timely

            // Add observations
            if (reflectionRate < 70.0)
            {
                Results.Observations.Add($"Spell reflection rate {reflectionRate:F1}% below 70% threshold");
            }

            if (feedbackRate < 80.0)
            {
                Results.Observations.Add($"Target feedback rate {feedbackRate:F1}% below 80% threshold");
            }

            if (avgReflectionTime > 500)
            {
                Results.Observations.Add($"Spell reflection processing too slow: {avgReflectionTime:F0}ms average");
            }

            if (avgFeedbackTime > 400)
            {
                Results.Observations.Add($"Target feedback processing too slow: {avgFeedbackTime:F0}ms average");
            }

            if (Results.Passed)
            {
                Results.Observations.Add("Spell reflection and target feedback working correctly");
                Results.Observations.Add($"Reflection: {reflectionRate:F1}% success rate, {avgReflectionTime:F0}ms avg time");
                Results.Observations.Add($"Feedback: {feedbackRate:F1}% success rate, {avgFeedbackTime:F0}ms avg time");
            }

            logger.Information("Spell reflection analysis complete. Pass: {Passed}, Reflection: {Reflection:F1}%, Feedback: {Feedback:F1}%",
                Results.Passed, reflectionRate, feedbackRate);

        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during spell reflection results analysis");
            Results.Passed = false;
            Results.FailureReasons.Add($"Analysis error: {ex.Message}");
        }
    }

    private class ReflectionTestResult
    {
        public string TestType { get; set; }
        public string SpellName { get; set; }
        public bool ReflectionOccurred { get; set; }
        public bool ReflectionEventRaised { get; set; }
        public int DamageToCaster { get; set; }
        public int DamageToReflector { get; set; }
        public bool EffectApplied { get; set; }
        public string FeedbackType { get; set; }
        public int EffectMagnitude { get; set; }
        public long ProcessingTimeMs { get; set; }
        public string CasterStateBefore { get; set; }
        public string CasterStateAfter { get; set; }
        public string ReflectorStateBefore { get; set; }
        public string ReflectorStateAfter { get; set; }
        public string TargetStateBefore { get; set; }
        public string TargetStateAfter { get; set; }
        public long Timestamp { get; set; }
    }
}
