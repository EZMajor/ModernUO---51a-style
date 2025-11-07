/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: SphereCombatSystem.cs
 *
 * Description: Combat system event handlers for Sphere 51a mechanics.
 *              Handles weapon swings, spell casting, bandaging, and wand usage.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Modules.Sphere51a.Configuration;
using Server.Modules.Sphere51a.Events;
using Server.Modules.Sphere51a.Extensions;
using Server.Spells;

namespace Server.Modules.Sphere51a.Combat;

/// <summary>
/// Central combat system for Sphere 51a mechanics.
/// Handles all combat-related events through the event system.
/// </summary>
public static class SphereCombatSystem
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(SphereCombatSystem));

    /// <summary>
    /// Whether the combat system has been initialized.
    /// </summary>
    public static bool IsInitialized { get; private set; }

    /// <summary>
    /// Initializes the combat system.
    /// </summary>
    public static void Initialize()
    {
        if (IsInitialized)
        {
            logger.Warning("Sphere combat system already initialized");
            return;
        }

        IsInitialized = true;
        logger.Information("Sphere combat system initialized");
    }

    /// <summary>
    /// Configures the combat system during the Configure phase.
    /// </summary>
    public static void Configure()
    {
        // Configuration phase - setup commands, etc.
        logger.Debug("Sphere combat system configured");
    }

    /// <summary>
    /// Performs final initialization during the Initialize phase.
    /// </summary>
    public static void InitializePhase()
    {
        // Final initialization phase
        logger.Debug("Sphere combat system initialization phase complete");
    }

    /// <summary>
    /// Gets the combat system status for diagnostics.
    /// </summary>
    public static string GetStatus() => IsInitialized ? "Initialized" : "Not Initialized";

    #region Weapon Combat Handlers

    /// <summary>
    /// Handles weapon swing events.
    /// </summary>
    public static void HandleWeaponSwing(object sender, WeaponSwingEventArgs e)
    {
        if (!SphereConfiguration.Enabled)
            return;

        var attacker = e.Attacker;
        var defender = e.Defender;
        var weapon = e.Weapon;

        // Validate swing according to Sphere rules
        var canSwing = attacker.SphereCanSwing();

        if (!canSwing)
        {
            e.Cancelled = true;
            SphereConfiguration.LogCancellation(attacker, "Weapon swing", "Timer not ready");
            return;
        }

        // Check if swing is blocked by current state
        if (SphereConfiguration.DisableSwingDuringCast && attacker.SphereIsCasting())
        {
            e.Cancelled = true;
            SphereConfiguration.LogCancellation(attacker, "Weapon swing", "Currently casting");
            return;
        }

        if (SphereConfiguration.DisableSwingDuringCastDelay && attacker.SphereIsInCastDelay())
        {
            e.Cancelled = true;
            SphereConfiguration.LogCancellation(attacker, "Weapon swing", "In cast delay");
            return;
        }

        // Swing is allowed - track it
        attacker.SphereBeginSwing();
        SphereConfiguration.DebugLog($"{attacker.Name} - Weapon swing initiated");
    }

    /// <summary>
    /// Handles weapon swing completion events.
    /// </summary>
    public static void HandleWeaponSwingComplete(object sender, WeaponSwingEventArgs e)
    {
        if (!SphereConfiguration.Enabled)
            return;

        var attacker = e.Attacker;
        var weapon = e.Weapon;
        var delay = e.Delay;

        // Apply Sphere swing timing
        if (SphereConfiguration.IndependentTimers)
        {
            attacker.SphereSetNextSwingTime(delay);
            SphereConfiguration.LogTimerChange(attacker, "NextSwingTime", 0, (long)delay.TotalMilliseconds);
        }

        // Calculate Sphere-style swing speed if enabled
        if (SphereConfiguration.SphereSwingSpeedCalculation)
        {
            // Apply dexterity modifier
            var dexModifier = attacker.SphereGetDexterityModifier();
            delay = TimeSpan.FromSeconds(delay.TotalSeconds * dexModifier);

            // Apply bounds
            delay = TimeSpan.FromSeconds(Math.Max(delay.TotalSeconds, SphereConfiguration.MinimumSwingSpeed));
            delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds, SphereConfiguration.MaximumSwingSpeed));
        }

        e.Delay = delay;
        SphereConfiguration.DebugLog($"{attacker.Name} - Weapon swing completed, delay: {delay.TotalSeconds}s");
    }

    #endregion

    #region Spell Casting Handlers

    /// <summary>
    /// Handles spell cast events.
    /// </summary>
    public static void HandleSpellCast(object sender, SpellCastEventArgs e)
    {
        if (!SphereConfiguration.Enabled)
            return;

        var caster = e.Caster;
        var spell = e.Spell;

        // Validate cast according to Sphere rules
        var canCast = caster.SphereCanCast();

        if (!canCast)
        {
            e.Cancelled = true;
            SphereConfiguration.LogCancellation(caster, "Spell cast", "Timer not ready");
            return;
        }

        SphereConfiguration.DebugLog($"{caster.Name} - Spell cast validated: {spell.GetType().Name}");
    }

    /// <summary>
    /// Handles spell cast begin events.
    /// </summary>
    public static void HandleSpellCastBegin(object sender, SpellCastEventArgs e)
    {
        if (!SphereConfiguration.Enabled)
            return;

        var caster = e.Caster;
        var spell = e.Spell;

        // Cancel pending swing if configured
        if (SphereConfiguration.SpellCancelSwing && caster.SphereHasPendingSwing())
        {
            caster.SphereCancelSwing("Spell cast begun");
        }

        // Track spell casting
        caster.SphereBeginSpellCast(spell);
        SphereConfiguration.DebugLog($"{caster.Name} - Spell cast begun: {spell.GetType().Name}");
    }

    /// <summary>
    /// Handles spell cast complete events.
    /// </summary>
    public static void HandleSpellCastComplete(object sender, SpellCastEventArgs e)
    {
        if (!SphereConfiguration.Enabled)
            return;

        var caster = e.Caster;
        var spell = e.Spell;

        // Remove post-cast recovery if configured
        if (SphereConfiguration.RemovePostCastRecovery)
        {
            // NextSpellTime is not set, allowing immediate next cast
            SphereConfiguration.DebugLog($"{caster.Name} - Post-cast recovery removed");
        }
        else if (SphereConfiguration.IndependentTimers)
        {
            // Set independent spell timer
            var recovery = spell.GetCastRecovery();
            caster.SphereSetNextSpellTime(recovery);
            SphereConfiguration.LogTimerChange(caster, "NextSpellTime", 0, (long)recovery.TotalMilliseconds);
        }

        caster.SphereEndSpellCast();
        SphereConfiguration.DebugLog($"{caster.Name} - Spell cast completed: {spell.GetType().Name}");
    }

    /// <summary>
    /// Handles spell blocks movement queries.
    /// </summary>
    public static bool HandleSpellBlocksMovement(Mobile caster, Spell spell)
    {
        if (!SphereConfiguration.Enabled)
            return true; // Default ModernUO behavior

        // Sphere allows movement during casting
        var blocksMovement = !SphereConfiguration.AllowMovementDuringCast;
        SphereConfiguration.DebugLog($"{caster.Name} - Spell blocks movement: {blocksMovement}");

        return blocksMovement;
    }

    #endregion

    #region Bandage Handlers

    /// <summary>
    /// Handles bandage use events.
    /// </summary>
    public static void HandleBandageUse(object sender, BandageUseEventArgs e)
    {
        if (!SphereConfiguration.Enabled)
            return;

        var healer = e.Healer;
        var patient = e.Patient;

        // Validate bandage use
        var canBandage = healer.SphereCanBandage();

        if (!canBandage)
        {
            e.Cancelled = true;
            SphereConfiguration.LogCancellation(healer, "Bandaging", "Timer not ready");
            return;
        }

        // Cancel other actions if configured
        if (SphereConfiguration.BandageCancelActions)
        {
            if (healer.SphereHasPendingSwing())
                healer.SphereCancelSwing("Bandage use");

            if (healer.SphereIsCasting())
                healer.SphereCancelSpell("Bandage use");
        }

        // Track bandage use
        healer.SphereBeginBandage(patient);
        SphereConfiguration.DebugLog($"{healer.Name} - Bandage use initiated on {patient?.Name ?? "self"}");
    }

    /// <summary>
    /// Handles bandage use complete events.
    /// </summary>
    public static void HandleBandageUseComplete(object sender, BandageUseEventArgs e)
    {
        if (!SphereConfiguration.Enabled)
            return;

        var healer = e.Healer;
        var patient = e.Patient;
        var delay = e.Delay;

        // Set independent bandage timer if configured
        if (SphereConfiguration.IndependentBandageTimer)
        {
            healer.SphereSetNextBandageTime(delay);
            SphereConfiguration.LogTimerChange(healer, "NextBandageTime", 0, (long)delay.TotalMilliseconds);
        }

        healer.SphereEndBandage();
        SphereConfiguration.DebugLog($"{healer.Name} - Bandage use completed on {patient?.Name ?? "self"}");
    }

    #endregion

    #region Wand Handlers

    /// <summary>
    /// Handles wand use events.
    /// </summary>
    public static void HandleWandUse(object sender, WandUseEventArgs e)
    {
        if (!SphereConfiguration.Enabled)
            return;

        var user = e.User;
        var wand = e.Wand;
        var spell = e.Spell;

        // Validate wand use
        var canUseWand = user.SphereCanUseWand();

        if (!canUseWand)
        {
            e.Cancelled = true;
            SphereConfiguration.LogCancellation(user, "Wand use", "Timer not ready");
            return;
        }

        // Cancel other actions if configured
        if (SphereConfiguration.WandCancelActions)
        {
            if (user.SphereHasPendingSwing())
                user.SphereCancelSwing("Wand use");

            if (user.SphereIsCasting())
                user.SphereCancelSpell("Wand use");
        }

        // Track wand use
        user.SphereBeginWandUse(wand, spell);
        SphereConfiguration.DebugLog($"{user.Name} - Wand use initiated: {wand.GetType().Name}");
    }

    /// <summary>
    /// Handles wand use complete events.
    /// </summary>
    public static void HandleWandUseComplete(object sender, WandUseEventArgs e)
    {
        if (!SphereConfiguration.Enabled)
            return;

        var user = e.User;
        var wand = e.Wand;
        var spell = e.Spell;
        var delay = e.Delay;

        // Apply instant cast if configured
        if (SphereConfiguration.InstantWandCast)
        {
            delay = TimeSpan.Zero;
            SphereConfiguration.DebugLog($"{user.Name} - Wand instant cast applied");
        }
        else if (SphereConfiguration.IndependentTimers)
        {
            // Set independent wand timer
            user.SphereSetNextWandTime(delay);
            SphereConfiguration.LogTimerChange(user, "NextWandTime", 0, (long)delay.TotalMilliseconds);
        }

        e.Delay = delay;
        user.SphereEndWandUse();
        SphereConfiguration.DebugLog($"{user.Name} - Wand use completed: {spell.GetType().Name}");
    }

    #endregion

    #region Combat State Handlers

    /// <summary>
    /// Handles combat enter events.
    /// </summary>
    public static void HandleCombatEnter(object sender, Mobile mobile)
    {
        if (!SphereConfiguration.Enabled)
            return;

        // Initialize combat state for mobile
        mobile.SphereInitializeCombatState();
        SphereConfiguration.DebugLog($"{mobile.Name} - Entered combat");
    }

    /// <summary>
    /// Handles combat exit events.
    /// </summary>
    public static void HandleCombatExit(object sender, Mobile mobile)
    {
        if (!SphereConfiguration.Enabled)
            return;

        // Clean up combat state
        mobile.SphereClearCombatState();
        SphereConfiguration.DebugLog($"{mobile.Name} - Exited combat");
    }

    #endregion
}
