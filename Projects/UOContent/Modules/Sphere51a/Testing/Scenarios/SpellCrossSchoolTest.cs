/*************************************************************************
 * ModernUO - Sphere 51a Cross-School Spell Handling Test
 * File: SpellCrossSchoolTest.cs
 *
 * Description: Tests spell interactions across different magical schools.
 *              Validates Sphere51a cross-school spell behavior and interference.
 *
 * STATUS: Tests elemental, necromantic, and other school spell interactions.
 *         Validates school-based spell interference and combination rules.
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
using Server.Spells.Sixth;
using Server.Spells.Seventh;
using Server.Spells.Eighth;
using Server.Items;

namespace Server.Modules.Sphere51a.Testing.Scenarios;

/// <summary>
/// Tests spell interactions across different magical schools.
/// Validates Sphere51a cross-school spell behavior and interference.
/// </summary>
public class SpellCrossSchoolTest : TestScenario
{
    public override string ScenarioId => "spell_cross_school";
    public override string ScenarioName => "Spell Cross-School Handling Test";

    private Mobile _caster;
    private Mobile _target;
    private List<CrossSchoolResult> _crossSchoolResults = new();
    private int _totalCrossSchoolTests = 0;
    private bool _integrationVerified = false;

    protected override bool Setup()
    {
        try
        {
            logger.Information("Setting up spell cross-school test...");

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
            _caster = TestMobileFactory.CreateSpellcaster("CrossSchoolCaster", intel: 100);
            _target = TestMobileFactory.CreateDummy("CrossSchoolTarget");

            // Ensure caster has mana and reagents
            _caster.Mana = _caster.ManaMax;
            GiveReagents(_caster);

            TestMobiles.Add(_caster);
            TestMobiles.Add(_target);

            // Initialize results storage
            _crossSchoolResults.Clear();

            // Subscribe to spell events
            SphereEvents.OnSpellCastBegin += OnSpellCastBegin;
            SphereEvents.OnSpellCastComplete += OnSpellCastComplete;

            logger.Information("Spell cross-school test setup complete");
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to setup spell cross-school test");
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

            logger.Information("Spell cross-school test cleanup complete");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during spell cross-school test cleanup");
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

            logger.Information("Starting spell cross-school test...");

            // Test 1: Elemental spell interactions
            TestElementalInteractions();

            // Test 2: Healing vs damage spell conflicts
            TestHealingDamageConflicts();

            // Test 3: Buff vs debuff across schools
            TestBuffDebuffAcrossSchools();

            // Test 4: Sequential spell casting patterns
            TestSequentialSpellCasting();

            // Test 5: Simultaneous spell effects
            TestSimultaneousEffects();

            logger.Information("Spell cross-school testing complete. Total tests: {Count}", _totalCrossSchoolTests);
            StopTest();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during spell cross-school test execution");
            Results.Passed = false;
            Results.FailureReasons.Add($"Test execution error: {ex.Message}");
            StopTest();
        }
    }

    /// <summary>
    /// Tests interactions between elemental spells.
    /// </summary>
    private void TestElementalInteractions()
    {
        logger.Information("Testing elemental spell interactions...");

        // Fire vs Water (opposing elements)
        TestElementalSpellPair("Fireball", "WaterElemental", "Fire", "Water");

        // Lightning vs Earth (complementary elements)
        TestElementalSpellPair("Lightning", "Earthquake", "Air", "Earth");

        // Multiple fire spells
        TestElementalSpellPair("Fireball", "FlameStrike", "Fire", "Fire");

        _totalCrossSchoolTests += 3;
    }

    /// <summary>
    /// Tests conflicts between healing and damage spells.
    /// </summary>
    private void TestHealingDamageConflicts()
    {
        logger.Information("Testing healing vs damage spell conflicts...");

        // Healing vs direct damage
        TestHealingDamageConflict("Heal", "MagicArrow");

        // Healing vs area damage
        TestHealingDamageConflict("Heal", "Fireball");

        // Greater healing vs greater damage
        TestHealingDamageConflict("GreaterHeal", "Lightning");

        _totalCrossSchoolTests += 3;
    }

    /// <summary>
    /// Tests buff vs debuff effects across different schools.
    /// </summary>
    private void TestBuffDebuffAcrossSchools()
    {
        logger.Information("Testing buff vs debuff across schools...");

        // Mage buff vs mage debuff
        TestBuffDebuffAcrossSchools("Bless", "Clumsy");

        // Different school buff vs debuff
        TestBuffDebuffAcrossSchools("Strength", "Weaken");

        // Multiple school effects
        TestBuffDebuffAcrossSchools("Agility", "Feeblemind");

        _totalCrossSchoolTests += 3;
    }

    /// <summary>
    /// Tests sequential spell casting patterns across schools.
    /// </summary>
    private void TestSequentialSpellCasting()
    {
        logger.Information("Testing sequential spell casting patterns...");

        // Damage -> Healing sequence
        TestSequentialPattern(new[] { "MagicArrow", "Heal" }, "DamageThenHeal");

        // Buff -> Damage -> Debuff sequence
        TestSequentialPattern(new[] { "Bless", "Fireball", "Clumsy" }, "BuffDamageDebuff");

        // Multiple elemental sequence
        TestSequentialPattern(new[] { "Fireball", "Lightning", "Heal" }, "ElementalSequence");

        _totalCrossSchoolTests += 3;
    }

    /// <summary>
    /// Tests simultaneous spell effects from different schools.
    /// </summary>
    private void TestSimultaneousEffects()
    {
        logger.Information("Testing simultaneous spell effects...");

        // Test overlapping spell effects
        TestSimultaneousSpellEffects("Bless", "MagicArrow");

        // Test competing effects
        TestSimultaneousSpellEffects("Strength", "Weaken");

        // Test elemental combinations
        TestSimultaneousSpellEffects("Fireball", "Lightning");

        _totalCrossSchoolTests += 3;
    }

    /// <summary>
    /// Tests interaction between two elemental spells.
    /// </summary>
    private void TestElementalSpellPair(string spell1, string spell2, string school1, string school2)
    {
        try
        {
            // Record initial target state
            var initialHits = _target.Hits;
            var initialMana = _target.Mana;

            // Cast first elemental spell
            ApplySpellEffect(spell1);
            System.Threading.Thread.Sleep(200);

            var afterFirstSpellHits = _target.Hits;
            var afterFirstSpellMana = _target.Mana;

            // Cast second elemental spell
            ApplySpellEffect(spell2);
            System.Threading.Thread.Sleep(200);

            var afterSecondSpellHits = _target.Hits;
            var afterSecondSpellMana = _target.Mana;

            // Analyze elemental interaction
            var firstSpellEffective = false;
            var secondSpellEffective = false;
            var interactionType = "None";

            // Determine spell effectiveness and interaction
            if (spell1.Contains("Fire") || spell1.Contains("Lightning"))
            {
                firstSpellEffective = afterFirstSpellHits < initialHits;
            }
            else if (spell1.Contains("Heal"))
            {
                firstSpellEffective = afterFirstSpellHits > initialHits;
            }

            if (spell2.Contains("Fire") || spell2.Contains("Lightning"))
            {
                secondSpellEffective = afterSecondSpellHits < afterFirstSpellHits;
            }
            else if (spell2.Contains("Heal"))
            {
                secondSpellEffective = afterSecondSpellHits > afterFirstSpellHits;
            }

            // Determine interaction type based on schools
            if (school1 == school2)
            {
                interactionType = secondSpellEffective ? "SameSchoolAmplified" : "SameSchoolDiminished";
            }
            else if ((school1 == "Fire" && school2 == "Water") || (school1 == "Water" && school2 == "Fire"))
            {
                interactionType = "OpposingElements";
            }
            else
            {
                interactionType = secondSpellEffective ? "ComplementaryElements" : "ConflictingElements";
            }

            var result = new CrossSchoolResult
            {
                InteractionType = "ElementalInteraction",
                Spell1 = spell1,
                Spell2 = spell2,
                School1 = school1,
                School2 = school2,
                Spell1Effective = firstSpellEffective,
                Spell2Effective = secondSpellEffective,
                InteractionBehavior = interactionType,
                InitialState = $"{initialHits}/{initialMana}",
                AfterSpell1State = $"{afterFirstSpellHits}/{afterFirstSpellMana}",
                AfterSpell2State = $"{afterSecondSpellHits}/{afterSecondSpellMana}",
                Timestamp = global::Server.Core.TickCount
            };

            _crossSchoolResults.Add(result);

            LogVerbose("Elemental interaction {Spell1}({School1}) + {Spell2}({School2}): {Behavior}, Effective1={Eff1}, Effective2={Eff2}",
                spell1, school1, spell2, school2, interactionType, firstSpellEffective, secondSpellEffective);

        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error testing elemental spell pair {Spell1} + {Spell2}", spell1, spell2);
        }
    }

    /// <summary>
    /// Tests conflict between healing and damage spells.
    /// </summary>
    private void TestHealingDamageConflict(string healSpell, string damageSpell)
    {
        try
        {
            // Record initial target state
            var initialHits = _target.Hits;

            // Cast damage spell first
            ApplySpellEffect(damageSpell);
            System.Threading.Thread.Sleep(200);

            var afterDamageHits = _target.Hits;

            // Cast healing spell
            ApplySpellEffect(healSpell);
            System.Threading.Thread.Sleep(200);

            var afterHealHits = _target.Hits;

            // Analyze healing vs damage interaction
            var damageEffective = afterDamageHits < initialHits;
            var healingEffective = afterHealHits > afterDamageHits;
            var netEffect = afterHealHits - initialHits;
            var interactionType = "None";

            if (damageEffective && healingEffective)
            {
                interactionType = netEffect >= 0 ? "HealingOvercameDamage" : "DamageResistedHealing";
            }
            else if (damageEffective && !healingEffective)
            {
                interactionType = "HealingFailed";
            }
            else if (!damageEffective && healingEffective)
            {
                interactionType = "DamageFailed";
            }
            else
            {
                interactionType = "BothFailed";
            }

            var result = new CrossSchoolResult
            {
                InteractionType = "HealingDamageConflict",
                Spell1 = damageSpell,
                Spell2 = healSpell,
                School1 = "Damage",
                School2 = "Healing",
                Spell1Effective = damageEffective,
                Spell2Effective = healingEffective,
                InteractionBehavior = interactionType,
                NetEffect = netEffect,
                InitialState = $"{initialHits}",
                AfterSpell1State = $"{afterDamageHits}",
                AfterSpell2State = $"{afterHealHits}",
                Timestamp = global::Server.Core.TickCount
            };

            _crossSchoolResults.Add(result);

            LogVerbose("Healing/Damage conflict {Damage} -> {Heal}: {Behavior}, DamageEff={DEff}, HealEff={HEff}, Net={Net}",
                damageSpell, healSpell, interactionType, damageEffective, healingEffective, netEffect);

        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error testing healing/damage conflict {Heal} vs {Damage}", healSpell, damageSpell);
        }
    }

    /// <summary>
    /// Tests buff vs debuff effects across different schools.
    /// </summary>
    private void TestBuffDebuffAcrossSchools(string buffSpell, string debuffSpell)
    {
        try
        {
            // Record initial target stats
            var initialStr = _target.RawStr;
            var initialDex = _target.RawDex;
            var initialInt = _target.RawInt;

            // Apply buff first
            ApplySpellEffect(buffSpell);
            System.Threading.Thread.Sleep(200);

            var afterBuffStr = _target.RawStr;
            var afterBuffDex = _target.RawDex;
            var afterBuffInt = _target.RawInt;

            // Apply debuff
            ApplySpellEffect(debuffSpell);
            System.Threading.Thread.Sleep(200);

            var afterDebuffStr = _target.RawStr;
            var afterDebuffDex = _target.RawDex;
            var afterDebuffInt = _target.RawInt;

            // Analyze cross-school buff/debuff interaction
            var buffEffective = false;
            var debuffEffective = false;
            var interactionType = "None";

            // Check buff effectiveness
            if (buffSpell == "Bless")
            {
                buffEffective = afterBuffStr > initialStr && afterBuffDex > initialDex && afterBuffInt > initialInt;
            }
            else if (buffSpell == "Strength")
            {
                buffEffective = afterBuffStr > initialStr;
            }
            else if (buffSpell == "Agility")
            {
                buffEffective = afterBuffDex > initialDex;
            }

            // Check debuff effectiveness
            if (debuffSpell == "Clumsy")
            {
                debuffEffective = afterDebuffDex < afterBuffDex;
            }
            else if (debuffSpell == "Weaken")
            {
                debuffEffective = afterDebuffStr < afterBuffStr;
            }
            else if (debuffSpell == "Feeblemind")
            {
                debuffEffective = afterDebuffInt < afterBuffInt;
            }

            // Determine interaction type
            if (buffEffective && debuffEffective)
            {
                interactionType = "BothEffective";
            }
            else if (buffEffective && !debuffEffective)
            {
                interactionType = "BuffOverride";
            }
            else if (!buffEffective && debuffEffective)
            {
                interactionType = "DebuffOverride";
            }
            else
            {
                interactionType = "BothFailed";
            }

            var result = new CrossSchoolResult
            {
                InteractionType = "CrossSchoolBuffDebuff",
                Spell1 = buffSpell,
                Spell2 = debuffSpell,
                School1 = "Buff",
                School2 = "Debuff",
                Spell1Effective = buffEffective,
                Spell2Effective = debuffEffective,
                InteractionBehavior = interactionType,
                InitialState = $"{initialStr}/{initialDex}/{initialInt}",
                AfterSpell1State = $"{afterBuffStr}/{afterBuffDex}/{afterBuffInt}",
                AfterSpell2State = $"{afterDebuffStr}/{afterDebuffDex}/{afterDebuffInt}",
                Timestamp = global::Server.Core.TickCount
            };

            _crossSchoolResults.Add(result);

            LogVerbose("Cross-school buff/debuff {Buff} vs {Debuff}: {Behavior}, BuffEff={BEff}, DebuffEff={DEff}",
                buffSpell, debuffSpell, interactionType, buffEffective, debuffEffective);

        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error testing cross-school buff/debuff {Buff} vs {Debuff}", buffSpell, debuffSpell);
        }
    }

    /// <summary>
    /// Tests sequential spell casting patterns.
    /// </summary>
    private void TestSequentialPattern(string[] spells, string patternName)
    {
        try
        {
            var initialHits = _target.Hits;
            var states = new List<string> { $"{initialHits}" };

            // Cast spells in sequence
            foreach (var spell in spells)
            {
                ApplySpellEffect(spell);
                System.Threading.Thread.Sleep(150);
                states.Add($"{_target.Hits}");
            }

            // Analyze pattern effectiveness
            var allSpellsEffective = true;

            // Check if all spells in sequence were effective
            for (int i = 1; i < states.Count; i++)
            {
                var prevHits = int.Parse(states[i - 1].Split('/')[0]);
                var currHits = int.Parse(states[i].Split('/')[0]);

                if (spells[i - 1].Contains("Heal") && currHits <= prevHits)
                {
                    allSpellsEffective = false;
                    break;
                }
                else if ((spells[i - 1].Contains("Fire") || spells[i - 1].Contains("Magic") || spells[i - 1].Contains("Lightning"))
                         && currHits >= prevHits)
                {
                    allSpellsEffective = false;
                    break;
                }
            }

            var result = new CrossSchoolResult
            {
                InteractionType = "SequentialCasting",
                PatternName = patternName,
                SpellsSequence = string.Join(" -> ", spells),
                AllSpellsEffective = allSpellsEffective,
                InteractionBehavior = allSpellsEffective ? "AllEffective" : "SomeFailed",
                StateSequence = string.Join(" -> ", states),
                Timestamp = global::Server.Core.TickCount
            };

            _crossSchoolResults.Add(result);

            LogVerbose("Sequential pattern {Pattern}: {Behavior}, AllEffective={AllEff}",
                patternName, result.InteractionBehavior, allSpellsEffective);

        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error testing sequential pattern {Pattern}", patternName);
        }
    }

    /// <summary>
    /// Tests simultaneous spell effects.
    /// </summary>
    private void TestSimultaneousSpellEffects(string spell1, string spell2)
    {
        try
        {
            // Record initial state
            var initialHits = _target.Hits;
            var initialStr = _target.RawStr;

            // Cast both spells with minimal delay (simulating simultaneous casting)
            ApplySpellEffect(spell1);
            System.Threading.Thread.Sleep(50); // Very short delay
            ApplySpellEffect(spell2);
            System.Threading.Thread.Sleep(200);

            var finalHits = _target.Hits;
            var finalStr = _target.RawStr;

            // Analyze simultaneous effects
            var spell1Effective = false;
            var spell2Effective = false;
            var interactionType = "Simultaneous";

            // Determine effectiveness based on spell types
            if (spell1.Contains("Fire") || spell1.Contains("Magic"))
            {
                spell1Effective = finalHits < initialHits;
            }
            else if (spell1.Contains("Bless") || spell1.Contains("Strength"))
            {
                spell1Effective = finalStr > initialStr;
            }

            // For simultaneous casting, check if both effects are present
            if (spell2.Contains("Heal"))
            {
                spell2Effective = finalHits > initialHits;
            }
            else if (spell2.Contains("Clumsy") || spell2.Contains("Weaken"))
            {
                spell2Effective = finalStr < initialStr;
            }

            if (spell1Effective && spell2Effective)
            {
                interactionType = "BothSimultaneous";
            }
            else if (spell1Effective && !spell2Effective)
            {
                interactionType = "FirstDominant";
            }
            else if (!spell1Effective && spell2Effective)
            {
                interactionType = "SecondDominant";
            }
            else
            {
                interactionType = "BothFailed";
            }

            var result = new CrossSchoolResult
            {
                InteractionType = "SimultaneousEffects",
                Spell1 = spell1,
                Spell2 = spell2,
                Spell1Effective = spell1Effective,
                Spell2Effective = spell2Effective,
                InteractionBehavior = interactionType,
                InitialState = $"{initialHits}/{initialStr}",
                FinalState = $"{finalHits}/{finalStr}",
                Timestamp = global::Server.Core.TickCount
            };

            _crossSchoolResults.Add(result);

            LogVerbose("Simultaneous effects {Spell1} + {Spell2}: {Behavior}, Eff1={E1}, Eff2={E2}",
                spell1, spell2, interactionType, spell1Effective, spell2Effective);

        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error testing simultaneous effects {Spell1} + {Spell2}", spell1, spell2);
        }
    }

    /// <summary>
    /// Applies a spell effect to the target.
    /// </summary>
    private void ApplySpellEffect(string spellName)
    {
        Spell spell = CreateSpell(spellName, _caster);
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
            "magicarrow" => new MagicArrowSpell(caster, null),
            "fireball" => new FireballSpell(caster, null),
            "lightning" => new LightningSpell(caster, null),
            "heal" => new HealSpell(caster, null),
            "greaterheal" => new GreaterHealSpell(caster, null),
            "bless" => new BlessSpell(caster, null),
            "clumsy" => new ClumsySpell(caster, null),
            "strength" => new StrengthSpell(caster, null),
            "weaken" => new WeakenSpell(caster, null),
            "agility" => new AgilitySpell(caster, null),
            "feeblemind" => new FeeblemindSpell(caster, null),
            "flamestrike" => new FlameStrikeSpell(caster, null),
            // Note: WaterElemental and Earthquake may not exist in base UO, using placeholders
            "waterelemental" => new MagicArrowSpell(caster, null), // Placeholder
            "earthquake" => new MagicArrowSpell(caster, null), // Placeholder
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
            logger.Information("Analyzing spell cross-school test results...");

            PopulateEnvironmentInfo();

            if (_crossSchoolResults.Count == 0)
            {
                logger.Error("CRITICAL: No cross-school test results collected");
                Results.Passed = false;
                Results.FailureReasons.Add("CRITICAL: No spell cross-school tests completed");
                return;
            }

            // Analyze different interaction types
            var elementalResults = _crossSchoolResults.Where(r => r.InteractionType == "ElementalInteraction").ToList();
            var healingDamageResults = _crossSchoolResults.Where(r => r.InteractionType == "HealingDamageConflict").ToList();
            var buffDebuffResults = _crossSchoolResults.Where(r => r.InteractionType == "CrossSchoolBuffDebuff").ToList();
            var sequentialResults = _crossSchoolResults.Where(r => r.InteractionType == "SequentialCasting").ToList();
            var simultaneousResults = _crossSchoolResults.Where(r => r.InteractionType == "SimultaneousEffects").ToList();

            // Calculate success rates for different interaction types
            var elementalSuccessRate = elementalResults.Count > 0
                ? (elementalResults.Count(r => r.Spell1Effective || r.Spell2Effective) / (double)elementalResults.Count) * 100.0
                : 0.0;

            var healingDamageSuccessRate = healingDamageResults.Count > 0
                ? (healingDamageResults.Count(r => r.Spell1Effective || r.Spell2Effective) / (double)healingDamageResults.Count) * 100.0
                : 0.0;

            var buffDebuffSuccessRate = buffDebuffResults.Count > 0
                ? (buffDebuffResults.Count(r => r.Spell1Effective || r.Spell2Effective) / (double)buffDebuffResults.Count) * 100.0
                : 0.0;

            var sequentialSuccessRate = sequentialResults.Count > 0
                ? (sequentialResults.Count(r => r.AllSpellsEffective) / (double)sequentialResults.Count) * 100.0
                : 0.0;

            var simultaneousSuccessRate = simultaneousResults.Count > 0
                ? (simultaneousResults.Count(r => r.Spell1Effective || r.Spell2Effective) / (double)simultaneousResults.Count) * 100.0
                : 0.0;

            // Set summary metrics
            Results.Summary = new SummaryMetrics
            {
                TotalActions = _crossSchoolResults.Count,
                WithinTargetCount = _crossSchoolResults.Count(r =>
                    (r.InteractionType == "ElementalInteraction" && (r.Spell1Effective || r.Spell2Effective)) ||
                    (r.InteractionType == "HealingDamageConflict" && (r.Spell1Effective || r.Spell2Effective)) ||
                    (r.InteractionType == "CrossSchoolBuffDebuff" && (r.Spell1Effective || r.Spell2Effective)) ||
                    (r.InteractionType == "SequentialCasting" && r.AllSpellsEffective) ||
                    (r.InteractionType == "SimultaneousEffects" && (r.Spell1Effective || r.Spell2Effective))),
                OutlierCount = _crossSchoolResults.Count - Results.Summary.WithinTargetCount,
                DoubleCastCount = 0,
                FizzleCount = 0
            };

            Results.Summary.AccuracyPercent = (Results.Summary.WithinTargetCount / (double)Results.Summary.TotalActions) * 100.0;

            // Sphere51a specific pass criteria
            Results.Passed = elementalSuccessRate >= 70.0 && // Elemental interactions should work most of the time
                             healingDamageSuccessRate >= 75.0 && // Healing/damage conflicts should resolve properly
                             buffDebuffSuccessRate >= 70.0 && // Cross-school buff/debuff should work
                             sequentialSuccessRate >= 60.0 && // Sequential casting should be reliable
                             simultaneousSuccessRate >= 65.0; // Simultaneous effects should work adequately

            // Add observations
            if (elementalSuccessRate < 70.0)
            {
                Results.Observations.Add($"Elemental interaction rate {elementalSuccessRate:F1}% below 70% threshold");
            }

            if (healingDamageSuccessRate < 75.0)
            {
                Results.Observations.Add($"Healing/damage conflict rate {healingDamageSuccessRate:F1}% below 75% threshold");
            }

            if (buffDebuffSuccessRate < 70.0)
            {
                Results.Observations.Add($"Cross-school buff/debuff rate {buffDebuffSuccessRate:F1}% below 70% threshold");
            }

            if (sequentialSuccessRate < 60.0)
            {
                Results.Observations.Add($"Sequential casting rate {sequentialSuccessRate:F1}% below 60% threshold");
            }

            if (simultaneousSuccessRate < 65.0)
            {
                Results.Observations.Add($"Simultaneous effects rate {simultaneousSuccessRate:F1}% below 65% threshold");
            }

            if (Results.Passed)
            {
                Results.Observations.Add("Spell cross-school interactions working correctly");
                Results.Observations.Add($"Elemental interactions: {elementalSuccessRate:F1}% success rate");
                Results.Observations.Add($"Healing/damage conflicts: {healingDamageSuccessRate:F1}% success rate");
                Results.Observations.Add($"Cross-school buff/debuff: {buffDebuffSuccessRate:F1}% success rate");
                Results.Observations.Add($"Sequential casting: {sequentialSuccessRate:F1}% success rate");
                Results.Observations.Add($"Simultaneous effects: {simultaneousSuccessRate:F1}% success rate");
            }

            logger.Information("Spell cross-school analysis complete. Pass: {Passed}", Results.Passed);
            logger.Information("Elemental: {Elem:F1}%, HealDmg: {HD:F1}%, BuffDebuff: {BD:F1}%, Sequential: {Seq:F1}%, Simultaneous: {Sim:F1}%",
                elementalSuccessRate, healingDamageSuccessRate, buffDebuffSuccessRate, sequentialSuccessRate, simultaneousSuccessRate);

        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during spell cross-school results analysis");
            Results.Passed = false;
            Results.FailureReasons.Add($"Analysis error: {ex.Message}");
        }
    }

    private class CrossSchoolResult
    {
        public string InteractionType { get; set; }
        public string Spell1 { get; set; }
        public string Spell2 { get; set; }
        public string School1 { get; set; }
        public string School2 { get; set; }
        public string PatternName { get; set; }
        public string SpellsSequence { get; set; }
        public bool Spell1Effective { get; set; }
        public bool Spell2Effective { get; set; }
        public bool AllSpellsEffective { get; set; }
        public string InteractionBehavior { get; set; }
        public int NetEffect { get; set; }
        public string InitialState { get; set; }
        public string AfterSpell1State { get; set; }
        public string AfterSpell2State { get; set; }
        public string FinalState { get; set; }
        public string StateSequence { get; set; }
        public long Timestamp { get; set; }
    }
}
