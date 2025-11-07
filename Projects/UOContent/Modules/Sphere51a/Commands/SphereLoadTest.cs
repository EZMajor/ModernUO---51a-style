/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: SphereLoadTest.cs
 *
 * Description: Synthetic load testing command for Sphere51a performance profiling.
 *              Simulates combat activity to generate performance metrics.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Modules.Sphere51a.Combat;
using Server.Modules.Sphere51a.Configuration;

namespace Server.Modules.Sphere51a.Commands;

/// <summary>
/// Command to run synthetic load tests for Sphere51a performance profiling.
/// Usage: [SphereLoadTest duration_minutes, concurrent_combatants, attack_frequency]
/// </summary>
public class SphereLoadTest
{
    public static void Initialize()
    {
        CommandSystem.Register("SphereLoadTest", AccessLevel.Administrator, OnCommand);
    }

    [Usage("SphereLoadTest <duration_minutes> <concurrent_combatants> <attack_frequency>")]
    [Description("Runs synthetic combat load test for performance profiling.")]
    private static void OnCommand(CommandEventArgs e)
    {
        var mobile = e.Mobile;

        if (e.Length < 3)
        {
            mobile.SendMessage("Usage: [SphereLoadTest <duration_minutes> <concurrent_combatants> <attack_frequency>]");
            mobile.SendMessage("Example: [SphereLoadTest 5 200 80] - 5 minutes, 200 combatants, 80% attack frequency");
            return;
        }

        var durationMinutes = e.GetInt32(0);
        var concurrentCombatants = e.GetInt32(1);
        var attackFrequencyPercent = e.GetInt32(2);

        if (durationMinutes < 1 || durationMinutes > 60)
        {
            mobile.SendMessage("Duration must be between 1-60 minutes");
            return;
        }

        if (concurrentCombatants < 10 || concurrentCombatants > 2000)
        {
            mobile.SendMessage("Concurrent combatants must be between 10-2000");
            return;
        }

        if (attackFrequencyPercent < 0 || attackFrequencyPercent > 100)
        {
            mobile.SendMessage("Attack frequency must be between 0-100 percent");
            return;
        }

        mobile.SendMessage($"Starting Sphere51a Load Test:");
        mobile.SendMessage($"  Duration: {durationMinutes} minutes");
        mobile.SendMessage($"  Combatants: {concurrentCombatants}");
        mobile.SendMessage($"  Attack Frequency: {attackFrequencyPercent}%");
        mobile.SendMessage($"Use [Perf] to monitor performance during test");

        // Start the load test asynchronously
        Task.Run(() => RunLoadTest(mobile, durationMinutes, concurrentCombatants, attackFrequencyPercent));
    }

    private static async Task RunLoadTest(Mobile admin, int durationMinutes, int concurrentCombatants, int attackFrequencyPercent)
    {
        try
        {
            admin.SendMessage("Load test starting...");

            // Create virtual combatants
            var virtualCombatants = CreateVirtualCombatants(concurrentCombatants);

            admin.SendMessage($"Created {virtualCombatants.Count} virtual combatants");

            // Start performance monitoring
            var startTime = DateTime.UtcNow;
            var endTime = startTime.AddMinutes(durationMinutes);
            var lastReport = startTime;

            admin.SendMessage("Load test running... Use [Perf] to check performance");

            // Main test loop
            while (DateTime.UtcNow < endTime)
            {
                // Simulate combat activity
                SimulateCombatRound(virtualCombatants, attackFrequencyPercent);

                // Progress reporting every 30 seconds
                if (DateTime.UtcNow - lastReport > TimeSpan.FromSeconds(30))
                {
                    var elapsed = DateTime.UtcNow - startTime;
                    var remaining = endTime - DateTime.UtcNow;
                    var progress = (elapsed.TotalMinutes / durationMinutes) * 100;

                    admin.SendMessage($"Load Test Progress: {progress:F1}% complete ({remaining.TotalMinutes:F1} min remaining)");
                    admin.SendMessage($"Active Combatants: {CombatPulse.ActiveCombatantCount}");

                    lastReport = DateTime.UtcNow;
                }

                // Small delay to prevent overwhelming the system
                await Task.Delay(100);
            }

            // Cleanup
            CleanupVirtualCombatants(virtualCombatants);

            var totalElapsed = DateTime.UtcNow - startTime;
            admin.SendMessage($"Load test completed in {totalElapsed.TotalMinutes:F1} minutes");
            admin.SendMessage($"Final active combatants: {CombatPulse.ActiveCombatantCount}");
            admin.SendMessage("Use [Perf] to review final performance metrics");

        }
        catch (Exception ex)
        {
            admin.SendMessage($"Load test failed: {ex.Message}");
            Console.WriteLine($"SphereLoadTest error: {ex}");
        }
    }

    private static List<VirtualCombatant> CreateVirtualCombatants(int count)
    {
        var combatants = new List<VirtualCombatant>();

        for (var i = 0; i < count; i++)
        {
            var combatant = new VirtualCombatant($"VirtualCombatant_{i}", i % 2 == 0); // Alternate attackers/defenders
            combatants.Add(combatant);

            // Register with combat pulse
            CombatPulse.RegisterCombatant(combatant.Mobile);
        }

        return combatants;
    }

    private static void SimulateCombatRound(List<VirtualCombatant> combatants, int attackFrequencyPercent)
    {
        var random = new System.Random();

        foreach (var combatant in combatants)
        {
            // Random chance to attack based on frequency
            if (random.Next(100) < attackFrequencyPercent)
            {
                combatant.PerformAttack();
            }

            // Update activity to prevent cleanup
            CombatPulse.UpdateCombatActivity(combatant.Mobile);
        }
    }

    private static void CleanupVirtualCombatants(List<VirtualCombatant> combatants)
    {
        foreach (var combatant in combatants)
        {
            CombatPulse.UnregisterCombatant(combatant.Mobile);
            combatant.Dispose();
        }
    }

    /// <summary>
    /// Represents a virtual combatant for load testing.
    /// </summary>
    private class VirtualCombatant : IDisposable
    {
        public Mobile Mobile { get; }
        private readonly bool _isAttacker;
        private readonly System.Random _random = new();

        public VirtualCombatant(string name, bool isAttacker)
        {
            _isAttacker = isAttacker;

            // Create a mock mobile - using PlayerMobile for weapon access
            Mobile = new PlayerMobile();
            Mobile.Name = name;

            // Set up basic stats
            Mobile.Str = 50 + _random.Next(50); // 50-100
            Mobile.Dex = 50 + _random.Next(50); // 50-100
            Mobile.Int = 50 + _random.Next(50); // 50-100

            // Give them a weapon
            var weapon = CreateRandomWeapon();
            if (weapon != null)
            {
                Mobile.AddToBackpack(weapon);
                Mobile.EquipItem(weapon);
            }
        }

        private BaseWeapon CreateRandomWeapon()
        {
            var weapons = new[]
            {
                typeof(Katana),
                typeof(Longsword),
                typeof(Broadsword),
                typeof(VikingSword),
                typeof(Mace),
                typeof(WarMace),
                typeof(WarHammer),
                typeof(Bow)
            };

            var weaponType = weapons[_random.Next(weapons.Length)];

            try
            {
                return (BaseWeapon)Activator.CreateInstance(weaponType);
            }
            catch
            {
                return new Katana(); // Fallback
            }
        }

        public void PerformAttack()
        {
            // Find a random target
            var target = FindRandomTarget();
            if (target == null)
                return;

            // Trigger attack through the normal combat system
            try
            {
                var weapon = Mobile.Weapon as BaseWeapon;
                if (weapon != null)
                {
                    // Use the Sphere51a attack routine
                    AttackRoutine.ExecuteAttack(Mobile, target, weapon, SphereInitializer.ActiveTimingProvider);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Virtual combatant attack failed: {ex.Message}");
            }
        }

        private Mobile FindRandomTarget()
        {
            // For load testing, create a simple target mobile if needed
            // This avoids complex target selection logic
            if (_targetMobile == null || _targetMobile.Deleted)
            {
                _targetMobile = new PlayerMobile();
                _targetMobile.Name = "LoadTestTarget";
                CombatPulse.RegisterCombatant(_targetMobile);
            }
            return _targetMobile;
        }

        private Mobile _targetMobile;

        public void Dispose()
        {
            // Clean up the mobile
            if (Mobile?.Deleted == false)
            {
                Mobile.Delete();
            }
        }
    }
}
