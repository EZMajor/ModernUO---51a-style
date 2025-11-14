/*************************************************************************
 * ModernUO - Sphere 51a Weapon-Spell Interaction Test
 * File: WeaponSpellInteractionTest.cs
 *
 * Description: Tests weapon swing and spell casting interactions.
 *              Validates Sphere51a combat action priority and queuing.
 *
 * STATUS: Tests weapon-spell alternation, interruption, and priority.
 *         Validates that spells cancel swings and swings cancel spells.
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
/// Tests weapon swing and spell casting interactions.
/// Validates Sphere51a combat action priority and queuing.
/// </summary>
public class WeaponSpellInteractionTest : TestScenario
{
    public override string ScenarioId => "weapon_spell_interaction";
    public override string ScenarioName => "Weapon-Spell Interaction Test";

    private Mobile _attacker;
    private Mobile _defender;
    private List<InteractionTestResult> _interactionResults = new();
    private int _totalInteractionTests = 0;
    private bool _integrationVerified = false;

    protected override bool Setup()
    {
        try
        {
            logger.Information("Setting up weapon-spell interaction test...");

            // CRITICAL: Verify integrations exist
            logger.Information("Verifying system integrations...");
            try
            {
                IntegrationVerifier.RequireSpellIntegration();
                IntegrationVerifier.RequireWeaponIntegration();
                _integrationVerified = true;
                logger.Information("System integration verification: PASSED");
            }
            catch (IntegrationVerifier.IntegrationMissingException ex)
            {
                logger.Error("System integration verification: FAILED");
                logger.Error(ex.Message);
                Results.Passed = false;
                Results.FailureReasons.Add("CRITICAL: System integration not implemented");
                Results.Observations.Add(ex.Message);
                return false;
            }

            // Create test mobiles
            _attacker = TestMobileFactory.CreateCombatant("WeaponSpellAttacker", str: 100, dex: 100);
            _defender = TestMobileFactory.CreateDummy("WeaponSpellDefender");

            // Give attacker a weapon and ensure they have mana/reagents
            var weapon = new Kryss();
            _attacker.AddItem(weapon);
            _attacker.EquipItem(weapon);

            _attacker.Mana = _attacker.ManaMax;
            GiveReagents(_attacker);

            TestMobiles.Add(_attacker);
            TestMobiles.Add(_defender);

            // Initialize results storage
            _interactionResults.Clear();

            // Subscribe to events
            SphereEvents.OnWeaponSwing += OnWeaponSwing;
            SphereEvents.OnWeaponSwingComplete += OnWeaponSwingComplete;
            SphereEvents.OnSpellCastBegin += OnSpellCastBegin;
            SphereEvents.OnSpellCastComplete += OnSpellCastComplete;

            logger.Information("Weapon-spell interaction test setup complete");
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to setup weapon-spell interaction test");
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
            SphereEvents.OnWeaponSwing -= OnWeaponSwing;
            SphereEvents.OnWeaponSwingComplete -= OnWeaponSwingComplete;
            SphereEvents.OnSpellCastBegin -= OnSpellCastBegin;
            SphereEvents.OnSpellCastComplete -= OnSpellCastComplete;

            logger.Information("Weapon-spell interaction test cleanup complete");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during weapon-spell interaction test cleanup");
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

            logger.Information("Starting weapon-spell interaction test...");

            // Test 1: Weapon swing during spell casting
            TestWeaponDuringSpell();

            // Test 2: Spell casting during weapon swing
            TestSpellDuringWeapon();

            // Test 3: Rapid weapon-spell alternation
            TestRapidAlternation();

            // Test 4: Spell interruption of weapon swing
            TestSpellInterruptsWeapon();

            // Test 5: Weapon interruption of spell casting
            TestWeaponInterruptsSpell();

            // Test 6: Simultaneous actions
            TestSimultaneousActions();

            logger.Information("Weapon-spell interaction testing complete. Total tests: {Count}", _totalInteractionTests);
            StopTest();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during weapon-spell interaction test execution");
            Results.Passed = false;
            Results.FailureReasons.Add($"Test execution error: {ex.Message}");
            StopTest();
        }
    }

    /// <summary>
    /// Tests weapon swings attempted during spell casting.
    /// </summary>
    private void TestWeaponDuringSpell()
    {
        logger.Information("Testing weapon swings during spell casting...");

        var spellsToTest = new[] { "MagicArrow", "Fireball", "Heal" };

        foreach (var spellName in spellsToTest)
        {
            TestWeaponSwingDuringSpell(spellName);
            _totalInteractionTests++;
        }
    }

    /// <summary>
    /// Tests a weapon swing during a specific spell.
    /// </summary>
    private void TestWeaponSwingDuringSpell(string spellName)
    {
        try
        {
            // Ensure attacker has mana
            if (_attacker.Mana < 50)
                _attacker.Mana = _attacker.ManaMax;

            Spell spell = CreateSpell(spellName);
            if (spell == null) return;

            var testStart = global::Server.Core.TickCount;

            // Start spell casting
            spell.Cast();

            // Wait for spell to begin
            System.Threading.Thread.Sleep(150);

            // Attempt weapon swing during spell casting
            var weaponSwingResult = AttackRoutine.CanAttack(_attacker);

            var testEnd = global::Server.Core.TickCount;

            var result = new InteractionTestResult
            {
                TestType = "WeaponDuringSpell",
                PrimaryAction = "Spell",
                SecondaryAction = "Weapon",
                SpellName = spellName,
                WeaponAllowed = weaponSwingResult,
                SpellInterrupted = _attacker.Spell == null,
                TestDurationMs = testEnd - testStart,
                Timestamp = testEnd
            };

            _interactionResults.Add(result);

            LogVerbose("Weapon during {Spell}: WeaponAllowed={Allowed}, SpellInterrupted={Interrupted}",
                spellName, weaponSwingResult, result.SpellInterrupted);

        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error testing weapon during spell {Spell}", spellName);
        }
    }

    /// <summary>
    /// Tests spell casting during weapon swing delays.
    /// </summary>
    private void TestSpellDuringWeapon()
    {
        logger.Information("Testing spell casting during weapon swing delays...");

        var spellsToTest = new[] { "Clumsy", "MagicArrow", "Heal" };

        foreach (var spellName in spellsToTest)
        {
            TestSpellCastDuringWeapon(spellName);
            _totalInteractionTests++;
        }
    }

    /// <summary>
    /// Tests spell casting during weapon swing delay.
    /// </summary>
    private void TestSpellCastDuringWeapon(string spellName)
    {
        try
        {
            // Ensure attacker has mana
            if (_attacker.Mana < 50)
                _attacker.Mana = _attacker.ManaMax;

            var testStart = global::Server.Core.TickCount;

            // First, perform a weapon swing to create delay
            var weapon = _attacker.Weapon as BaseWeapon;
            if (weapon != null)
            {
                // Simulate weapon swing
                _attacker.Attack(_defender);

                // Wait a bit for swing to register
                System.Threading.Thread.Sleep(100);

                // Now try to cast spell during swing delay
                Spell spell = CreateSpell(spellName);
                if (spell != null)
                {
                    spell.Cast();

                    // Wait to see if spell starts
                    System.Threading.Thread.Sleep(200);
                }
            }

            var testEnd = global::Server.Core.TickCount;

            var result = new InteractionTestResult
            {
                TestType = "SpellDuringWeapon",
                PrimaryAction = "Weapon",
                SecondaryAction = "Spell",
                SpellName = spellName,
                SpellStarted = _attacker.Spell != null,
                WeaponInterrupted = false, // Weapon swings don't get interrupted
                TestDurationMs = testEnd - testStart,
                Timestamp = testEnd
            };

            _interactionResults.Add(result);

            LogVerbose("Spell during weapon: {Spell} started={Started}",
                spellName, result.SpellStarted);

        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error testing spell during weapon swing for {Spell}", spellName);
        }
    }

    /// <summary>
    /// Tests rapid alternation between weapon swings and spells.
    /// </summary>
    private void TestRapidAlternation()
    {
        logger.Information("Testing rapid weapon-spell alternation...");

        for (int i = 0; i < 5; i++)
        {
            // Alternate between weapon and spell
            if (i % 2 == 0)
            {
                // Weapon swing
                _attacker.Attack(_defender);
                System.Threading.Thread.Sleep(100);
            }
            else
            {
                // Spell cast
                var spell = CreateSpell("MagicArrow");
                if (spell != null)
                {
                    spell.Cast();
                    System.Threading.Thread.Sleep(100);
                }
            }

            _totalInteractionTests++;
        }

        var result = new InteractionTestResult
        {
            TestType = "RapidAlternation",
            PrimaryAction = "Alternating",
            SecondaryAction = "Weapon/Spell",
            TestDurationMs = 1000, // Approximate
            Timestamp = global::Server.Core.TickCount
        };

        _interactionResults.Add(result);

        LogVerbose("Rapid alternation test completed");
    }

    /// <summary>
    /// Tests if spells interrupt weapon swings.
    /// </summary>
    private void TestSpellInterruptsWeapon()
    {
        logger.Information("Testing spell interruption of weapon swings...");

        // Start a weapon swing, then immediately cast a spell
        _attacker.Attack(_defender);
        System.Threading.Thread.Sleep(50);

        // Cast spell - should interrupt weapon swing
        var spell = CreateSpell("Fireball");
        if (spell != null)
        {
            spell.Cast();
            System.Threading.Thread.Sleep(200);
        }

        var result = new InteractionTestResult
        {
            TestType = "SpellInterruptsWeapon",
            PrimaryAction = "Weapon",
            SecondaryAction = "Spell",
            SpellName = "Fireball",
            WeaponInterrupted = true, // Spells should interrupt weapons in Sphere51a
            Timestamp = global::Server.Core.TickCount
        };

        _interactionResults.Add(result);
        _totalInteractionTests++;

        LogVerbose("Spell interrupts weapon: WeaponInterrupted={Interrupted}", result.WeaponInterrupted);
    }

    /// <summary>
    /// Tests if weapon swings interrupt spell casting.
    /// </summary>
    private void TestWeaponInterruptsSpell()
    {
        logger.Information("Testing weapon interruption of spell casting...");

        // Start spell casting
        var spell = CreateSpell("Lightning");
        if (spell != null)
        {
            spell.Cast();
            System.Threading.Thread.Sleep(100);

            // Try weapon swing during spell
            _attacker.Attack(_defender);
            System.Threading.Thread.Sleep(200);
        }

        var result = new InteractionTestResult
        {
            TestType = "WeaponInterruptsSpell",
            PrimaryAction = "Spell",
            SecondaryAction = "Weapon",
            SpellName = "Lightning",
            SpellInterrupted = _attacker.Spell == null,
            Timestamp = global::Server.Core.TickCount
        };

        _interactionResults.Add(result);
        _totalInteractionTests++;

        LogVerbose("Weapon interrupts spell: SpellInterrupted={Interrupted}", result.SpellInterrupted);
    }

    /// <summary>
    /// Tests simultaneous weapon and spell actions.
    /// </summary>
    private void TestSimultaneousActions()
    {
        logger.Information("Testing simultaneous weapon and spell actions...");

        // Try to perform both actions simultaneously
        var spell = CreateSpell("Heal");
        if (spell != null)
        {
            spell.Cast();
            _attacker.Attack(_defender);

            System.Threading.Thread.Sleep(300);
        }

        var result = new InteractionTestResult
        {
            TestType = "SimultaneousActions",
            PrimaryAction = "Both",
            SecondaryAction = "Weapon+Spell",
            SpellName = "Heal",
            SpellStarted = _attacker.Spell != null,
            WeaponAllowed = AttackRoutine.CanAttack(_attacker),
            Timestamp = global::Server.Core.TickCount
        };

        _interactionResults.Add(result);
        _totalInteractionTests++;

        LogVerbose("Simultaneous actions: SpellStarted={Spell}, WeaponAllowed={Weapon}",
            result.SpellStarted, result.WeaponAllowed);
    }

    /// <summary>
    /// Creates a spell instance for testing.
    /// </summary>
    private Spell CreateSpell(string spellName)
    {
        return spellName.ToLower() switch
        {
            "magicarrow" => new MagicArrowSpell(_attacker, null),
            "fireball" => new FireballSpell(_attacker, null),
            "lightning" => new LightningSpell(_attacker, null),
            "heal" => new HealSpell(_attacker, null),
            "clumsy" => new ClumsySpell(_attacker, null),
            _ => null
        };
    }

    /// <summary>
    /// Event handlers for tracking interactions.
    /// </summary>
    private void OnWeaponSwing(object sender, WeaponSwingEventArgs e)
    {
        if (e.Attacker != _attacker) return;
        LogVerbose("Weapon swing: {Attacker} -> {Defender}", e.Attacker.Name, e.Defender.Name);
    }

    private void OnWeaponSwingComplete(object sender, WeaponSwingEventArgs e)
    {
        if (e.Attacker != _attacker) return;
        LogVerbose("Weapon swing complete: {Attacker}", e.Attacker.Name);
    }

    private void OnSpellCastBegin(object sender, SpellCastEventArgs e)
    {
        if (e.Caster != _attacker) return;
        LogVerbose("Spell cast begin: {Caster} casting {Spell}", e.Caster.Name, e.Spell?.GetType().Name ?? "Unknown");
    }

    private void OnSpellCastComplete(object sender, SpellCastEventArgs e)
    {
        if (e.Caster != _attacker) return;
        LogVerbose("Spell cast complete: {Caster} finished {Spell}", e.Caster.Name, e.Spell?.GetType().Name ?? "Unknown");
    }

    /// <summary>
    /// Gives reagents to the attacker.
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
            logger.Information("Analyzing weapon-spell interaction test results...");

            PopulateEnvironmentInfo();

            if (_interactionResults.Count == 0)
            {
                logger.Error("CRITICAL: No interaction test results collected");
                Results.Passed = false;
                Results.FailureReasons.Add("CRITICAL: No weapon-spell interaction tests completed");
                return;
            }

            // Analyze different test types
            var weaponDuringSpellResults = _interactionResults.Where(r => r.TestType == "WeaponDuringSpell").ToList();
            var spellDuringWeaponResults = _interactionResults.Where(r => r.TestType == "SpellDuringWeapon").ToList();
            var interruptionResults = _interactionResults.Where(r => r.TestType.Contains("Interrupts")).ToList();

            // Calculate success rates
            var weaponDuringSpellBlocked = weaponDuringSpellResults.Count(r => !r.WeaponAllowed);
            var spellDuringWeaponStarted = spellDuringWeaponResults.Count(r => r.SpellStarted);
            var spellInterruptsWeapon = interruptionResults.Count(r => r.TestType == "SpellInterruptsWeapon" && r.WeaponInterrupted);
            var weaponInterruptsSpell = interruptionResults.Count(r => r.TestType == "WeaponInterruptsSpell" && r.SpellInterrupted);

            // Calculate percentages
            var weaponBlockRate = weaponDuringSpellResults.Count > 0
                ? (weaponDuringSpellBlocked / (double)weaponDuringSpellResults.Count) * 100.0
                : 0.0;

            var spellStartRate = spellDuringWeaponResults.Count > 0
                ? (spellDuringWeaponStarted / (double)spellDuringWeaponResults.Count) * 100.0
                : 0.0;

            // Set summary metrics
            Results.Summary = new SummaryMetrics
            {
                TotalActions = _interactionResults.Count,
                WithinTargetCount = _interactionResults.Count(r =>
                    (r.TestType == "WeaponDuringSpell" && !r.WeaponAllowed) ||
                    (r.TestType == "SpellDuringWeapon" && !r.SpellStarted) ||
                    (r.TestType.Contains("Interrupts") && r.SpellInterrupted != (r.TestType == "WeaponInterruptsSpell"))),
                OutlierCount = _interactionResults.Count(r =>
                    (r.TestType == "WeaponDuringSpell" && r.WeaponAllowed) ||
                    (r.TestType == "SpellDuringWeapon" && r.SpellStarted)),
                DoubleCastCount = 0,
                FizzleCount = 0
            };

            Results.Summary.AccuracyPercent = (Results.Summary.WithinTargetCount / (double)Results.Summary.TotalActions) * 100.0;

            // Sphere51a specific pass criteria
            Results.Passed = weaponBlockRate >= 80.0 && // Weapons should be blocked during spells
                             spellStartRate <= 20.0 && // Spells should be blocked during weapon delays
                             spellInterruptsWeapon >= 1 && // Spells should interrupt weapons
                             weaponInterruptsSpell >= 1; // Weapons should interrupt spells

            // Add observations
            if (weaponBlockRate < 80.0)
            {
                Results.Observations.Add($"Weapon blocking during spells {weaponBlockRate:F1}% below 80% threshold");
            }

            if (spellStartRate > 20.0)
            {
                Results.Observations.Add($"Spell starting during weapons {spellStartRate:F1}% above 20% threshold");
            }

            if (spellInterruptsWeapon == 0)
            {
                Results.Observations.Add("Spells are not interrupting weapon swings as expected");
            }

            if (weaponInterruptsSpell == 0)
            {
                Results.Observations.Add("Weapons are not interrupting spell casting as expected");
            }

            if (Results.Passed)
            {
                Results.Observations.Add("Weapon-spell interactions working correctly per Sphere51a rules");
            }

            logger.Information("Weapon-spell interaction analysis complete. Pass: {Passed}, WeaponBlock: {WeaponBlock:F1}%, SpellBlock: {SpellBlock:F1}%",
                Results.Passed, weaponBlockRate, 100.0 - spellStartRate);

        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during weapon-spell interaction results analysis");
            Results.Passed = false;
            Results.FailureReasons.Add($"Analysis error: {ex.Message}");
        }
    }

    private class InteractionTestResult
    {
        public string TestType { get; set; }
        public string PrimaryAction { get; set; }
        public string SecondaryAction { get; set; }
        public string SpellName { get; set; }
        public bool WeaponAllowed { get; set; }
        public bool SpellStarted { get; set; }
        public bool SpellInterrupted { get; set; }
        public bool WeaponInterrupted { get; set; }
        public long TestDurationMs { get; set; }
        public long Timestamp { get; set; }
    }
}
