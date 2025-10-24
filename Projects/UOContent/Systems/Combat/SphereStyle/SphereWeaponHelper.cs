/*************************************************************************
 * ModernUO - Sphere 0.51a Combat System
 * File: SphereWeaponHelper.cs
 *
 * Description: Helper methods for integrating Sphere 0.51a weapon swing
 *              mechanics into BaseWeapon. Handles swing validation,
 *              cancellation logic, and damage application.
 *
 * Reference: Sphere0.51aCombatSystem.md - Section 2.1 Combat Flow
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using Server.Items;
using Server.Mobiles;
using Server.Spells;

namespace Server.Systems.Combat.SphereStyle;

/// <summary>
/// Helper methods for Sphere 0.51a weapon swing mechanics.
/// </summary>
/// <remarks>
/// Integrates with BaseWeapon to provide Sphere-style:
/// - Swing cancellation on spell cast
/// - Swing blocking during casting
/// - Independent swing timers
/// - Immediate damage application
/// </remarks>
public static class SphereWeaponHelper
{
    /// <summary>
    /// Validates if a swing can proceed according to Sphere 0.51a rules.
    /// Sphere-style edit: Enhanced swing validation with spell state checks.
    /// </summary>
    /// <param name="attacker">The attacking mobile.</param>
    /// <param name="defender">The defending mobile.</param>
    /// <param name="canSwing">Original canSwing value from BaseWeapon.</param>
    /// <returns>True if swing should proceed; false otherwise.</returns>
    public static bool ValidateSwing(Mobile attacker, Mobile defender, bool canSwing)
    {
        if (!SphereConfig.IsEnabled())
            return canSwing; // Use ModernUO default behavior

        // Start with original validation
        if (!canSwing)
            return false;

        // Sphere-style edit: Check Sphere-specific conditions
        if (!attacker.SphereCanSwing())
        {
            SphereConfig.DebugLog($"{attacker.Name} - Swing blocked by Sphere state");
            return false;
        }

        // Sphere-style edit: Block swing if currently casting
        if (SphereConfig.DisableSwingDuringCast && attacker.SphereIsCasting())
        {
            SphereConfig.DebugLog($"{attacker.Name} - Swing blocked: Currently casting");
            return false;
        }

        // Sphere-style edit: Block swing if in cast delay
        if (SphereConfig.DisableSwingDuringCastDelay && attacker.SphereIsInCastDelay())
        {
            SphereConfig.DebugLog($"{attacker.Name} - Swing blocked: In cast delay");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Handles swing initiation according to Sphere 0.51a rules.
    /// Sphere-style edit: Begins swing tracking and cancels spell if configured.
    /// </summary>
    /// <param name="attacker">The attacking mobile.</param>
    public static void OnSwingBegin(Mobile attacker)
    {
        if (!SphereConfig.IsEnabled())
            return;

        // Sphere-style edit: Begin swing (will cancel spell if configured)
        attacker.SphereBeginSwing();

        SphereConfig.DebugLog($"{attacker.Name} - Swing initiated");
    }

    /// <summary>
    /// Handles swing completion according to Sphere 0.51a rules.
    /// Sphere-style edit: Ends swing tracking and sets next swing time.
    /// </summary>
    /// <param name="attacker">The attacking mobile.</param>
    /// <param name="delay">The delay until next swing.</param>
    public static void OnSwingComplete(Mobile attacker, TimeSpan delay)
    {
        if (!SphereConfig.IsEnabled())
            return;

        // Sphere-style edit: End swing tracking
        attacker.SphereEndSwing();

        // Sphere-style edit: Set next swing time (independent timer)
        if (SphereConfig.IndependentTimers)
        {
            attacker.SphereSetNextSwingTime(delay);
            SphereConfig.DebugLog($"{attacker.Name} - Next swing time: {delay.TotalSeconds}s");
        }
    }

    /// <summary>
    /// Calculates swing speed according to Sphere 0.51a rules.
    /// Sphere-style edit: Optional simplified swing speed calculation.
    /// </summary>
    /// <param name="weapon">The weapon being used.</param>
    /// <param name="attacker">The attacking mobile.</param>
    /// <param name="baseDelay">The base delay from ModernUO calculation.</param>
    /// <returns>The swing delay.</returns>
    public static TimeSpan CalculateSwingSpeed(BaseWeapon weapon, Mobile attacker, TimeSpan baseDelay)
    {
        if (!SphereConfig.IsEnabled() || !SphereConfig.SphereSwingSpeedCalculation)
            return baseDelay; // Use ModernUO default calculation

        // Sphere-style edit: Simplified swing speed calculation
        // Formula: BaseSpeed / (Dexterity / 100)
        var weaponSpeed = weapon.Speed; // Base weapon speed
        var dex = attacker.Dex;

        if (dex <= 0)
            dex = 1; // Prevent division by zero

        // Calculate swing delay in seconds
        var swingDelay = weaponSpeed / (dex / 100.0);

        // Apply min/max bounds
        swingDelay = Math.Max(swingDelay, SphereConfig.MinimumSwingSpeed);
        swingDelay = Math.Min(swingDelay, SphereConfig.MaximumSwingSpeed);

        SphereConfig.DebugLog($"{attacker.Name} - Sphere swing speed: {swingDelay}s (weapon: {weaponSpeed}, dex: {dex})");

        return TimeSpan.FromSeconds(swingDelay);
    }

    /// <summary>
    /// Applies damage immediately according to Sphere 0.51a rules.
    /// Sphere-style edit: Immediate damage application on hit confirmation.
    /// </summary>
    /// <param name="attacker">The attacking mobile.</param>
    /// <param name="defender">The defending mobile.</param>
    /// <param name="damage">The damage amount.</param>
    /// <remarks>
    /// In Sphere 0.51a, damage applies immediately upon hit confirmation.
    /// No deferred damage application or event tick delays.
    /// </remarks>
    public static void ApplyDamage(Mobile attacker, Mobile defender, int damage)
    {
        if (!SphereConfig.IsEnabled() || !SphereConfig.ImmediateDamageApplication)
            return; // Use ModernUO default behavior

        // Sphere-style edit: Immediate damage application
        // Note: This would typically be called from BaseWeapon.OnHit
        // The actual damage application is handled by AOS.Damage or defender.Damage
        // This method primarily serves as a marker and logging point

        SphereConfig.DebugLog($"{attacker.Name} -> {defender.Name}: {damage} damage applied immediately");
    }

    /// <summary>
    /// Handles swing interruption according to Sphere 0.51a rules.
    /// Sphere-style edit: Resets swing timer on interrupt if configured.
    /// </summary>
    /// <param name="attacker">The attacking mobile.</param>
    /// <param name="reason">The reason for interruption.</param>
    public static void InterruptSwing(Mobile attacker, string reason)
    {
        if (!SphereConfig.IsEnabled())
            return;

        // Sphere-style edit: Cancel pending swing
        attacker.SphereCancelSwing(reason);

        SphereConfig.DebugLog($"{attacker.Name} - Swing interrupted: {reason}");
    }

    /// <summary>
    /// Checks if a mobile's weapon is ready to swing.
    /// Sphere-style edit: Uses independent NextSwingTime tracking.
    /// </summary>
    /// <param name="attacker">The mobile to check.</param>
    /// <returns>True if weapon is ready; false otherwise.</returns>
    public static bool IsWeaponReady(Mobile attacker)
    {
        if (!SphereConfig.IsEnabled())
            return true; // Default to ModernUO behavior

        // Check if enough time has passed since last swing
        var state = attacker.GetSphereState();
        var ready = Core.TickCount - state.NextSwingTime >= 0;

        if (!ready)
        {
            SphereConfig.DebugLog($"{attacker.Name} - Weapon not ready (cooldown remaining)");
        }

        return ready;
    }

    /// <summary>
    /// Gets the weapon delay with Sphere-style calculation if enabled.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    /// <param name="attacker">The attacker.</param>
    /// <param name="originalDelay">The original delay from ModernUO.</param>
    /// <returns>The adjusted delay.</returns>
    public static TimeSpan GetWeaponDelay(BaseWeapon weapon, Mobile attacker, TimeSpan originalDelay)
    {
        if (!SphereConfig.IsEnabled())
            return originalDelay;

        // Use Sphere calculation if enabled, otherwise use ModernUO default
        return CalculateSwingSpeed(weapon, attacker, originalDelay);
    }

    /// <summary>
    /// Processes swing effects according to Sphere 0.51a rules.
    /// </summary>
    /// <param name="attacker">The attacker.</param>
    /// <param name="defender">The defender.</param>
    /// <param name="hit">Whether the attack hit.</param>
    public static void ProcessSwingEffects(Mobile attacker, Mobile defender, bool hit)
    {
        if (!SphereConfig.IsEnabled())
            return;

        if (hit)
        {
            SphereConfig.DebugLog($"{attacker.Name} - Hit {defender.Name}");
        }
        else
        {
            SphereConfig.DebugLog($"{attacker.Name} - Missed {defender.Name}");
        }
    }
}
