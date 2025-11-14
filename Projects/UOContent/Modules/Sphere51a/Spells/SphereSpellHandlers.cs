/*************************************************************************
 * ModernUO - Sphere 51a Spell Event Handlers
 * File: SphereSpellHandlers.cs
 *
 * Description: Event handlers for Sphere51a spell casting mechanics.
 *              Implements authentic Sphere51a spell timing and behavior.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using Server.Logging;
using Server.Mobiles;
using Server.Spells;
using Server.Modules.Sphere51a.Combat;
using Server.Modules.Sphere51a.Configuration;
using Server.Modules.Sphere51a.Events;

namespace Server.Modules.Sphere51a.Spells;

/// <summary>
/// Event handlers for Sphere51a spell casting mechanics.
/// Implements authentic Sphere51a spell timing and behavior.
/// </summary>
public static class SphereSpellHandlers
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(SphereSpellHandlers));

    /// <summary>
    /// Handles spell cast begin event - target selected, start cast delay timer.
    /// </summary>
    public static void HandleSpellCastBegin(object sender, SpellCastEventArgs e)
    {
        var caster = e.Caster;
        var spell = e.Spell;

        if (caster == null || spell == null)
            return;

        // Get combat state and begin spell cast
        var state = SphereCombatState.GetOrCreate(caster);
        System.Diagnostics.Debug.Assert(state != null, $"Failed to create SphereCombatState for {caster.Serial}");
        state?.BeginSpellCast(spell);

        // Calculate cast delay using Sphere51a timing
        var skillValue = caster.Skills[spell.CastSkill].Value;
        var fromScroll = spell.Scroll != null && !(spell.Scroll is Server.Items.BaseWand);
        var delayMs = SpellTimingProvider.GetCastDelay(spell, skillValue, fromScroll);

        // Set cast delay (separate from spell recovery timer)
        state?.SetCastDelay(TimeSpan.FromMilliseconds(delayMs));

        SphereConfiguration.DebugLog($"{caster.Name} - Spell cast begun: {spell.GetType().Name}, Delay: {delayMs}ms");
    }

    /// <summary>
    /// Handles spell cast event - optional mid-cast telemetry.
    /// </summary>
    public static void HandleSpellCast(object sender, SpellCastEventArgs e)
    {
        // Optional: Mid-cast telemetry or UI feedback
        // Not used for core timing logic in Sphere51a
    }

    /// <summary>
    /// Handles spell cast complete event - cast delay finished, apply effect.
    /// </summary>
    public static void HandleSpellCastComplete(object sender, SpellCastEventArgs e)
    {
        var caster = e.Caster;

        if (caster == null)
            return;

        // End the spell cast in combat state
        var state = SphereCombatState.GetOrCreate(caster);
        state?.EndSpellCast();

        SphereConfiguration.DebugLog($"{caster.Name} - Spell cast completed");
    }

    /// <summary>
    /// Handles spell blocks movement query - always allow movement in Sphere51a.
    /// </summary>
    public static bool HandleSpellBlocksMovement(Mobile caster, Spell spell)
    {
        // Sphere51a allows free movement during spell casting
        return false;
    }
}
