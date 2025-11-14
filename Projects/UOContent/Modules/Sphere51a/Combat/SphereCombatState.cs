/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: SphereCombatState.cs
 *
 * Description: Combat state management for individual mobiles.
 *              Tracks independent timers and action states for Sphere mechanics.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Runtime.CompilerServices;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Modules.Sphere51a.Configuration;
using Server.Spells;

namespace Server.Modules.Sphere51a.Combat;

/// <summary>
/// Combat state for a single mobile in Sphere 51a system.
/// Manages independent timers and action states.
/// </summary>
public class SphereCombatState
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(SphereCombatState));

    // Thread-safe storage for combat states
    private static readonly ConditionalWeakTable<Mobile, SphereCombatState> _combatStates = new();

    /// <summary>
    /// Gets or creates the combat state for a mobile.
    /// </summary>
    public static SphereCombatState GetOrCreate(Mobile mobile)
    {
        if (mobile == null)
            return null;

        return _combatStates.GetValue(mobile, _ => new SphereCombatState(mobile));
    }

    /// <summary>
    /// Removes the combat state for a mobile.
    /// </summary>
    public static void Remove(Mobile mobile)
    {
        if (mobile != null)
        {
            _combatStates.Remove(mobile);
        }
    }

    // Instance fields
    private readonly Mobile _mobile;

    // Independent timers (in ticks)
    private long _nextSwingTime;
    private long _nextSpellTime;
    private long _nextBandageTime;
    private long _nextWandTime;

    // Action states
    private bool _hasPendingSwing;
    private bool _isCasting;
    private bool _isInCastDelay;
    private bool _isBandaging;
    private bool _isUsingWand;

    // Cast delay timing (separate from spell recovery)
    private long _castDelayEndTime;

    // Current actions
    private Spell _currentSpell;
    private BaseWand _currentWand;
    private Mobile _bandagePatient;

    /// <summary>
    /// Creates a new combat state for the specified mobile.
    /// </summary>
    private SphereCombatState(Mobile mobile)
    {
        _mobile = mobile;
        ClearAllTimers();
    }

    #region Timer Management

    /// <summary>
    /// Clears all combat timers.
    /// </summary>
    public void ClearAllTimers()
    {
        _nextSwingTime = 0;
        _nextSpellTime = 0;
        _nextBandageTime = 0;
        _nextWandTime = 0;

        _hasPendingSwing = false;
        _isCasting = false;
        _isInCastDelay = false;
        _isBandaging = false;
        _isUsingWand = false;

        _currentSpell = null;
        _currentWand = null;
        _bandagePatient = null;
    }

    /// <summary>
    /// Gets the current server time in ticks.
    /// </summary>
    private static long Now => Server.Core.TickCount;

    #endregion

    #region Swing State

    /// <summary>
    /// Checks if swinging is allowed.
    /// </summary>
    public bool CanSwing()
    {
        if (!SphereConfiguration.IndependentTimers)
            return _mobile.NextCombatTime <= Now; // Use ModernUO timer

        return _nextSwingTime <= Now;
    }

    /// <summary>
    /// Gets whether there's a pending swing.
    /// </summary>
    public bool HasPendingSwing => _hasPendingSwing;

    /// <summary>
    /// Begins a swing action.
    /// </summary>
    public void BeginSwing()
    {
        _hasPendingSwing = true;
        SphereConfiguration.DebugLog($"{_mobile.Name} - Swing begun");
    }

    /// <summary>
    /// Cancels any pending swing.
    /// </summary>
    public void CancelSwing(string reason = null)
    {
        if (_hasPendingSwing)
        {
            _hasPendingSwing = false;
            SphereConfiguration.LogCancellation(_mobile, "Weapon swing", reason ?? "Cancelled");
        }
    }

    /// <summary>
    /// Sets the next swing time.
    /// </summary>
    public void SetNextSwingTime(TimeSpan delay)
    {
        if (SphereConfiguration.IndependentTimers)
        {
            _nextSwingTime = Now + (long)delay.TotalMilliseconds;
        }
        else
        {
            // Use ModernUO's shared timer
            _mobile.NextCombatTime = Now + (long)delay.TotalMilliseconds;
        }

        _hasPendingSwing = false;
    }

    #endregion

    #region Spell Casting State

    /// <summary>
    /// Checks if spell casting is allowed.
    /// </summary>
    public bool CanCast()
    {
        if (!SphereConfiguration.IndependentTimers)
            return _mobile.NextSpellTime <= Now; // Use ModernUO timer

        return _nextSpellTime <= Now;
    }

    /// <summary>
    /// Gets whether the mobile is currently casting.
    /// </summary>
    public bool IsCasting => _isCasting;

    /// <summary>
    /// Gets whether the mobile is in cast delay phase.
    /// </summary>
    public bool IsInCastDelay => _isInCastDelay;

    /// <summary>
    /// Begins a spell cast.
    /// </summary>
    public void BeginSpellCast(Spell spell)
    {
        _isCasting = true;
        _currentSpell = spell;
        SphereConfiguration.DebugLog($"{_mobile.Name} - Spell cast begun: {spell?.GetType().Name}");
    }

    /// <summary>
    /// Moves to cast delay phase.
    /// </summary>
    public void EnterCastDelay()
    {
        _isCasting = false;
        _isInCastDelay = true;
        SphereConfiguration.DebugLog($"{_mobile.Name} - Entered cast delay");
    }

    /// <summary>
    /// Ends a spell cast.
    /// </summary>
    public void EndSpellCast()
    {
        _isCasting = false;
        _isInCastDelay = false;
        _currentSpell = null;
        SphereConfiguration.DebugLog($"{_mobile.Name} - Spell cast ended");
    }

    /// <summary>
    /// Cancels any active spell cast.
    /// </summary>
    public void CancelSpell(string reason = null)
    {
        if (_isCasting || _isInCastDelay)
        {
            _isCasting = false;
            _isInCastDelay = false;
            var spellName = _currentSpell?.GetType().Name ?? "Unknown";
            _currentSpell = null;

            SphereConfiguration.LogCancellation(_mobile, "Spell cast", $"{reason ?? "Cancelled"} ({spellName})");
        }
    }

    /// <summary>
    /// Sets the next spell time.
    /// </summary>
    public void SetNextSpellTime(TimeSpan delay)
    {
        if (SphereConfiguration.IndependentTimers)
        {
            _nextSpellTime = Now + (long)delay.TotalMilliseconds;
        }
        else
        {
            // Use ModernUO's shared timer
            _mobile.NextSpellTime = Now + (long)delay.TotalMilliseconds;
        }
    }

    /// <summary>
    /// Sets the cast delay timer (separate from spell recovery).
    /// </summary>
    public void SetCastDelay(TimeSpan delay)
    {
        _castDelayEndTime = Now + (long)delay.TotalMilliseconds;
        SphereConfiguration.DebugLog($"{_mobile.Name} - Cast delay set: {delay.TotalMilliseconds}ms");
    }

    /// <summary>
    /// Gets the remaining cast delay in milliseconds.
    /// </summary>
    public long GetRemainingCastDelay() => Math.Max(0, _castDelayEndTime - Now);

    /// <summary>
    /// Checks if the mobile can swing during cast delay (Sphere51a behavior).
    /// </summary>
    public bool CanSwingDuringCast() => _castDelayEndTime <= Now;

    #endregion

    #region Bandage State

    /// <summary>
    /// Checks if bandaging is allowed.
    /// </summary>
    public bool CanBandage()
    {
        if (!SphereConfiguration.IndependentTimers)
            return _mobile.NextSkillTime <= Now; // Use ModernUO timer

        return _nextBandageTime <= Now;
    }

    /// <summary>
    /// Begins a bandage action.
    /// </summary>
    public void BeginBandage(Mobile patient)
    {
        _isBandaging = true;
        _bandagePatient = patient;
        SphereConfiguration.DebugLog($"{_mobile.Name} - Bandage begun on {patient?.Name ?? "self"}");
    }

    /// <summary>
    /// Ends a bandage action.
    /// </summary>
    public void EndBandage()
    {
        _isBandaging = false;
        _bandagePatient = null;
        SphereConfiguration.DebugLog($"{_mobile.Name} - Bandage ended");
    }

    /// <summary>
    /// Sets the next bandage time.
    /// </summary>
    public void SetNextBandageTime(TimeSpan delay)
    {
        if (SphereConfiguration.IndependentTimers)
        {
            _nextBandageTime = Now + (long)delay.TotalMilliseconds;
        }
        else
        {
            // Use ModernUO's shared timer
            _mobile.NextSkillTime = Now + (long)delay.TotalMilliseconds;
        }
    }

    #endregion

    #region Wand State

    /// <summary>
    /// Checks if wand usage is allowed.
    /// </summary>
    public bool CanUseWand()
    {
        if (!SphereConfiguration.IndependentTimers)
            return _mobile.NextSkillTime <= Now; // Use ModernUO timer

        return _nextWandTime <= Now;
    }

    /// <summary>
    /// Begins a wand action.
    /// </summary>
    public void BeginWandUse(BaseWand wand, Spell spell)
    {
        _isUsingWand = true;
        _currentWand = wand;
        SphereConfiguration.DebugLog($"{_mobile.Name} - Wand use begun: {wand?.GetType().Name}");
    }

    /// <summary>
    /// Ends a wand action.
    /// </summary>
    public void EndWandUse()
    {
        _isUsingWand = false;
        _currentWand = null;
        SphereConfiguration.DebugLog($"{_mobile.Name} - Wand use ended");
    }

    /// <summary>
    /// Sets the next wand time.
    /// </summary>
    public void SetNextWandTime(TimeSpan delay)
    {
        if (SphereConfiguration.IndependentTimers)
        {
            _nextWandTime = Now + (long)delay.TotalMilliseconds;
        }
        else
        {
            // Use ModernUO's shared timer
            _mobile.NextSkillTime = Now + (long)delay.TotalMilliseconds;
        }
    }

    #endregion

    #region State Queries

    /// <summary>
    /// Checks if any action is currently in progress.
    /// </summary>
    public bool IsBusy => _isCasting || _isInCastDelay || _isBandaging || _isUsingWand;

    /// <summary>
    /// Gets a summary of the current combat state.
    /// </summary>
    public string GetStateSummary()
    {
        return $"Sphere State: Cast={_isCasting}, Delay={_isInCastDelay}, Bandage={_isBandaging}, Wand={_isUsingWand}, Swing={_hasPendingSwing}";
    }

    #endregion

    #region Timer Queries

    /// <summary>
    /// Gets the remaining swing delay in milliseconds.
    /// </summary>
    public long GetRemainingSwingDelay()
    {
        if (!SphereConfiguration.IndependentTimers)
            return Math.Max(0, _mobile.NextCombatTime - Now);

        return Math.Max(0, _nextSwingTime - Now);
    }

    /// <summary>
    /// Gets the remaining spell delay in milliseconds.
    /// </summary>
    public long GetRemainingSpellDelay()
    {
        if (!SphereConfiguration.IndependentTimers)
            return Math.Max(0, _mobile.NextSpellTime - Now);

        return Math.Max(0, _nextSpellTime - Now);
    }

    /// <summary>
    /// Gets the remaining bandage delay in milliseconds.
    /// </summary>
    public long GetRemainingBandageDelay()
    {
        if (!SphereConfiguration.IndependentTimers)
            return Math.Max(0, _mobile.NextSkillTime - Now);

        return Math.Max(0, _nextBandageTime - Now);
    }

    /// <summary>
    /// Gets the remaining wand delay in milliseconds.
    /// </summary>
    public long GetRemainingWandDelay()
    {
        if (!SphereConfiguration.IndependentTimers)
            return Math.Max(0, _mobile.NextSkillTime - Now);

        return Math.Max(0, _nextWandTime - Now);
    }

    #endregion
}
