/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: VerifyWeaponTiming.cs
 *
 * Description: Command to verify weapon timing calculations.
 *              Shows attack intervals and timing details for debugging.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Modules.Sphere51a;
using Server.Modules.Sphere51a.Extensions;

namespace Server.Modules.Sphere51a.Commands;

/// <summary>
/// Command to verify weapon timing calculations.
/// Usage: [VerifyWeaponTiming]
/// </summary>
public class VerifyWeaponTiming
{
    public static void Initialize()
    {
        CommandSystem.Register("VerifyWeaponTiming", AccessLevel.Player, OnCommand);
    }

    [Usage("VerifyWeaponTiming")]
    [Description("Displays weapon timing information for your equipped weapon.")]
    private static void OnCommand(CommandEventArgs e)
    {
        var mobile = e.Mobile;
        var weapon = mobile.Weapon as BaseWeapon;

        if (weapon == null)
        {
            mobile.SendMessage("You must have a weapon equipped to use this command.");
            return;
        }

        // Get timing information from active provider
        var provider = SphereInitializer.ActiveTimingProvider;
        if (provider == null)
        {
            mobile.SendMessage("Sphere timing system is not active.");
            return;
        }

        var attackInterval = provider.GetAttackIntervalMs(mobile, weapon);
        var hitOffset = provider.GetAnimationHitOffsetMs(weapon);
        var animationDuration = provider.GetAnimationDurationMs(weapon);

        // Display information
        mobile.SendMessage($"=== Weapon Timing Verification ===");
        mobile.SendMessage($"Weapon: {weapon.Name ?? weapon.GetType().Name}");
        mobile.SendMessage($"Provider: {provider.ProviderName}");
        mobile.SendMessage($"Dexterity: {mobile.Dex}");
        mobile.SendMessage($"Attack Interval: {attackInterval}ms ({attackInterval / 1000.0:F2}s)");
        mobile.SendMessage($"Animation Hit Offset: {hitOffset}ms");
        mobile.SendMessage($"Animation Duration: {animationDuration}ms");
        mobile.SendMessage($"Next Swing Time: {mobile.NextCombatTime}");

        // Show Sphere-specific state if available
        var sphereState = mobile.SphereGetCombatStateSummary();
        if (!string.IsNullOrEmpty(sphereState))
        {
            mobile.SendMessage($"Sphere State: {sphereState}");
        }

        // Show combat pulse info if active
        if (Server.Modules.Sphere51a.Combat.CombatPulse.IsActiveCombatant(mobile))
        {
            mobile.SendMessage($"Active Combatant: Yes (Total: {Server.Modules.Sphere51a.Combat.CombatPulse.ActiveCombatantCount})");
        }
        else
        {
            mobile.SendMessage("Active Combatant: No");
        }
    }
}
