/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: SphereEvents.cs
 *
 * Description: Event system for modular Sphere 51a combat mechanics.
 *              Provides hooks for combat actions without modifying core files.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using Server.Items;
using Server.Mobiles;
using Server.Spells;

namespace Server.Modules.Sphere51a.Events;

/// <summary>
/// Event arguments for weapon swing events.
/// </summary>
public class WeaponSwingEventArgs : EventArgs
{
    public Mobile Attacker { get; }
    public Mobile Defender { get; }
    public BaseWeapon Weapon { get; }
    public bool Cancelled { get; set; }
    public TimeSpan Delay { get; set; }

    public WeaponSwingEventArgs(Mobile attacker, Mobile defender, BaseWeapon weapon)
    {
        Attacker = attacker;
        Defender = defender;
        Weapon = weapon;
        Cancelled = false;
        Delay = TimeSpan.Zero;
    }
}

/// <summary>
/// Event arguments for spell casting events.
/// </summary>
public class SpellCastEventArgs : EventArgs
{
    public Mobile Caster { get; }
    public Spell Spell { get; }
    public bool Cancelled { get; set; }
    public bool BlocksMovement { get; set; }

    public SpellCastEventArgs(Mobile caster, Spell spell)
    {
        Caster = caster;
        Spell = spell;
        Cancelled = false;
        BlocksMovement = true;
    }
}

/// <summary>
/// Event arguments for bandage usage events.
/// </summary>
public class BandageUseEventArgs : EventArgs
{
    public Mobile Healer { get; }
    public Mobile Patient { get; }
    public bool Cancelled { get; set; }
    public TimeSpan Delay { get; set; }

    public BandageUseEventArgs(Mobile healer, Mobile patient)
    {
        Healer = healer;
        Patient = patient;
        Cancelled = false;
        Delay = TimeSpan.Zero;
    }
}

/// <summary>
/// Event arguments for wand usage events.
/// </summary>
public class WandUseEventArgs : EventArgs
{
    public Mobile User { get; }
    public BaseWand Wand { get; }
    public Spell Spell { get; }
    public bool Cancelled { get; set; }
    public TimeSpan Delay { get; set; }

    public WandUseEventArgs(Mobile user, BaseWand wand, Spell spell)
    {
        User = user;
        Wand = wand;
        Spell = spell;
        Cancelled = false;
        Delay = TimeSpan.Zero;
    }
}

/// <summary>
/// Central event hub for Sphere 51a combat mechanics.
/// Core files raise events here, Sphere module subscribes to handle them.
/// </summary>
public static class SphereEvents
{
    // Weapon combat events
    public static event EventHandler<WeaponSwingEventArgs> OnWeaponSwing;
    public static event EventHandler<WeaponSwingEventArgs> OnWeaponSwingComplete;

    // Spell casting events
    public static event EventHandler<SpellCastEventArgs> OnSpellCast;
    public static event EventHandler<SpellCastEventArgs> OnSpellCastBegin;
    public static event EventHandler<SpellCastEventArgs> OnSpellCastComplete;
    public static event Func<Mobile, Spell, bool> OnSpellBlocksMovement;

    // Bandage events
    public static event EventHandler<BandageUseEventArgs> OnBandageUse;
    public static event EventHandler<BandageUseEventArgs> OnBandageUseComplete;

    // Wand events
    public static event EventHandler<WandUseEventArgs> OnWandUse;
    public static event EventHandler<WandUseEventArgs> OnWandUseComplete;

    // Combat state events
    public static event EventHandler<Mobile> OnCombatEnter;
    public static event EventHandler<Mobile> OnCombatExit;

    // Spell reflection events
    public static event Action<Mobile, Mobile, string> OnSpellReflected;

    /// <summary>
    /// Raises the weapon swing event.
    /// </summary>
    public static WeaponSwingEventArgs RaiseWeaponSwing(Mobile attacker, Mobile defender, BaseWeapon weapon)
    {
        var args = new WeaponSwingEventArgs(attacker, defender, weapon);
        OnWeaponSwing?.Invoke(null, args);
        return args;
    }

    /// <summary>
    /// Raises the weapon swing complete event.
    /// </summary>
    public static void RaiseWeaponSwingComplete(Mobile attacker, Mobile defender, BaseWeapon weapon, TimeSpan delay)
    {
        var args = new WeaponSwingEventArgs(attacker, defender, weapon) { Delay = delay };
        OnWeaponSwingComplete?.Invoke(null, args);
    }

    /// <summary>
    /// Raises the spell cast event.
    /// </summary>
    public static void RaiseSpellCast(Mobile caster, Spell spell)
    {
        var args = new SpellCastEventArgs(caster, spell);
        OnSpellCast?.Invoke(null, args);
    }

    /// <summary>
    /// Raises the spell cast begin event.
    /// </summary>
    public static void RaiseSpellCastBegin(Mobile caster, Spell spell)
    {
        var args = new SpellCastEventArgs(caster, spell);
        OnSpellCastBegin?.Invoke(null, args);
    }

    /// <summary>
    /// Raises the spell cast complete event.
    /// </summary>
    public static void RaiseSpellCastComplete(Mobile caster, Spell spell)
    {
        var args = new SpellCastEventArgs(caster, spell);
        OnSpellCastComplete?.Invoke(null, args);
    }

    /// <summary>
    /// Queries if spell should block movement.
    /// </summary>
    public static bool QuerySpellBlocksMovement(Mobile caster, Spell spell)
    {
        if (OnSpellBlocksMovement == null)
            return true; // Default ModernUO behavior

        return OnSpellBlocksMovement(caster, spell);
    }

    /// <summary>
    /// Raises the bandage use event.
    /// </summary>
    public static void RaiseBandageUse(Mobile healer, Mobile patient)
    {
        var args = new BandageUseEventArgs(healer, patient);
        OnBandageUse?.Invoke(null, args);
    }

    /// <summary>
    /// Raises the bandage use complete event.
    /// </summary>
    public static void RaiseBandageUseComplete(Mobile healer, Mobile patient, TimeSpan delay)
    {
        var args = new BandageUseEventArgs(healer, patient) { Delay = delay };
        OnBandageUseComplete?.Invoke(null, args);
    }

    /// <summary>
    /// Raises the wand use event.
    /// </summary>
    public static void RaiseWandUse(Mobile user, BaseWand wand, Spell spell)
    {
        var args = new WandUseEventArgs(user, wand, spell);
        OnWandUse?.Invoke(null, args);
    }

    /// <summary>
    /// Raises the wand use complete event.
    /// </summary>
    public static void RaiseWandUseComplete(Mobile user, BaseWand wand, Spell spell, TimeSpan delay)
    {
        var args = new WandUseEventArgs(user, wand, spell) { Delay = delay };
        OnWandUseComplete?.Invoke(null, args);
    }

    /// <summary>
    /// Raises the combat enter event.
    /// </summary>
    public static void RaiseCombatEnter(Mobile mobile)
    {
        OnCombatEnter?.Invoke(null, mobile);
    }

    /// <summary>
    /// Raises the combat exit event.
    /// </summary>
    public static void RaiseCombatExit(Mobile mobile)
    {
        OnCombatExit?.Invoke(null, mobile);
    }

    /// <summary>
    /// Raises the spell reflected event.
    /// </summary>
    public static void RaiseSpellReflected(Mobile originalCaster, Mobile reflector, string spellName)
    {
        OnSpellReflected?.Invoke(originalCaster, reflector, spellName);
    }

    /// <summary>
    /// Gets diagnostic information about event handler registrations.
    /// Used for detecting duplicate handlers and integration verification.
    /// </summary>
    public static class Diagnostics
    {
        public static int SpellCastBeginHandlerCount => OnSpellCastBegin?.GetInvocationList()?.Length ?? 0;
        public static int SpellCastHandlerCount => OnSpellCast?.GetInvocationList()?.Length ?? 0;
        public static int SpellCastCompleteHandlerCount => OnSpellCastComplete?.GetInvocationList()?.Length ?? 0;
        public static int WeaponSwingHandlerCount => OnWeaponSwing?.GetInvocationList()?.Length ?? 0;
        public static int WeaponSwingCompleteHandlerCount => OnWeaponSwingComplete?.GetInvocationList()?.Length ?? 0;
    }
}
