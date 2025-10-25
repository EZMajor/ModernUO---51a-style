/*************************************************************************
 * ModernUO - Sphere 0.51a Combat System
 * File: SphereCombatState.cs
 *
 * Description: State management for Sphere 0.51a-style combat mechanics.
 *              Tracks independent timers and action states for combat,
 *              spellcasting, bandaging, and wand usage.
 *
 * Reference: Sphere0.51aCombatSystem.md - Section 2.4 System Rules
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using Server.Mobiles;
using Server.Spells;

namespace Server.Systems.Combat.SphereStyle;

/// <summary>
/// Manages Sphere 0.51a-style combat state for a mobile.
/// Provides independent timer tracking and action cancellation logic.
/// </summary>
/// <remarks>
/// Sphere 0.51a operates with fully independent timers:
/// - NextSwingTime: Weapon attack timer
/// - NextSpellTime: Spell casting timer
/// - NextBandageTime: Bandaging timer
/// - NextWandTime: Wand usage timer
///
/// These timers do NOT share recovery delays and operate independently.
/// Action cancellation follows specific hierarchical rules.
/// </remarks>
public class SphereCombatState
{
    private readonly Mobile _mobile;

    /// <summary>
    /// Next allowed weapon swing time (in Core.TickCount milliseconds).
    /// </summary>
    public long NextSwingTime { get; set; }

    /// <summary>
    /// Next allowed spell cast time (in Core.TickCount milliseconds).
    /// </summary>
    public long NextSpellTime { get; set; }

    /// <summary>
    /// Next allowed bandage use time (in Core.TickCount milliseconds).
    /// </summary>
    public long NextBandageTime { get; set; }

    /// <summary>
    /// Next allowed wand use time (in Core.TickCount milliseconds).
    /// </summary>
    public long NextWandTime { get; set; }

    /// <summary>
    /// Indicates if the mobile is currently casting a spell.
    /// </summary>
    public bool IsCasting { get; set; }

    /// <summary>
    /// Indicates if the mobile is currently in cast delay (post-target, pre-effect).
    /// </summary>
    public bool IsInCastDelay { get; set; }

    /// <summary>
    /// Indicates if the mobile has a pending weapon swing.
    /// </summary>
    public bool HasPendingSwing { get; set; }

    /// <summary>
    /// Indicates if the mobile is currently bandaging.
    /// </summary>
    public bool IsBandaging { get; set; }

    /// <summary>
    /// Reference to the currently casting spell (if any).
    /// </summary>
    public ISpell? CurrentSpell { get; set; }

    /// <summary>
    /// Timestamp when the current spell cast started.
    /// </summary>
    public long SpellCastStartTime { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SphereCombatState"/> class.
    /// </summary>
    /// <param name="mobile">The mobile this state belongs to.</param>
    public SphereCombatState(Mobile mobile)
    {
        _mobile = mobile ?? throw new ArgumentNullException(nameof(mobile));

        // Initialize timers to allow immediate action
        NextSwingTime = 0;
        NextSpellTime = 0;
        NextBandageTime = 0;
        NextWandTime = 0;
    }

    #region Timer Checks

    /// <summary>
    /// Checks if the mobile can perform a weapon swing.
    /// </summary>
    public bool CanSwing()
    {
        if (!SphereConfig.IsEnabled())
            return true; // Default to ModernUO behavior

        // Check timer
        if (Core.TickCount - NextSwingTime < 0)
        {
            SphereConfig.DebugLog($"{_mobile.Name} - Swing blocked: Timer not ready");
            return false;
        }

        // Sphere-style edit: Swings disabled during casting
        if (SphereConfig.DisableSwingDuringCast && IsCasting)
        {
            SphereConfig.DebugLog($"{_mobile.Name} - Swing blocked: Currently casting");
            return false;
        }

        // Sphere-style edit: Swings disabled during cast delay
        if (SphereConfig.DisableSwingDuringCastDelay && IsInCastDelay)
        {
            SphereConfig.DebugLog($"{_mobile.Name} - Swing blocked: In cast delay");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if the mobile can cast a spell.
    /// </summary>
    public bool CanCast()
    {
        if (!SphereConfig.IsEnabled())
            return true; // Default to ModernUO behavior

        // Check timer
        if (Core.TickCount - NextSpellTime < 0)
        {
            SphereConfig.DebugLog($"{_mobile.Name} - Cast blocked: Timer not ready");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if the mobile can use a bandage.
    /// </summary>
    public bool CanBandage()
    {
        if (!SphereConfig.IsEnabled())
            return true; // Default to ModernUO behavior

        // Check timer
        if (Core.TickCount - NextBandageTime < 0)
        {
            SphereConfig.DebugLog($"{_mobile.Name} - Bandage blocked: Timer not ready");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if the mobile can use a wand.
    /// </summary>
    public bool CanUseWand()
    {
        if (!SphereConfig.IsEnabled())
            return true; // Default to ModernUO behavior

        // Check timer
        if (Core.TickCount - NextWandTime < 0)
        {
            SphereConfig.DebugLog($"{_mobile.Name} - Wand blocked: Timer not ready");
            return false;
        }

        return true;
    }

    #endregion

    #region Action Cancellation (Sphere 0.51a Hierarchy)

    /// <summary>
    /// Initiates a spell cast, cancelling swing if configured.
    /// Sphere-style edit: Starting spell cancels pending weapon swing.
    /// </summary>
    public void BeginSpellCast(ISpell spell)
    {
        if (!SphereConfig.IsEnabled())
            return;

        // Sphere-style edit: Cancel pending swing
        if (SphereConfig.SpellCancelSwing && HasPendingSwing)
        {
            CancelSwing("Spell cast started");
        }

        IsCasting = true;
        CurrentSpell = spell;
        SpellCastStartTime = Core.TickCount;

        SphereConfig.DebugLog($"{_mobile.Name} - Spell cast begun");
    }

    /// <summary>
    /// Completes or cancels a spell cast.
    /// </summary>
    public void EndSpellCast(bool completed)
    {
        if (!SphereConfig.IsEnabled())
            return;

        IsCasting = false;
        IsInCastDelay = false;
        CurrentSpell = null;

        SphereConfig.DebugLog($"{_mobile.Name} - Spell cast ended (completed: {completed})");

        // Sphere-style edit: No post-cast recovery delay
        if (SphereConfig.RemovePostCastRecovery && completed)
        {
            // Do not set NextSpellTime - allow immediate next cast
            SphereConfig.DebugLog($"{_mobile.Name} - Post-cast recovery removed");
        }
    }

    /// <summary>
    /// Transitions spell from casting to cast delay phase.
    /// </summary>
    public void EnterCastDelay()
    {
        if (!SphereConfig.IsEnabled())
            return;

        IsCasting = false;
        IsInCastDelay = true;

        SphereConfig.DebugLog($"{_mobile.Name} - Entered cast delay phase");
    }

    /// <summary>
    /// Initiates a weapon swing, cancelling spell if configured.
    /// Sphere-style edit: Beginning attack cancels active spell cast.
    /// </summary>
    public void BeginSwing()
    {
        if (!SphereConfig.IsEnabled())
            return;

        // Sphere-style edit: Cancel active spell cast
        if (SphereConfig.SwingCancelSpell && IsCasting && CurrentSpell != null)
        {
            CancelSpell("Weapon swing started");
        }

        HasPendingSwing = true;

        SphereConfig.DebugLog($"{_mobile.Name} - Swing begun");
    }

    /// <summary>
    /// Completes or cancels a weapon swing.
    /// </summary>
    public void EndSwing()
    {
        if (!SphereConfig.IsEnabled())
            return;

        HasPendingSwing = false;

        SphereConfig.DebugLog($"{_mobile.Name} - Swing ended");
    }

    /// <summary>
    /// Cancels the current spell cast.
    /// </summary>
    public void CancelSpell(string reason)
    {
        if (!SphereConfig.IsEnabled())
            return;

        if (CurrentSpell != null)
        {
            SphereConfig.LogCancellation(_mobile, "Spell cast", reason);

            // Disturb the spell
            if (CurrentSpell is Spell spell)
            {
                spell.Disturb(DisturbType.UseRequest, false, false);
            }

            EndSpellCast(false);
        }
    }

    /// <summary>
    /// Cancels the pending weapon swing.
    /// </summary>
    public void CancelSwing(string reason)
    {
        if (!SphereConfig.IsEnabled())
            return;

        if (HasPendingSwing)
        {
            SphereConfig.LogCancellation(_mobile, "Weapon swing", reason);

            // Sphere-style edit: Reset swing timer on interrupt
            if (SphereConfig.ResetSwingOnInterrupt)
            {
                HasPendingSwing = false;
                SphereConfig.DebugLog($"{_mobile.Name} - Swing timer reset");
            }
        }
    }

    /// <summary>
    /// Initiates bandage use, cancelling swing and cast if configured.
    /// Sphere-style edit: Bandage use cancels both swing and cast.
    /// </summary>
    public void BeginBandage()
    {
        if (!SphereConfig.IsEnabled())
            return;

        // Sphere-style edit: Cancel both swing and cast
        if (SphereConfig.BandageCancelActions)
        {
            CancelSwing("Bandage use started");
            CancelSpell("Bandage use started");
        }

        IsBandaging = true;

        SphereConfig.DebugLog($"{_mobile.Name} - Bandaging begun");
    }

    /// <summary>
    /// Completes or cancels bandage use.
    /// </summary>
    public void EndBandage()
    {
        if (!SphereConfig.IsEnabled())
            return;

        IsBandaging = false;

        SphereConfig.DebugLog($"{_mobile.Name} - Bandaging ended");
    }

    /// <summary>
    /// Initiates wand use, cancelling swing and cast if configured.
    /// Sphere-style edit: Wand use cancels both swing and cast.
    /// </summary>
    public void BeginWandUse()
    {
        if (!SphereConfig.IsEnabled())
            return;

        // Sphere-style edit: Cancel both swing and cast
        if (SphereConfig.WandCancelActions)
        {
            CancelSwing("Wand use started");
            CancelSpell("Wand use started");
        }

        SphereConfig.DebugLog($"{_mobile.Name} - Wand use begun");
    }

    #endregion

    #region Timer Manipulation

    /// <summary>
    /// Sets the next allowed swing time.
    /// </summary>
    public void SetNextSwingTime(TimeSpan delay)
    {
        if (!SphereConfig.IsEnabled())
            return;

        long oldValue = NextSwingTime;
        NextSwingTime = Core.TickCount + (long)delay.TotalMilliseconds;

        SphereConfig.LogTimerChange(_mobile, "NextSwingTime", oldValue, NextSwingTime);
    }

    /// <summary>
    /// Sets the next allowed spell cast time.
    /// </summary>
    public void SetNextSpellTime(TimeSpan delay)
    {
        if (!SphereConfig.IsEnabled())
            return;

        long oldValue = NextSpellTime;
        NextSpellTime = Core.TickCount + (long)delay.TotalMilliseconds;

        SphereConfig.LogTimerChange(_mobile, "NextSpellTime", oldValue, NextSpellTime);
    }

    /// <summary>
    /// Sets the next allowed bandage time.
    /// </summary>
    public void SetNextBandageTime(TimeSpan delay)
    {
        if (!SphereConfig.IsEnabled())
            return;

        long oldValue = NextBandageTime;
        NextBandageTime = Core.TickCount + (long)delay.TotalMilliseconds;

        SphereConfig.LogTimerChange(_mobile, "NextBandageTime", oldValue, NextBandageTime);
    }

    /// <summary>
    /// Sets the next allowed wand use time.
    /// </summary>
    public void SetNextWandTime(TimeSpan delay)
    {
        if (!SphereConfig.IsEnabled())
            return;

        long oldValue = NextWandTime;
        NextWandTime = Core.TickCount + (long)delay.TotalMilliseconds;

        SphereConfig.LogTimerChange(_mobile, "NextWandTime", oldValue, NextWandTime);
    }

    /// <summary>
    /// Clears all timers (allows immediate action).
    /// </summary>
    public void ClearAllTimers()
    {
        if (!SphereConfig.IsEnabled())
            return;

        NextSwingTime = 0;
        NextSpellTime = 0;
        NextBandageTime = 0;
        NextWandTime = 0;

        SphereConfig.DebugLog($"{_mobile.Name} - All timers cleared");
    }

    #endregion
}
