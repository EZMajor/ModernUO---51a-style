/*************************************************************************
 * ModernUO - Sphere 0.51a Combat System
 * File: MobileExtensions.cs
 *
 * Description: Extension methods for Mobile class to integrate Sphere 0.51a
 *              combat state management. Provides convenient access to
 *              Sphere-style combat state tracking.
 *
 * Reference: Sphere0.51aCombatSystem.md
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Server.Systems.Combat.SphereStyle;

/// <summary>
/// Extension methods for Mobile class to support Sphere 0.51a combat mechanics.
/// </summary>
public static class MobileExtensions
{
    // Sphere-style edit: Storage for Sphere combat state per mobile
    private static readonly ConditionalWeakTable<Mobile, SphereCombatState> _sphereStates = new();

    /// <summary>
    /// Gets the Sphere combat state for this mobile.
    /// Creates a new state if one doesn't exist.
    /// </summary>
    /// <param name="mobile">The mobile to get state for.</param>
    /// <returns>The Sphere combat state instance.</returns>
    public static SphereCombatState GetSphereState(this Mobile mobile)
    {
        if (mobile == null)
            return null!;

        return _sphereStates.GetValue(mobile, m => new SphereCombatState(m));
    }

    /// <summary>
    /// Checks if this mobile has an active Sphere combat state.
    /// </summary>
    public static bool HasSphereState(this Mobile mobile)
    {
        return mobile != null && _sphereStates.TryGetValue(mobile, out _);
    }

    #region Sphere Timer Checks (Convenience Methods)

    /// <summary>
    /// Checks if the mobile can perform a weapon swing (Sphere rules).
    /// </summary>
    public static bool SphereCanSwing(this Mobile mobile)
    {
        if (!SphereConfig.IsEnabled())
            return true;

        return mobile?.GetSphereState()?.CanSwing() ?? true;
    }

    /// <summary>
    /// Checks if the mobile can cast a spell (Sphere rules).
    /// </summary>
    public static bool SphereCanCast(this Mobile mobile)
    {
        if (!SphereConfig.IsEnabled())
            return true;

        return mobile?.GetSphereState()?.CanCast() ?? true;
    }

    /// <summary>
    /// Checks if the mobile can use a bandage (Sphere rules).
    /// </summary>
    public static bool SphereCanBandage(this Mobile mobile)
    {
        if (!SphereConfig.IsEnabled())
            return true;

        return mobile?.GetSphereState()?.CanBandage() ?? true;
    }

    /// <summary>
    /// Checks if the mobile can use a wand (Sphere rules).
    /// </summary>
    public static bool SphereCanUseWand(this Mobile mobile)
    {
        if (!SphereConfig.IsEnabled())
            return true;

        return mobile?.GetSphereState()?.CanUseWand() ?? true;
    }

    #endregion

    #region Sphere Action Management (Convenience Methods)

    /// <summary>
    /// Begins a spell cast using Sphere rules.
    /// </summary>
    public static void SphereBeginSpellCast(this Mobile mobile, Spells.ISpell spell)
    {
        if (!SphereConfig.IsEnabled() || mobile == null)
            return;

        mobile.GetSphereState().BeginSpellCast(spell);
    }

    /// <summary>
    /// Ends a spell cast using Sphere rules.
    /// </summary>
    public static void SphereEndSpellCast(this Mobile mobile, bool completed)
    {
        if (!SphereConfig.IsEnabled() || mobile == null)
            return;

        mobile.GetSphereState().EndSpellCast(completed);
    }

    /// <summary>
    /// Enters cast delay phase using Sphere rules.
    /// </summary>
    public static void SphereEnterCastDelay(this Mobile mobile)
    {
        if (!SphereConfig.IsEnabled() || mobile == null)
            return;

        mobile.GetSphereState().EnterCastDelay();
    }

    /// <summary>
    /// Begins a weapon swing using Sphere rules.
    /// </summary>
    public static void SphereBeginSwing(this Mobile mobile)
    {
        if (!SphereConfig.IsEnabled() || mobile == null)
            return;

        mobile.GetSphereState().BeginSwing();
    }

    /// <summary>
    /// Ends a weapon swing using Sphere rules.
    /// </summary>
    public static void SphereEndSwing(this Mobile mobile)
    {
        if (!SphereConfig.IsEnabled() || mobile == null)
            return;

        mobile.GetSphereState().EndSwing();
    }

    /// <summary>
    /// Cancels the current spell using Sphere rules.
    /// </summary>
    public static void SphereCancelSpell(this Mobile mobile, string reason)
    {
        if (!SphereConfig.IsEnabled() || mobile == null)
            return;

        mobile.GetSphereState().CancelSpell(reason);
    }

    /// <summary>
    /// Cancels the pending swing using Sphere rules.
    /// </summary>
    public static void SphereCancelSwing(this Mobile mobile, string reason)
    {
        if (!SphereConfig.IsEnabled() || mobile == null)
            return;

        mobile.GetSphereState().CancelSwing(reason);
    }

    /// <summary>
    /// Begins bandaging using Sphere rules.
    /// </summary>
    public static void SphereBeginBandage(this Mobile mobile)
    {
        if (!SphereConfig.IsEnabled() || mobile == null)
            return;

        mobile.GetSphereState().BeginBandage();
    }

    /// <summary>
    /// Ends bandaging using Sphere rules.
    /// </summary>
    public static void SphereEndBandage(this Mobile mobile)
    {
        if (!SphereConfig.IsEnabled() || mobile == null)
            return;

        mobile.GetSphereState().EndBandage();
    }

    /// <summary>
    /// Begins wand use using Sphere rules.
    /// </summary>
    public static void SphereBeginWandUse(this Mobile mobile)
    {
        if (!SphereConfig.IsEnabled() || mobile == null)
            return;

        mobile.GetSphereState().BeginWandUse();
    }

    #endregion

    #region Sphere Timer Manipulation (Convenience Methods)

    /// <summary>
    /// Sets the next allowed swing time using Sphere rules.
    /// </summary>
    public static void SphereSetNextSwingTime(this Mobile mobile, System.TimeSpan delay)
    {
        if (!SphereConfig.IsEnabled() || mobile == null)
            return;

        mobile.GetSphereState().SetNextSwingTime(delay);
    }

    /// <summary>
    /// Sets the next allowed spell cast time using Sphere rules.
    /// </summary>
    public static void SphereSetNextSpellTime(this Mobile mobile, System.TimeSpan delay)
    {
        if (!SphereConfig.IsEnabled() || mobile == null)
            return;

        mobile.GetSphereState().SetNextSpellTime(delay);
    }

    /// <summary>
    /// Sets the next allowed bandage time using Sphere rules.
    /// </summary>
    public static void SphereSetNextBandageTime(this Mobile mobile, System.TimeSpan delay)
    {
        if (!SphereConfig.IsEnabled() || mobile == null)
            return;

        mobile.GetSphereState().SetNextBandageTime(delay);
    }

    /// <summary>
    /// Sets the next allowed wand use time using Sphere rules.
    /// </summary>
    public static void SphereSetNextWandTime(this Mobile mobile, System.TimeSpan delay)
    {
        if (!SphereConfig.IsEnabled() || mobile == null)
            return;

        mobile.GetSphereState().SetNextWandTime(delay);
    }

    /// <summary>
    /// Clears all Sphere timers for this mobile.
    /// </summary>
    public static void SphereClearAllTimers(this Mobile mobile)
    {
        if (!SphereConfig.IsEnabled() || mobile == null)
            return;

        mobile.GetSphereState().ClearAllTimers();
    }

    #endregion

    #region Sphere State Queries

    /// <summary>
    /// Checks if mobile is currently casting (Sphere state).
    /// </summary>
    public static bool SphereIsCasting(this Mobile mobile)
    {
        if (!SphereConfig.IsEnabled() || mobile == null)
            return false;

        return mobile.GetSphereState().IsCasting;
    }

    /// <summary>
    /// Checks if mobile is in cast delay phase (Sphere state).
    /// </summary>
    public static bool SphereIsInCastDelay(this Mobile mobile)
    {
        if (!SphereConfig.IsEnabled() || mobile == null)
            return false;

        return mobile.GetSphereState().IsInCastDelay;
    }

    /// <summary>
    /// Checks if mobile has a pending swing (Sphere state).
    /// </summary>
    public static bool SphereHasPendingSwing(this Mobile mobile)
    {
        if (!SphereConfig.IsEnabled() || mobile == null)
            return false;

        return mobile.GetSphereState().HasPendingSwing;
    }

    /// <summary>
    /// Checks if mobile is currently bandaging (Sphere state).
    /// </summary>
    public static bool SphereIsBandaging(this Mobile mobile)
    {
        if (!SphereConfig.IsEnabled() || mobile == null)
            return false;

        return mobile.GetSphereState().IsBandaging;
    }

    #endregion
}
