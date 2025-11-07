/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: MobileExtensions.cs
 *
 * Description: Extension methods for Mobile class to support Sphere 51a mechanics.
 *              Provides convenient access to combat state and Sphere-specific operations.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using Server.Items;
using Server.Mobiles;
using Server.Modules.Sphere51a.Combat;
using Server.Modules.Sphere51a.Configuration;
using Server.Spells;

namespace Server.Modules.Sphere51a.Extensions;

/// <summary>
/// Extension methods for Mobile class to support Sphere 51a combat mechanics.
/// </summary>
public static class MobileExtensions
{
    #region Combat State Access

    /// <summary>
    /// Gets or creates the Sphere combat state for this mobile.
    /// </summary>
    public static SphereCombatState SphereGetCombatState(this Mobile mobile)
    {
        if (mobile == null)
            return null;

        return SphereCombatState.GetOrCreate(mobile);
    }

    /// <summary>
    /// Initializes combat state for this mobile.
    /// </summary>
    public static void SphereInitializeCombatState(this Mobile mobile)
    {
        if (mobile == null || !SphereConfiguration.Enabled)
            return;

        SphereCombatState.GetOrCreate(mobile);
    }

    /// <summary>
    /// Clears combat state for this mobile.
    /// </summary>
    public static void SphereClearCombatState(this Mobile mobile)
    {
        if (mobile == null)
            return;

        SphereCombatState.Remove(mobile);
    }

    #endregion

    #region Swing Validation & Control

    /// <summary>
    /// Checks if this mobile can perform a weapon swing according to Sphere rules.
    /// </summary>
    public static bool SphereCanSwing(this Mobile mobile)
    {
        if (mobile == null || !SphereConfiguration.Enabled)
            return true; // Default ModernUO behavior

        var state = mobile.SphereGetCombatState();
        return state?.CanSwing() ?? true;
    }

    /// <summary>
    /// Checks if this mobile has a pending swing.
    /// </summary>
    public static bool SphereHasPendingSwing(this Mobile mobile)
    {
        if (mobile == null || !SphereConfiguration.Enabled)
            return false;

        var state = mobile.SphereGetCombatState();
        return state?.HasPendingSwing ?? false;
    }

    /// <summary>
    /// Begins tracking a weapon swing for this mobile.
    /// </summary>
    public static void SphereBeginSwing(this Mobile mobile)
    {
        if (mobile == null || !SphereConfiguration.Enabled)
            return;

        var state = mobile.SphereGetCombatState();
        state?.BeginSwing();
    }

    /// <summary>
    /// Cancels any pending swing for this mobile.
    /// </summary>
    public static void SphereCancelSwing(this Mobile mobile, string reason = null)
    {
        if (mobile == null || !SphereConfiguration.Enabled)
            return;

        var state = mobile.SphereGetCombatState();
        state?.CancelSwing(reason);
    }

    /// <summary>
    /// Sets the next swing time for this mobile.
    /// </summary>
    public static void SphereSetNextSwingTime(this Mobile mobile, TimeSpan delay)
    {
        if (mobile == null || !SphereConfiguration.Enabled)
            return;

        var state = mobile.SphereGetCombatState();
        state?.SetNextSwingTime(delay);
    }

    #endregion

    #region Spell Casting Validation & Control

    /// <summary>
    /// Checks if this mobile can cast a spell according to Sphere rules.
    /// </summary>
    public static bool SphereCanCast(this Mobile mobile)
    {
        if (mobile == null || !SphereConfiguration.Enabled)
            return true; // Default ModernUO behavior

        var state = mobile.SphereGetCombatState();
        return state?.CanCast() ?? true;
    }

    /// <summary>
    /// Checks if this mobile is currently casting a spell.
    /// </summary>
    public static bool SphereIsCasting(this Mobile mobile)
    {
        if (mobile == null || !SphereConfiguration.Enabled)
            return false;

        var state = mobile.SphereGetCombatState();
        return state?.IsCasting ?? false;
    }

    /// <summary>
    /// Checks if this mobile is in cast delay phase.
    /// </summary>
    public static bool SphereIsInCastDelay(this Mobile mobile)
    {
        if (mobile == null || !SphereConfiguration.Enabled)
            return false;

        var state = mobile.SphereGetCombatState();
        return state?.IsInCastDelay ?? false;
    }

    /// <summary>
    /// Begins tracking a spell cast for this mobile.
    /// </summary>
    public static void SphereBeginSpellCast(this Mobile mobile, Spell spell)
    {
        if (mobile == null || !SphereConfiguration.Enabled)
            return;

        var state = mobile.SphereGetCombatState();
        state?.BeginSpellCast(spell);
    }

    /// <summary>
    /// Ends spell cast tracking for this mobile.
    /// </summary>
    public static void SphereEndSpellCast(this Mobile mobile)
    {
        if (mobile == null || !SphereConfiguration.Enabled)
            return;

        var state = mobile.SphereGetCombatState();
        state?.EndSpellCast();
    }

    /// <summary>
    /// Cancels any active spell cast for this mobile.
    /// </summary>
    public static void SphereCancelSpell(this Mobile mobile, string reason = null)
    {
        if (mobile == null || !SphereConfiguration.Enabled)
            return;

        var state = mobile.SphereGetCombatState();
        state?.CancelSpell(reason);
    }

    /// <summary>
    /// Sets the next spell time for this mobile.
    /// </summary>
    public static void SphereSetNextSpellTime(this Mobile mobile, TimeSpan delay)
    {
        if (mobile == null || !SphereConfiguration.Enabled)
            return;

        var state = mobile.SphereGetCombatState();
        state?.SetNextSpellTime(delay);
    }

    #endregion

    #region Bandage Validation & Control

    /// <summary>
    /// Checks if this mobile can use a bandage according to Sphere rules.
    /// </summary>
    public static bool SphereCanBandage(this Mobile mobile)
    {
        if (mobile == null || !SphereConfiguration.Enabled)
            return true; // Default ModernUO behavior

        var state = mobile.SphereGetCombatState();
        return state?.CanBandage() ?? true;
    }

    /// <summary>
    /// Begins tracking bandage use for this mobile.
    /// </summary>
    public static void SphereBeginBandage(this Mobile mobile, Mobile patient)
    {
        if (mobile == null || !SphereConfiguration.Enabled)
            return;

        var state = mobile.SphereGetCombatState();
        state?.BeginBandage(patient);
    }

    /// <summary>
    /// Ends bandage tracking for this mobile.
    /// </summary>
    public static void SphereEndBandage(this Mobile mobile)
    {
        if (mobile == null || !SphereConfiguration.Enabled)
            return;

        var state = mobile.SphereGetCombatState();
        state?.EndBandage();
    }

    /// <summary>
    /// Sets the next bandage time for this mobile.
    /// </summary>
    public static void SphereSetNextBandageTime(this Mobile mobile, TimeSpan delay)
    {
        if (mobile == null || !SphereConfiguration.Enabled)
            return;

        var state = mobile.SphereGetCombatState();
        state?.SetNextBandageTime(delay);
    }

    #endregion

    #region Wand Validation & Control

    /// <summary>
    /// Checks if this mobile can use a wand according to Sphere rules.
    /// </summary>
    public static bool SphereCanUseWand(this Mobile mobile)
    {
        if (mobile == null || !SphereConfiguration.Enabled)
            return true; // Default ModernUO behavior

        var state = mobile.SphereGetCombatState();
        return state?.CanUseWand() ?? true;
    }

    /// <summary>
    /// Begins tracking wand use for this mobile.
    /// </summary>
    public static void SphereBeginWandUse(this Mobile mobile, BaseWand wand, Spell spell)
    {
        if (mobile == null || !SphereConfiguration.Enabled)
            return;

        var state = mobile.SphereGetCombatState();
        state?.BeginWandUse(wand, spell);
    }

    /// <summary>
    /// Ends wand tracking for this mobile.
    /// </summary>
    public static void SphereEndWandUse(this Mobile mobile)
    {
        if (mobile == null || !SphereConfiguration.Enabled)
            return;

        var state = mobile.SphereGetCombatState();
        state?.EndWandUse();
    }

    /// <summary>
    /// Sets the next wand time for this mobile.
    /// </summary>
    public static void SphereSetNextWandTime(this Mobile mobile, TimeSpan delay)
    {
        if (mobile == null || !SphereConfiguration.Enabled)
            return;

        var state = mobile.SphereGetCombatState();
        state?.SetNextWandTime(delay);
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Gets the dexterity modifier for swing speed calculations.
    /// </summary>
    public static double SphereGetDexterityModifier(this Mobile mobile)
    {
        if (mobile == null)
            return 1.0;

        // Sphere formula: BaseSpeed / (Dexterity / 100)
        // This gives faster swings for higher dexterity
        var dex = Math.Max(mobile.Dex, 1); // Prevent division by zero
        return 100.0 / dex;
    }

    /// <summary>
    /// Gets the current Sphere combat state summary for debugging.
    /// </summary>
    public static string SphereGetCombatStateSummary(this Mobile mobile)
    {
        if (mobile == null)
            return "Mobile is null";

        var state = mobile.SphereGetCombatState();
        if (state == null)
            return "No Sphere combat state";

        return state.GetStateSummary();
    }

    #endregion
}
