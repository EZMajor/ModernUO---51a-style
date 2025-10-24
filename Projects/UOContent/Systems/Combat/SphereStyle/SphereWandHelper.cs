/*************************************************************************
 * ModernUO - Sphere 0.51a Combat System
 * File: SphereWandHelper.cs
 *
 * Description: Helper methods for integrating Sphere 0.51a wand usage
 *              mechanics. Handles instant-cast behavior, action cancellation,
 *              and independent timer management.
 *
 * Reference: Sphere0.51aCombatSystem.md - Section 2.3 Item and Skill Interaction
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using Server.Items;
using Server.Spells;

namespace Server.Systems.Combat.SphereStyle;

/// <summary>
/// Helper methods for Sphere 0.51a wand usage mechanics.
/// </summary>
/// <remarks>
/// Integrates with BaseWand to provide Sphere-style:
/// - Wand use cancels swing and cast
/// - Instant-cast behavior
/// - Independent wand timer
/// </remarks>
public static class SphereWandHelper
{
    /// <summary>
    /// Validates if wand use can proceed according to Sphere 0.51a rules.
    /// Sphere-style edit: Enhanced wand validation with Sphere state checks.
    /// </summary>
    /// <param name="user">The mobile using the wand.</param>
    /// <param name="wand">The wand being used.</param>
    /// <param name="canUseWand">Original canUseWand value.</param>
    /// <returns>True if wand use should proceed; false otherwise.</returns>
    public static bool ValidateWandUse(Mobile user, BaseWand wand, bool canUseWand)
    {
        if (!SphereConfig.IsEnabled())
            return canUseWand; // Use ModernUO default behavior

        // Start with original validation
        if (!canUseWand)
            return false;

        // Sphere-style edit: Check Sphere-specific conditions
        if (!user.SphereCanUseWand())
        {
            SphereConfig.DebugLog($"{user.Name} - Wand blocked by Sphere state");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Handles wand use initiation according to Sphere 0.51a rules.
    /// Sphere-style edit: Begins wand tracking and cancels swing/cast if configured.
    /// </summary>
    /// <param name="user">The mobile using the wand.</param>
    /// <param name="wand">The wand being used.</param>
    public static void OnWandUseBegin(Mobile user, BaseWand wand)
    {
        if (!SphereConfig.IsEnabled())
            return;

        // Sphere-style edit: Begin wand use (will cancel swing and cast if configured)
        user.SphereBeginWandUse();

        SphereConfig.DebugLog($"{user.Name} - Wand use initiated: {wand.GetType().Name}");
    }

    /// <summary>
    /// Handles wand use completion according to Sphere 0.51a rules.
    /// Sphere-style edit: Sets next wand time.
    /// </summary>
    /// <param name="user">The mobile using the wand.</param>
    /// <param name="wand">The wand being used.</param>
    /// <param name="delay">The delay until next wand use.</param>
    public static void OnWandUseComplete(Mobile user, BaseWand wand, TimeSpan delay)
    {
        if (!SphereConfig.IsEnabled())
            return;

        // Sphere-style edit: Set next wand time (independent timer)
        user.SphereSetNextWandTime(delay);

        SphereConfig.DebugLog($"{user.Name} - Next wand time: {delay.TotalSeconds}s");
    }

    /// <summary>
    /// Gets the wand cast delay according to Sphere 0.51a rules.
    /// Sphere-style edit: Instant cast if configured.
    /// </summary>
    /// <param name="user">The mobile using the wand.</param>
    /// <param name="wand">The wand being used.</param>
    /// <param name="originalDelay">The original delay from ModernUO.</param>
    /// <returns>The adjusted delay.</returns>
    public static TimeSpan GetWandCastDelay(Mobile user, BaseWand wand, TimeSpan originalDelay)
    {
        if (!SphereConfig.IsEnabled())
            return originalDelay;

        // Sphere-style edit: Instant cast if configured
        if (SphereConfig.InstantWandCast)
        {
            SphereConfig.DebugLog($"{user.Name} - Wand instant cast (Sphere mode)");
            return TimeSpan.Zero;
        }

        return originalDelay;
    }

    /// <summary>
    /// Gets the wand use cooldown according to Sphere 0.51a rules.
    /// </summary>
    /// <param name="user">The mobile using the wand.</param>
    /// <param name="wand">The wand being used.</param>
    /// <param name="originalDelay">The original delay from ModernUO.</param>
    /// <returns>The adjusted delay.</returns>
    public static TimeSpan GetWandCooldown(Mobile user, BaseWand wand, TimeSpan originalDelay)
    {
        if (!SphereConfig.IsEnabled())
            return originalDelay;

        // Currently uses ModernUO default (4 seconds)
        // Can be customized if Sphere uses different cooldown
        return originalDelay;
    }

    /// <summary>
    /// Checks if a mobile's wand timer is ready.
    /// Sphere-style edit: Uses independent NextWandTime tracking.
    /// </summary>
    /// <param name="user">The mobile to check.</param>
    /// <returns>True if wand is ready; false otherwise.</returns>
    public static bool IsWandReady(Mobile user)
    {
        if (!SphereConfig.IsEnabled())
            return true; // Default to ModernUO behavior

        // Check if enough time has passed since last wand use
        var state = user.GetSphereState();
        var ready = Core.TickCount - state.NextWandTime >= 0;

        if (!ready)
        {
            SphereConfig.DebugLog($"{user.Name} - Wand not ready (cooldown remaining)");
        }

        return ready;
    }

    /// <summary>
    /// Processes wand spell effects according to Sphere 0.51a rules.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="target">The target.</param>
    /// <param name="wand">The wand.</param>
    /// <param name="spell">The spell from the wand.</param>
    public static void ProcessWandSpellEffects(Mobile user, Mobile target, BaseWand wand, ISpell spell)
    {
        if (!SphereConfig.IsEnabled())
            return;

        SphereConfig.DebugLog($"{user.Name} - Wand spell cast on {target?.Name ?? "unknown"}: {spell.GetType().Name}");
    }

    /// <summary>
    /// Checks if wand should bypass NextSpellTime according to Sphere 0.51a rules.
    /// Sphere-style edit: Wands typically bypass spell timers in Sphere.
    /// </summary>
    /// <param name="user">The mobile using the wand.</param>
    /// <param name="wand">The wand being used.</param>
    /// <returns>True if should bypass NextSpellTime; false otherwise.</returns>
    public static bool BypassSpellTimer(Mobile user, BaseWand wand)
    {
        if (!SphereConfig.IsEnabled())
            return true; // Default ModernUO behavior (wands bypass)

        // Sphere-style: Wands use their own independent timer
        // They don't check or affect NextSpellTime
        return true;
    }

    /// <summary>
    /// Handles wand interruption according to Sphere 0.51a rules.
    /// Sphere-style edit: Processes interruption with Sphere-specific logic.
    /// </summary>
    /// <param name="user">The mobile using the wand.</param>
    /// <param name="reason">The reason for interruption.</param>
    public static void InterruptWandUse(Mobile user, string reason)
    {
        if (!SphereConfig.IsEnabled())
            return;

        SphereConfig.LogCancellation(user, "Wand use", reason);
        SphereConfig.DebugLog($"{user.Name} - Wand use interrupted: {reason}");
    }
}
