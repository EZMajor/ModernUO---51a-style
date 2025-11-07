/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: AttackRoutine.cs
 *
 * Description: Attack routine handler for global tick system.
 *              Manages animation start and scheduled hit resolution.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Modules.Sphere51a.Configuration;
using Server.Modules.Sphere51a.Extensions;
using Server.Network;

namespace Server.Modules.Sphere51a.Combat;

/// <summary>
/// Handles attack routines for the global tick combat system.
/// Manages immediate animation start and scheduled hit resolution.
/// </summary>
public static class AttackRoutine
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(AttackRoutine));

    /// <summary>
    /// Executes a complete attack routine using the global tick system.
    /// </summary>
    /// <param name="attacker">The attacking mobile</param>
    /// <param name="defender">The defending mobile</param>
    /// <param name="weapon">The weapon being used</param>
    /// <param name="timingProvider">The timing provider for calculations</param>
    public static void ExecuteAttack(Mobile attacker, Mobile defender, BaseWeapon weapon, ITimingProvider timingProvider)
    {
        if (attacker == null || defender == null || weapon == null || timingProvider == null)
            return;

        // Register attacker as active combatant if not already
        if (!CombatPulse.IsActiveCombatant(attacker))
        {
            CombatPulse.RegisterCombatant(attacker);
        }

        // Update combat activity
        CombatPulse.UpdateCombatActivity(attacker);

        // Start animation immediately
        StartAnimation(attacker, weapon);

        // Calculate timing values
        var attackIntervalMs = timingProvider.GetAttackIntervalMs(attacker, weapon);
        var hitOffsetMs = timingProvider.GetAnimationHitOffsetMs(weapon);

        // Schedule hit resolution via global tick
        CombatPulse.ScheduleHitResolution(attacker, defender, weapon, hitOffsetMs);

        // Set next swing time for the attacker
        var nextSwingTime = DateTime.UtcNow.AddMilliseconds(attackIntervalMs);
        SetNextSwingTime(attacker, nextSwingTime);

        SphereConfiguration.DebugLog($"{attacker.Name} - Attack routine: interval={attackIntervalMs}ms, hitOffset={hitOffsetMs}ms");
    }

    /// <summary>
    /// Starts the weapon swing animation immediately.
    /// </summary>
    /// <param name="attacker">The attacking mobile</param>
    /// <param name="weapon">The weapon being used</param>
    private static void StartAnimation(Mobile attacker, BaseWeapon weapon)
    {
        if (attacker == null || weapon == null)
            return;

        try
        {
            // Use weapon's built-in animation method
            weapon.PlaySwingAnimation(attacker);

            // Send swing packet to clients (attacker swinging at self for animation)
            if (attacker.NetState != null)
            {
                attacker.NetState.SendSwing(attacker.Serial, attacker.Serial);
            }

            SphereConfiguration.DebugLog($"{attacker.Name} - Animation started for {weapon.GetType().Name}");
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Failed to start animation for {attacker.Name}");
        }
    }

    /// <summary>
    /// Resolves a hit that was scheduled by the global tick system.
    /// </summary>
    /// <param name="attacker">The attacking mobile</param>
    /// <param name="defender">The defending mobile</param>
    /// <param name="weapon">The weapon used</param>
    public static void ResolveScheduledHit(Mobile attacker, Mobile defender, Item weapon)
    {
        if (attacker?.Deleted != false || defender?.Deleted != false || weapon == null)
            return;

        try
        {
            // Convert to BaseWeapon if possible
            if (weapon is BaseWeapon baseWeapon)
            {
                // Check hit before proceeding
                if (baseWeapon.CheckHit(attacker, defender))
                {
                    // Perform the hit
                    baseWeapon.OnHit(attacker, defender);
                    SphereConfiguration.DebugLog($"{attacker.Name} - Hit resolved on {defender.Name}");
                }
                else
                {
                    // Miss
                    baseWeapon.OnMiss(attacker, defender);
                    SphereConfiguration.DebugLog($"{attacker.Name} - Attack missed {defender.Name}");
                }
            }
            else
            {
                logger.Warning($"Cannot resolve hit: weapon {weapon.GetType().Name} is not a BaseWeapon");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Failed to resolve scheduled hit for {attacker?.Name ?? "null"}");
        }
    }

    /// <summary>
    /// Sets the next swing time for a mobile.
    /// </summary>
    /// <param name="mobile">The mobile</param>
    /// <param name="nextSwingTime">The next swing time</param>
    private static void SetNextSwingTime(Mobile mobile, DateTime nextSwingTime)
    {
        if (mobile == null)
            return;

        // Update the mobile's NextCombatTime for compatibility
        mobile.NextCombatTime = Server.Core.TickCount + (long)(nextSwingTime - DateTime.UtcNow).TotalMilliseconds;

        // If using independent timers, also update Sphere combat state
        if (SphereConfiguration.IndependentTimers)
        {
            mobile.SphereSetNextSwingTime(nextSwingTime - DateTime.UtcNow);
        }

        // CRITICAL: Update CombatPulse timing to prevent machine-gun attacks
        CombatPulse.UpdateNextSwingTime(mobile, nextSwingTime);

        SphereConfiguration.DebugLog($"{mobile.Name} - Next swing time set to {nextSwingTime:HH:mm:ss.fff}");
    }

    /// <summary>
    /// Cancels any pending attack for a mobile.
    /// </summary>
    /// <param name="mobile">The mobile</param>
    /// <param name="reason">Reason for cancellation</param>
    public static void CancelPendingAttack(Mobile mobile, string reason = null)
    {
        if (mobile == null)
            return;

        // Cancel through Sphere combat state
        mobile.SphereCancelSwing(reason);

        SphereConfiguration.DebugLog($"{mobile.Name} - Pending attack cancelled: {reason ?? "No reason"}");
    }

    /// <summary>
    /// Checks if a mobile can perform an attack.
    /// </summary>
    /// <param name="mobile">The mobile</param>
    /// <returns>True if attack is allowed</returns>
    public static bool CanAttack(Mobile mobile)
    {
        if (mobile == null)
            return false;

        // Check Sphere-specific conditions
        return mobile.SphereCanSwing();
    }
}
