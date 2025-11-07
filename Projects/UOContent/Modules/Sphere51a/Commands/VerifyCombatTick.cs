/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: VerifyCombatTick.cs
 *
 * Description: Command to verify combat tick system status.
 *              Shows global pulse information and active combatants.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using Server.Commands;
using Server.Modules.Sphere51a;
using Server.Modules.Sphere51a.Combat;

namespace Server.Modules.Sphere51a.Commands;

/// <summary>
/// Command to verify combat tick system status.
/// Usage: [VerifyCombatTick]
/// </summary>
public class VerifyCombatTick
{
    public static void Initialize()
    {
        CommandSystem.Register("VerifyCombatTick", AccessLevel.Player, OnCommand);
    }

    [Usage("VerifyCombatTick")]
    [Description("Displays combat tick system status and active combatants.")]
    private static void OnCommand(CommandEventArgs e)
    {
        var mobile = e.Mobile;

        mobile.SendMessage($"=== Combat Tick Verification ===");

        // Global pulse status
        mobile.SendMessage($"Global Pulse Enabled: {Server.Modules.Sphere51a.Configuration.SphereConfiguration.UseGlobalPulse}");
        mobile.SendMessage($"Combat Pulse Initialized: {CombatPulse.IsInitialized}");
        mobile.SendMessage($"Global Tick Interval: {Server.Modules.Sphere51a.Configuration.SphereConfiguration.GlobalTickMs}ms");
        mobile.SendMessage($"Combat Idle Timeout: {Server.Modules.Sphere51a.Configuration.SphereConfiguration.CombatIdleTimeoutMs}ms");

        // Active combatants
        var activeCount = CombatPulse.ActiveCombatantCount;
        mobile.SendMessage($"Active Combatants: {activeCount}");

        // Personal status
        var isActive = CombatPulse.IsActiveCombatant(mobile);
        mobile.SendMessage($"You are Active Combatant: {isActive}");

        // Timing provider
        var provider = SphereInitializer.ActiveTimingProvider;
        mobile.SendMessage($"Active Timing Provider: {provider?.ProviderName ?? "None"}");

        // System status
        mobile.SendMessage($"Sphere System Enabled: {Server.Modules.Sphere51a.Configuration.SphereConfiguration.Enabled}");
        mobile.SendMessage($"Sphere System Initialized: {SphereInitializer.IsInitialized}");

        // Performance note
        if (activeCount > 100)
        {
            mobile.SendMessage($"Warning: High active combatant count ({activeCount}) may impact performance.");
        }
        else if (activeCount > 50)
        {
            mobile.SendMessage($"Note: Moderate active combatant count ({activeCount}).");
        }
        else
        {
            mobile.SendMessage($"Active combatant count ({activeCount}) is within normal range.");
        }
    }
}
