/*************************************************************************
 * ModernUO - Sphere 0.51a Combat System
 * File: SphereSpellHelper.cs
 *
 * Description: Helper methods for integrating Sphere 0.51a spellcasting
 *              mechanics into Spell system. Handles cast validation,
 *              movement permissions, and recovery delays.
 *
 * Reference: Sphere0.51aCombatSystem.md - Section 2.2 Spellcasting
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using Server.Spells;

namespace Server.Systems.Combat.SphereStyle;

/// <summary>
/// Helper methods for Sphere 0.51a spellcasting mechanics.
/// </summary>
/// <remarks>
/// Integrates with Spell to provide Sphere-style:
/// - Movement during casting
/// - No post-cast recovery
/// - Swing cancellation on cast start
/// - Independent spell timers
/// </remarks>
public static class SphereSpellHelper
{
    /// <summary>
    /// Validates if a cast can proceed according to Sphere 0.51a rules.
    /// Sphere-style edit: Enhanced cast validation with Sphere state checks.
    /// </summary>
    /// <param name="caster">The casting mobile.</param>
    /// <param name="spell">The spell being cast.</param>
    /// <param name="canCast">Original canCast value from Spell.</param>
    /// <returns>True if cast should proceed; false otherwise.</returns>
    public static bool ValidateCast(Mobile caster, ISpell spell, bool canCast)
    {
        if (!SphereConfig.IsEnabled())
            return canCast; // Use ModernUO default behavior

        // Start with original validation
        if (!canCast)
            return false;

        // Sphere-style edit: Check Sphere-specific conditions
        if (!caster.SphereCanCast())
        {
            SphereConfig.DebugLog($"{caster.Name} - Cast blocked by Sphere state");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Handles spell cast initiation according to Sphere 0.51a rules.
    /// Sphere-style edit: Begins spell tracking and cancels swing if configured.
    /// </summary>
    /// <param name="caster">The casting mobile.</param>
    /// <param name="spell">The spell being cast.</param>
    public static void OnCastBegin(Mobile caster, ISpell spell)
    {
        if (!SphereConfig.IsEnabled())
            return;

        //Sphere-style edit: Cancel active bandage if spell is being cast
        if (SphereConfig.SpellCancelSwing) // Use same config as swing cancellation
        {
            var bandageContext = Items.BandageContext.GetContext(caster);
            if (bandageContext != null)
            {
                bandageContext.StopHeal();
                SphereConfig.DebugLog($"{caster.Name} - Bandage interrupted by spell cast");
            }
        }

        // Sphere-style edit: Begin spell cast (will cancel swing if configured)
        caster.SphereBeginSpellCast(spell);

        SphereConfig.DebugLog($"{caster.Name} - Spell cast initiated: {spell.GetType().Name}");
    }

    /// <summary>
    /// Handles spell cast completion according to Sphere 0.51a rules.
    /// Sphere-style edit: Ends spell tracking and optionally removes post-cast recovery.
    /// </summary>
    /// <param name="caster">The casting mobile.</param>
    /// <param name="spell">The spell that was cast.</param>
    /// <param name="completed">True if cast completed successfully.</param>
    public static void OnCastComplete(Mobile caster, ISpell spell, bool completed)
    {
        if (!SphereConfig.IsEnabled())
            return;

        // Sphere-style edit: End spell tracking
        caster.SphereEndSpellCast(completed);

        SphereConfig.DebugLog($"{caster.Name} - Spell cast ended: {spell.GetType().Name} (completed: {completed})");
    }

    /// <summary>
    /// Handles entry into cast delay phase according to Sphere 0.51a rules.
    /// Sphere-style edit: Tracks cast delay state (post-target, pre-effect).
    /// </summary>
    /// <param name="caster">The casting mobile.</param>
    public static void OnEnterCastDelay(Mobile caster)
    {
        if (!SphereConfig.IsEnabled())
            return;

        // Sphere-style edit: Enter cast delay phase
        caster.SphereEnterCastDelay();

        SphereConfig.DebugLog($"{caster.Name} - Entered cast delay phase");
    }

    /// <summary>
    /// Checks if movement should be blocked during casting according to Sphere 0.51a rules.
    /// Sphere-style edit: Allows movement during casting if configured.
    /// </summary>
    /// <param name="caster">The casting mobile.</param>
    /// <param name="spell">The spell being cast.</param>
    /// <param name="defaultBlocksMovement">Default BlocksMovement value from spell.</param>
    /// <returns>True if movement should be blocked; false otherwise.</returns>
    public static bool CheckBlocksMovement(Mobile caster, ISpell spell, bool defaultBlocksMovement)
    {
        if (!SphereConfig.IsEnabled())
            return defaultBlocksMovement; // Use ModernUO default

        // Sphere-style edit: Allow movement during cast if configured
        if (SphereConfig.AllowMovementDuringCast)
        {
            SphereConfig.DebugLog($"{caster.Name} - Movement allowed during cast (Sphere mode)");
            return false; // Do not block movement
        }

        return defaultBlocksMovement;
    }

    /// <summary>
    /// Gets the post-cast recovery delay according to Sphere 0.51a rules.
    /// Sphere-style edit: Removes post-cast recovery delay if configured.
    /// </summary>
    /// <param name="caster">The casting mobile.</param>
    /// <param name="spell">The spell being cast.</param>
    /// <param name="defaultRecovery">Default recovery from GetCastRecovery().</param>
    /// <returns>The adjusted recovery time.</returns>
    public static TimeSpan GetCastRecovery(Mobile caster, ISpell spell, TimeSpan defaultRecovery)
    {
        if (!SphereConfig.IsEnabled())
            return defaultRecovery; // Use ModernUO default

        // Sphere-style edit: Remove post-cast recovery if configured
        if (SphereConfig.RemovePostCastRecovery)
        {
            SphereConfig.DebugLog($"{caster.Name} - Post-cast recovery removed (Sphere mode)");
            return TimeSpan.Zero; // No recovery delay
        }

        return defaultRecovery;
    }

    /// <summary>
    /// Handles spell interruption according to Sphere 0.51a rules.
    /// Sphere-style edit: Processes interruption with Sphere-specific logic.
    /// </summary>
    /// <param name="caster">The casting mobile.</param>
    /// <param name="spell">The spell being interrupted.</param>
    /// <param name="type">The type of disturbance.</param>
    public static void OnSpellDisturb(Mobile caster, ISpell spell, DisturbType type)
    {
        if (!SphereConfig.IsEnabled())
            return;

        // Sphere-style edit: Handle interruption
        caster.SphereEndSpellCast(false);

        SphereConfig.DebugLog($"{caster.Name} - Spell disturbed: {type}");
    }

    /// <summary>
    /// Checks if spell should be disturbed by damage according to Sphere 0.51a rules.
    /// Sphere-style edit: Optional damage-based fizzle configuration.
    /// </summary>
    /// <param name="caster">The casting mobile.</param>
    /// <param name="spell">The spell being cast.</param>
    /// <param name="defaultDisturbOnDamage">Default damage disturb behavior.</param>
    /// <returns>True if damage should disturb; false otherwise.</returns>
    public static bool CheckDamageDisturb(Mobile caster, ISpell spell, bool defaultDisturbOnDamage)
    {
        if (!SphereConfig.IsEnabled())
            return defaultDisturbOnDamage;

        // Sphere-style edit: Disable damage fizzle if configured
        if (!SphereConfig.DamageBasedFizzle)
        {
            SphereConfig.DebugLog($"{caster.Name} - Damage fizzle disabled (Sphere mode)");
            return false; // Do not disturb on damage
        }

        // Sphere-style edit: Restricted fizzle triggers
        if (SphereConfig.RestrictedFizzleTriggers)
        {
            // Only allow fizzle from specific actions, not damage
            return false;
        }

        return defaultDisturbOnDamage;
    }

    /// <summary>
    /// Checks if NextSpellTime should be checked according to Sphere 0.51a rules.
    /// Sphere-style edit: Handles independent spell timer logic.
    /// </summary>
    /// <param name="caster">The casting mobile.</param>
    /// <param name="spell">The spell being cast.</param>
    /// <param name="defaultCheckNextSpellTime">Default CheckNextSpellTime value.</param>
    /// <returns>True if NextSpellTime should be checked; false otherwise.</returns>
    public static bool CheckNextSpellTime(Mobile caster, ISpell spell, bool defaultCheckNextSpellTime)
    {
        if (!SphereConfig.IsEnabled())
            return defaultCheckNextSpellTime;

        // Sphere-style edit: Use independent timer if configured
        if (SphereConfig.IndependentTimers)
        {
            // Check Sphere-specific NextSpellTime instead of Mobile.NextSpellTime
            var state = caster.GetSphereState();
            if (Core.TickCount - state.NextSpellTime < 0)
            {
                caster.SendLocalizedMessage(502644); // You have not yet recovered from casting a spell.
                return false;
            }

            // We've already checked, so tell the spell system not to check again
            return false;
        }

        return defaultCheckNextSpellTime;
    }

    /// <summary>
    /// Sets the next spell time according to Sphere 0.51a rules.
    /// Sphere-style edit: Uses independent NextSpellTime tracking.
    /// </summary>
    /// <param name="caster">The casting mobile.</param>
    /// <param name="delay">The delay until next allowed cast.</param>
    public static void SetNextSpellTime(Mobile caster, TimeSpan delay)
    {
        if (!SphereConfig.IsEnabled())
            return;

        // Sphere-style edit: Set next spell time (independent timer)
        if (SphereConfig.IndependentTimers)
        {
            caster.SphereSetNextSpellTime(delay);
            SphereConfig.DebugLog($"{caster.Name} - Next spell time: {delay.TotalSeconds}s");
        }
    }

    /// <summary>
    /// Handles mana deduction timing according to Sphere 0.51a rules.
    /// Sphere-style edit: Optional mana deduction at target confirmation.
    /// </summary>
    /// <param name="caster">The casting mobile.</param>
    /// <param name="spell">The spell being cast.</param>
    /// <param name="manaCost">The mana cost.</param>
    /// <param name="atCastStart">True if called at cast start; false if at target confirmation.</param>
    /// <returns>True if mana should be deducted now; false otherwise.</returns>
    public static bool CheckManaDeduction(Mobile caster, ISpell spell, int manaCost, bool atCastStart)
    {
        if (!SphereConfig.IsEnabled())
            return atCastStart; // Use ModernUO default (deduct at cast start)

        // Sphere-style edit: Deduct mana at target confirmation if configured
        if (SphereConfig.TargetManaDeduction)
        {
            if (!atCastStart)
            {
                SphereConfig.DebugLog($"{caster.Name} - Mana deducted at target confirmation (Sphere mode)");
                return true; // Deduct at target confirmation
            }

            return false; // Don't deduct at cast start
        }

        return atCastStart; // Default behavior
    }

    /// <summary>
    /// Gets the minimum cast delay according to Sphere 0.51a rules.
    /// Sphere-style edit: Uses configurable minimum cast delay.
    /// </summary>
    /// <param name="caster">The casting mobile.</param>
    /// <param name="spell">The spell being cast.</param>
    /// <param name="defaultMinimum">Default minimum from spell.</param>
    /// <returns>The adjusted minimum cast delay.</returns>
    public static TimeSpan GetMinimumCastDelay(Mobile caster, ISpell spell, TimeSpan defaultMinimum)
    {
        if (!SphereConfig.IsEnabled())
            return defaultMinimum;

        // Sphere-style edit: Use Sphere minimum if configured
        var sphereMinimum = TimeSpan.FromSeconds(SphereConfig.MinimumCastDelay);

        SphereConfig.DebugLog($"{caster.Name} - Minimum cast delay: {sphereMinimum.TotalSeconds}s (Sphere mode)");

        return sphereMinimum;
    }

    /// <summary>
    /// Processes spell effects according to Sphere 0.51a rules.
    /// </summary>
    /// <param name="caster">The caster.</param>
    /// <param name="target">The target.</param>
    /// <param name="spell">The spell.</param>
    public static void ProcessSpellEffects(Mobile caster, Mobile target, ISpell spell)
    {
        if (!SphereConfig.IsEnabled())
            return;

        SphereConfig.DebugLog($"{caster.Name} - Spell cast on {target?.Name ?? "unknown"}: {spell.GetType().Name}");
    }
}
