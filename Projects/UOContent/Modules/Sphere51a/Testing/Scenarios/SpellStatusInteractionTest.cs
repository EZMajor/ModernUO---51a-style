/*************************************************************************
 * ModernUO - Sphere 51a Spell Status Effect Interactions Test
 * File: SpellStatusInteractionTest.cs
 *
 * Description: Tests spell interactions with status effects and ongoing spell states.
 *              Validates Sphere51a status effect handling and spell stacking.
 *
 * STATUS: Tests buff/debuff interactions, spell effect stacking, and status conflicts.
 *         Validates status effect persistence and interaction rules.
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
/// Tests spell interactions with status effects and ongoing spell states.
/// Validates Sphere51a status effect handling and spell stacking.
/// </summary>
public class SpellStatusInteractionTest : TestScenario
{
    public override string ScenarioId => "spell_status_interaction";
    public override string ScenarioName => "Spell Status Effect Interactions Test";

    private Mobile _caster;
    private Mobile _target;
    private List<StatusInteractionResult> _interactionResults = new();
    private int _totalInteractionTests = 0;
    private bool _integrationVerified = false;

    protected override bool Setup()
    {
        try
        {
            logger.Information("Setting up spell status interaction test...");

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
            _caster = TestMobileFactory.CreateSpellcaster("StatusCaster", intel: 100);
            _target = TestMobileFactory.CreateDummy("StatusTarget");

            // Ensure caster has mana and reagents
            _caster.Mana = _caster.ManaMax;
            GiveReagents(_caster);

            TestMobiles.Add(_caster);
            TestMobiles.Add(_target);

            // Initialize results storage
            _interactionResults.Clear();

            // Subscribe to spell events
            SphereEvents.OnSpellCastBegin += OnSpellCastBegin;
            SphereEvents.OnSpellCastComplete += OnSpellCastComplete;

            logger.Information("Spell status interaction test setup complete");
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to setup spell status interaction test");
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

            logger.Information("Spell status interaction test cleanup complete");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during spell status interaction test cleanup");
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

            logger.Information("Starting spell status interaction test...");

            // Test 1: Buff stacking and interactions
            TestBuffInteractions();

            // Test 2: Debuff stacking and interactions
            TestDebuffInteractions();

            // Test 3: Buff vs debuff conflicts
            TestBuffDebuffConflicts();

            // Test 4: Status effect persistence
            TestStatusPersistence();

            // Test 5: Spell effect combinations
            TestSpellCombinations();

            logger.Information("Spell status interaction testing complete. Total tests: {Count}", _totalInteractionTests);
            StopTest();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during spell status interaction test execution");
            Results.Passed = false;
            Results.FailureReasons.Add($"Test execution error: {ex.Message}");
            StopTest();
        }
    }

    /// <summary>
    /// Tests buff stacking and interaction behaviors.
    /// </summary>
    private void TestBuffInteractions()
    {
        logger.Information("Testing buff interactions and stacking...");

        // Test Bless + Bless stacking
        TestBuffStacking("Bless", "Bless");

        // Test Bless + Strength interactions
        TestBuffStacking("Bless", "Strength");

        // Test multiple buff combinations
        TestBuffStacking("Bless", "Agility");

        _totalInteractionTests += 3;
    }

    /// <summary>
    /// Tests debuff stacking and interaction behaviors.
    /// </summary>
    private void TestDebuffInteractions()
    {
        logger.Information("Testing debuff interactions and stacking...");

        // Test Clumsy + Clumsy stacking
        TestDebuffStacking("Clumsy", "Clumsy");

        // Test Clumsy + Weaken interactions
        TestDebuffStacking("Clumsy", "Weaken");

        // Test Feeblemind + Clumsy interactions
        TestDebuffStacking("Feeblemind", "Clumsy");

        _totalInteractionTests += 3;
    }

    /// <summary>
    /// Tests buff vs debuff conflict scenarios.
    /// </summary>
    private void TestBuffDebuffConflicts()
    {
        logger.Information("Testing buff vs debuff conflicts...");

        // Test Bless vs Clumsy (opposite effects)
        TestBuffDebuffConflict("Bless", "Clumsy");

        // Test Strength vs Weaken
        TestBuffDebuffConflict("Strength", "Weaken");

        // Test Agility vs Clumsy
        TestBuffDebuffConflict("Agility", "Clumsy");

        _totalInteractionTests += 3;
    }

    /// <summary>
    /// Tests status effect persistence over time.
    /// </summary>
    private void TestStatusPersistence()
    {
        logger.Information("Testing status effect persistence...");

        // Test buff persistence
        TestStatusEffectPersistence("Bless", "Buff");

        // Test debuff persistence
        TestStatusEffectPersistence("Clumsy", "Debuff");

        _totalInteractionTests += 2;
    }

    /// <summary>
    /// Tests spell effect combinations and interactions.
    /// </summary>
    private void TestSpellCombinations()
    {
        logger.Information("Testing spell effect combinations...");

        // Test healing on buffed target
        TestSpellOnStatusTarget("Heal", "Bless");

        // Test damage on debuffed target
        TestSpellOnStatusTarget("MagicArrow", "Clumsy");

        // Test buff application on debuffed target
        TestSpellOnStatusTarget("Strength", "Weaken");

        _totalInteractionTests += 3;
    }

    /// <summary>
    /// Tests stacking behavior of two buffs.
    /// </summary>
    private void TestBuffStacking(string buff1, string buff2)
    {
        try
        {
            // Record initial target stats
            var initialStr = _target.RawStr;
            var initialDex = _target.RawDex;
            var initialInt = _target.RawInt;

            // Apply first buff
            ApplyStatusEffect(buff1);
            System.Threading.Thread.Sleep(200);

            var afterFirstBuffStr = _target.RawStr;
            var afterFirstBuffDex = _target.RawDex;
            var afterFirstBuffInt = _target.RawInt;

            // Apply second buff
            ApplyStatusEffect(buff2);
            System.Threading.Thread.Sleep(200);

            var afterSecondBuffStr = _target.RawStr;
            var afterSecondBuffDex = _target.RawDex;
            var afterSecondBuffInt = _target.RawInt;

            // Analyze stacking behavior
            var firstBuffApplied = false;
            var secondBuffApplied = false;
            var stackingType = "None";

            // Determine if buffs stacked, replaced, or conflicted
            if (buff1 == "Bless" && buff2 == "Bless")
            {
                // Bless affects all stats
                firstBuffApplied = afterFirstBuffStr > initialStr || afterFirstBuffDex > initialDex || afterFirstBuffInt > initialInt;
                secondBuffApplied = afterSecondBuffStr > afterFirstBuffStr || afterSecondBuffDex > afterFirstBuffDex || afterSecondBuffInt > afterFirstBuffInt;

                if (secondBuffApplied)
                    stackingType = "Stacked";
                else if (firstBuffApplied)
                    stackingType = "NoStack";
            }
            else if (buff1 == "Bless" && buff2 == "Strength")
            {
                // Bless affects all, Strength affects only Str
                firstBuffApplied = afterFirstBuffStr > initialStr && afterFirstBuffDex > initialDex && afterFirstBuffInt > initialInt;
                secondBuffApplied = afterSecondBuffStr > afterFirstBuffStr;

                if (secondBuffApplied && firstBuffApplied)
                    stackingType = "Complementary";
                else if (secondBuffApplied)
                    stackingType = "Override";
            }

            var result = new StatusInteractionResult
            {
                InteractionType = "BuffStacking",
                Effect1 = buff1,
                Effect2 = buff2,
                Effect1Applied = firstBuffApplied,
                Effect2Applied = secondBuffApplied,
                StackingBehavior = stackingType,
                InitialStats = $"{initialStr}/{initialDex}/{initialInt}",
                AfterFirstStats = $"{afterFirstBuffStr}/{afterFirstBuffDex}/{afterFirstBuffInt}",
                AfterSecondStats = $"{afterSecondBuffStr}/{afterSecondBuffDex}/{afterSecondBuffInt}",
                Timestamp = global::Server.Core.TickCount
            };

            _interactionResults.Add(result);

            LogVerbose("Buff stacking {Effect1}+{Effect2}: {Behavior}, Applied1={Applied1}, Applied2={Applied2}",
                buff1, buff2, stackingType, firstBuffApplied, secondBuffApplied);

        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error testing buff stacking {Buff1} + {Buff2}", buff1, buff2);
        }
    }

    /// <summary>
    /// Tests stacking behavior of two debuffs.
    /// </summary>
    private void TestDebuffStacking(string debuff1, string debuff2)
    {
        try
        {
            // Record initial target stats
            var initialStr = _target.RawStr;
            var initialDex = _target.RawDex;
            var initialInt = _target.RawInt;

            // Apply first debuff
            ApplyStatusEffect(debuff1);
            System.Threading.Thread.Sleep(200);

            var afterFirstDebuffStr = _target.RawStr;
            var afterFirstDebuffDex = _target.RawDex;
            var afterFirstDebuffInt = _target.RawInt;

            // Apply second debuff
            ApplyStatusEffect(debuff2);
            System.Threading.Thread.Sleep(200);

            var afterSecondDebuffStr = _target.RawStr;
            var afterSecondDebuffDex = _target.RawDex;
            var afterSecondDebuffInt = _target.RawInt;

            // Analyze stacking behavior
            var firstDebuffApplied = false;
            var secondDebuffApplied = false;
            var stackingType = "None";

            // Determine debuff stacking behavior
            if (debuff1 == "Clumsy" && debuff2 == "Clumsy")
            {
                firstDebuffApplied = afterFirstDebuffDex < initialDex;
                secondDebuffApplied = afterSecondDebuffDex < afterFirstDebuffDex;

                if (secondDebuffApplied)
                    stackingType = "Stacked";
                else if (firstDebuffApplied)
                    stackingType = "NoStack";
            }
            else if (debuff1 == "Clumsy" && debuff2 == "Weaken")
            {
                firstDebuffApplied = afterFirstDebuffDex < initialDex;
                secondDebuffApplied = afterSecondDebuffStr < afterFirstDebuffStr;

                if (secondDebuffApplied && firstDebuffApplied)
                    stackingType = "Complementary";
                else if (secondDebuffApplied)
                    stackingType = "Override";
            }

            var result = new StatusInteractionResult
            {
                InteractionType = "DebuffStacking",
                Effect1 = debuff1,
                Effect2 = debuff2,
                Effect1Applied = firstDebuffApplied,
                Effect2Applied = secondDebuffApplied,
                StackingBehavior = stackingType,
                InitialStats = $"{initialStr}/{initialDex}/{initialInt}",
                AfterFirstStats = $"{afterFirstDebuffStr}/{afterFirstDebuffDex}/{afterFirstDebuffInt}",
                AfterSecondStats = $"{afterSecondDebuffStr}/{afterSecondDebuffDex}/{afterSecondDebuffInt}",
                Timestamp = global::Server.Core.TickCount
            };

            _interactionResults.Add(result);

            LogVerbose("Debuff stacking {Effect1}+{Effect2}: {Behavior}, Applied1={Applied1}, Applied2={Applied2}",
                debuff1, debuff2, stackingType, firstDebuffApplied, secondDebuffApplied);

        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error testing debuff stacking {Debuff1} + {Debuff2}", debuff1, debuff2);
        }
    }

    /// <summary>
    /// Tests buff vs debuff conflict scenarios.
    /// </summary>
    private void TestBuffDebuffConflict(string buff, string debuff)
    {
        try
        {
            // Record initial target stats
            var initialStr = _target.RawStr;
            var initialDex = _target.RawDex;
            var initialInt = _target.RawInt;

            // Apply buff first
            ApplyStatusEffect(buff);
            System.Threading.Thread.Sleep(200);

            var afterBuffStr = _target.RawStr;
            var afterBuffDex = _target.RawDex;
            var afterBuffInt = _target.RawInt;

            // Apply conflicting debuff
            ApplyStatusEffect(debuff);
            System.Threading.Thread.Sleep(200);

            var afterDebuffStr = _target.RawStr;
            var afterDebuffDex = _target.RawDex;
            var afterDebuffInt = _target.RawInt;

            // Analyze conflict behavior
            var buffApplied = false;
            var debuffApplied = false;
            var conflictType = "None";

            // Check specific buff/debuff conflicts
            if (buff == "Bless" && debuff == "Clumsy")
            {
                buffApplied = afterBuffStr > initialStr && afterBuffDex > initialDex && afterBuffInt > initialInt;
                debuffApplied = afterDebuffDex < afterBuffDex;

                if (debuffApplied && buffApplied)
                    conflictType = "PartialConflict";
                else if (debuffApplied)
                    conflictType = "DebuffOverride";
                else if (buffApplied)
                    conflictType = "BuffOverride";
            }
            else if (buff == "Strength" && debuff == "Weaken")
            {
                buffApplied = afterBuffStr > initialStr;
                debuffApplied = afterDebuffStr < afterBuffStr;

                if (debuffApplied && buffApplied)
                    conflictType = "DirectConflict";
                else if (debuffApplied)
                    conflictType = "DebuffOverride";
                else if (buffApplied)
                    conflictType = "BuffOverride";
            }

            var result = new StatusInteractionResult
            {
                InteractionType = "BuffDebuffConflict",
                Effect1 = buff,
                Effect2 = debuff,
                Effect1Applied = buffApplied,
                Effect2Applied = debuffApplied,
                StackingBehavior = conflictType,
                InitialStats = $"{initialStr}/{initialDex}/{initialInt}",
                AfterFirstStats = $"{afterBuffStr}/{afterBuffDex}/{afterBuffInt}",
                AfterSecondStats = $"{afterDebuffStr}/{afterDebuffDex}/{afterDebuffInt}",
                Timestamp = global::Server.Core.TickCount
            };

            _interactionResults.Add(result);

            LogVerbose("Buff/Debuff conflict {Buff} vs {Debuff}: {Conflict}, BuffApplied={BuffApplied}, DebuffApplied={DebuffApplied}",
                buff, debuff, conflictType, buffApplied, debuffApplied);

        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error testing buff/debuff conflict {Buff} vs {Debuff}", buff, debuff);
        }
    }

    /// <summary>
    /// Tests status effect persistence over time.
    /// </summary>
    private void TestStatusEffectPersistence(string effect, string effectType)
    {
        try
        {
            // Record initial target stats
            var initialStr = _target.RawStr;
            var initialDex = _target.RawDex;
            var initialInt = _target.RawInt;

            // Apply status effect
            ApplyStatusEffect(effect);
            System.Threading.Thread.Sleep(200);

            var afterApplyStr = _target.RawStr;
            var afterApplyDex = _target.RawDex;
            var afterApplyInt = _target.RawInt;

            // Wait for effect to potentially wear off
            System.Threading.Thread.Sleep(2000); // Wait 2 seconds

            var afterWaitStr = _target.RawStr;
            var afterWaitDex = _target.RawDex;
            var afterWaitInt = _target.RawInt;

            // Analyze persistence
            var effectApplied = false;
            var effectPersisted = false;
            var persistenceType = "None";

            if (effectType == "Buff")
            {
                effectApplied = afterApplyStr > initialStr || afterApplyDex > initialDex || afterApplyInt > initialInt;
                effectPersisted = afterWaitStr == afterApplyStr && afterWaitDex == afterApplyDex && afterWaitInt == afterApplyInt;

                if (effectPersisted)
                    persistenceType = "Persistent";
                else if (effectApplied)
                    persistenceType = "Temporary";
            }
            else if (effectType == "Debuff")
            {
                effectApplied = afterApplyStr < initialStr || afterApplyDex < initialDex || afterApplyInt < initialInt;
                effectPersisted = afterWaitStr == afterApplyStr && afterWaitDex == afterApplyDex && afterWaitInt == afterApplyInt;

                if (effectPersisted)
                    persistenceType = "Persistent";
                else if (effectApplied)
                    persistenceType = "Temporary";
            }

            var result = new StatusInteractionResult
            {
                InteractionType = "StatusPersistence",
                Effect1 = effect,
                EffectType = effectType,
                Effect1Applied = effectApplied,
                EffectPersisted = effectPersisted,
                StackingBehavior = persistenceType,
                InitialStats = $"{initialStr}/{initialDex}/{initialInt}",
                AfterFirstStats = $"{afterApplyStr}/{afterApplyDex}/{afterApplyInt}",
                AfterSecondStats = $"{afterWaitStr}/{afterWaitDex}/{afterWaitInt}",
                Timestamp = global::Server.Core.TickCount
            };

            _interactionResults.Add(result);

            LogVerbose("Status persistence {Effect}: Applied={Applied}, Persisted={Persisted}, Type={Type}",
                effect, effectApplied, effectPersisted, persistenceType);

        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error testing status persistence for {Effect}", effect);
        }
    }

    /// <summary>
    /// Tests spell effects on targets with existing status effects.
    /// </summary>
    private void TestSpellOnStatusTarget(string spell, string statusEffect)
    {
        try
        {
            // Apply status effect first
            ApplyStatusEffect(statusEffect);
            System.Threading.Thread.Sleep(200);

            var beforeSpellStr = _target.RawStr;
            var beforeSpellDex = _target.RawDex;
            var beforeSpellInt = _target.RawInt;
            var beforeSpellHits = _target.Hits;

            // Cast spell on status-affected target
            Spell spellInstance = CreateSpell(spell, _caster);
            if (spellInstance == null) return;

            spellInstance.Cast();
            System.Threading.Thread.Sleep(300);

            var afterSpellStr = _target.RawStr;
            var afterSpellDex = _target.RawDex;
            var afterSpellInt = _target.RawInt;
            var afterSpellHits = _target.Hits;

            // Analyze spell effect on status target
            var spellEffectApplied = false;
            var statusEffectModified = false;
            var interactionType = "None";

            switch (spell.ToLower())
            {
                case "heal":
                    spellEffectApplied = afterSpellHits > beforeSpellHits;
                    statusEffectModified = afterSpellStr != beforeSpellStr || afterSpellDex != beforeSpellDex || afterSpellInt != beforeSpellInt;
                    interactionType = spellEffectApplied ? "HealingOnBuffed" : "NoEffect";
                    break;
                case "magicarrow":
                    spellEffectApplied = afterSpellHits < beforeSpellHits;
                    statusEffectModified = afterSpellStr != beforeSpellStr || afterSpellDex != beforeSpellDex || afterSpellInt != beforeSpellInt;
                    interactionType = spellEffectApplied ? "DamageOnDebuffed" : "NoEffect";
                    break;
                case "strength":
                    spellEffectApplied = afterSpellStr > beforeSpellStr;
                    statusEffectModified = afterSpellDex != beforeSpellDex || afterSpellInt != beforeSpellInt;
                    interactionType = spellEffectApplied ? "BuffOnDebuffed" : "NoEffect";
                    break;
            }

            var result = new StatusInteractionResult
            {
                InteractionType = "SpellOnStatusTarget",
                Effect1 = statusEffect,
                Effect2 = spell,
                Effect1Applied = true, // Status effect was applied first
                Effect2Applied = spellEffectApplied,
                StatusModified = statusEffectModified,
                StackingBehavior = interactionType,
                InitialStats = $"{beforeSpellStr}/{beforeSpellDex}/{beforeSpellInt}",
                AfterFirstStats = $"{beforeSpellStr}/{beforeSpellDex}/{beforeSpellInt}",
                AfterSecondStats = $"{afterSpellStr}/{afterSpellDex}/{afterSpellInt}",
                Timestamp = global::Server.Core.TickCount
            };

            _interactionResults.Add(result);

            LogVerbose("Spell on status target {Spell} on {Status}: Applied={Applied}, Modified={Modified}, Type={Type}",
                spell, statusEffect, spellEffectApplied, statusEffectModified, interactionType);

        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error testing spell on status target {Spell} on {Status}", spell, statusEffect);
        }
    }

    /// <summary>
    /// Applies a status effect to the target.
    /// </summary>
    private void ApplyStatusEffect(string effectName)
    {
        Spell spell = CreateSpell(effectName, _caster);
        if (spell != null)
        {
            spell.Cast();
        }
    }

    /// <summary>
    /// Creates a spell instance for testing.
    /// </summary>
    private Spell CreateSpell(string spellName, Mobile caster)
    {
        return spellName.ToLower() switch
        {
            "bless" => new BlessSpell(caster, null),
            "clumsy" => new ClumsySpell(caster, null),
            "strength" => new StrengthSpell(caster, null),
            "weaken" => new WeakenSpell(caster, null),
            "agility" => new AgilitySpell(caster, null),
            "feeblemind" => new FeeblemindSpell(caster, null),
            "heal" => new HealSpell(caster, null),
            "magicarrow" => new MagicArrowSpell(caster, null),
            _ => null
        };
    }

    /// <summary>
    /// Event handlers for tracking spell casts.
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
            logger.Information("Analyzing spell status interaction test results...");

            PopulateEnvironmentInfo();

            if (_interactionResults.Count == 0)
            {
                logger.Error("CRITICAL: No status interaction test results collected");
                Results.Passed = false;
                Results.FailureReasons.Add("CRITICAL: No spell status interaction tests completed");
                return;
            }

            // Analyze different interaction types
            var buffStackingResults = _interactionResults.Where(r => r.InteractionType == "BuffStacking").ToList();
            var debuffStackingResults = _interactionResults.Where(r => r.InteractionType == "DebuffStacking").ToList();
            var conflictResults = _interactionResults.Where(r => r.InteractionType == "BuffDebuffConflict").ToList();
            var persistenceResults = _interactionResults.Where(r => r.InteractionType == "StatusPersistence").ToList();
            var spellOnStatusResults = _interactionResults.Where(r => r.InteractionType == "SpellOnStatusTarget").ToList();

            // Calculate success rates for different interaction types
            var buffStackingRate = buffStackingResults.Count > 0
                ? (buffStackingResults.Count(r => r.Effect1Applied && r.Effect2Applied) / (double)buffStackingResults.Count) * 100.0
                : 0.0;

            var debuffStackingRate = debuffStackingResults.Count > 0
                ? (debuffStackingResults.Count(r => r.Effect1Applied && r.Effect2Applied) / (double)debuffStackingResults.Count) * 100.0
                : 0.0;

            var conflictResolutionRate = conflictResults.Count > 0
                ? (conflictResults.Count(r => r.Effect1Applied || r.Effect2Applied) / (double)conflictResults.Count) * 100.0
                : 0.0;

            var persistenceRate = persistenceResults.Count > 0
                ? (persistenceResults.Count(r => r.EffectPersisted) / (double)persistenceResults.Count) * 100.0
                : 0.0;

            var spellOnStatusRate = spellOnStatusResults.Count > 0
                ? (spellOnStatusResults.Count(r => r.Effect2Applied) / (double)spellOnStatusResults.Count) * 100.0
                : 0.0;

            // Set summary metrics
            Results.Summary = new SummaryMetrics
            {
                TotalActions = _interactionResults.Count,
                WithinTargetCount = _interactionResults.Count(r =>
                    (r.InteractionType == "BuffStacking" && r.Effect1Applied && r.Effect2Applied) ||
                    (r.InteractionType == "DebuffStacking" && r.Effect1Applied && r.Effect2Applied) ||
                    (r.InteractionType == "BuffDebuffConflict" && (r.Effect1Applied || r.Effect2Applied)) ||
                    (r.InteractionType == "StatusPersistence" && r.EffectPersisted) ||
                    (r.InteractionType == "SpellOnStatusTarget" && r.Effect2Applied)),
                OutlierCount = _interactionResults.Count - Results.Summary.WithinTargetCount,
                DoubleCastCount = 0,
                FizzleCount = 0
            };

            Results.Summary.AccuracyPercent = (Results.Summary.WithinTargetCount / (double)Results.Summary.TotalActions) * 100.0;

            // Sphere51a specific pass criteria
            Results.Passed = buffStackingRate >= 70.0 && // Buff stacking should work most of the time
                             debuffStackingRate >= 70.0 && // Debuff stacking should work most of the time
                             conflictResolutionRate >= 60.0 && // Conflicts should be resolved reasonably
                             persistenceRate >= 50.0 && // Status effects should persist adequately
                             spellOnStatusRate >= 75.0; // Spells should work on status-affected targets

            // Add observations
            if (buffStackingRate < 70.0)
            {
                Results.Observations.Add($"Buff stacking rate {buffStackingRate:F1}% below 70% threshold");
            }

            if (debuffStackingRate < 70.0)
            {
                Results.Observations.Add($"Debuff stacking rate {debuffStackingRate:F1}% below 70% threshold");
            }

            if (conflictResolutionRate < 60.0)
            {
                Results.Observations.Add($"Conflict resolution rate {conflictResolutionRate:F1}% below 60% threshold");
            }

            if (persistenceRate < 50.0)
            {
                Results.Observations.Add($"Status persistence rate {persistenceRate:F1}% below 50% threshold");
            }

            if (spellOnStatusRate < 75.0)
            {
                Results.Observations.Add($"Spell on status target rate {spellOnStatusRate:F1}% below 75% threshold");
            }

            if (Results.Passed)
            {
                Results.Observations.Add("Spell status effect interactions working correctly");
                Results.Observations.Add($"Buff stacking: {buffStackingRate:F1}% success rate");
                Results.Observations.Add($"Debuff stacking: {debuffStackingRate:F1}% success rate");
                Results.Observations.Add($"Conflict resolution: {conflictResolutionRate:F1}% success rate");
                Results.Observations.Add($"Status persistence: {persistenceRate:F1}% success rate");
                Results.Observations.Add($"Spell on status targets: {spellOnStatusRate:F1}% success rate");
            }

            logger.Information("Spell status interaction analysis complete. Pass: {Passed}", Results.Passed);
            logger.Information("BuffStack: {Buff:F1}%, DebuffStack: {Debuff:F1}%, Conflicts: {Conflict:F1}%, Persistence: {Persist:F1}%, SpellOnStatus: {Spell:F1}%",
                buffStackingRate, debuffStackingRate, conflictResolutionRate, persistenceRate, spellOnStatusRate);

        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during spell status interaction results analysis");
            Results.Passed = false;
            Results.FailureReasons.Add($"Analysis error: {ex.Message}");
        }
    }

    private class StatusInteractionResult
    {
        public string InteractionType { get; set; }
        public string Effect1 { get; set; }
        public string Effect2 { get; set; }
        public string EffectType { get; set; }
        public bool Effect1Applied { get; set; }
        public bool Effect2Applied { get; set; }
        public bool EffectPersisted { get; set; }
        public bool StatusModified { get; set; }
        public string StackingBehavior { get; set; }
        public string InitialStats { get; set; }
        public string AfterFirstStats { get; set; }
        public string AfterSecondStats { get; set; }
        public long Timestamp { get; set; }
    }
}
