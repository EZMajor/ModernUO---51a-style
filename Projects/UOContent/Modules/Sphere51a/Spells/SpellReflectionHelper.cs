/*************************************************************************
 * ModernUO - Sphere 51a Spell Reflection Helper
 * File: SpellReflectionHelper.cs
 *
 * Description: Handles spell reflection mechanics for testing.
 *              Detects reflection auras and manages caster/target swaps.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using Server.Logging;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Fifth;
using Server.Modules.Sphere51a.Events;

namespace Server.Modules.Sphere51a.Spells;

/// <summary>
/// Helper class for handling spell reflection mechanics in Sphere51a testing.
/// Provides fallback reflection detection when existing systems are insufficient.
/// </summary>
public static class SpellReflectionHelper
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(SpellReflectionHelper));

    /// <summary>
    /// Checks if a target has spell reflection active.
    /// </summary>
    /// <param name="target">The target to check</param>
    /// <returns>True if reflection is active</returns>
    public static bool HasSpellReflection(Mobile target)
    {
        if (target == null)
            return false;

        // Check for MagicReflectSpell effect using the same method as MagicReflectSpell
        return target.MagicDamageAbsorb > 0;
    }

    /// <summary>
    /// Processes spell reflection for a spell cast.
    /// </summary>
    /// <param name="caster">The original caster</param>
    /// <param name="target">The original target</param>
    /// <param name="spell">The spell being cast</param>
    /// <returns>Tuple of (reflected, actualTarget)</returns>
    public static (bool Reflected, Mobile ActualTarget) ProcessReflection(Mobile caster, Mobile target, Spell spell)
    {
        if (!HasSpellReflection(target))
        {
            return (false, target);
        }

        // Spell is reflected - swap caster and target
        var actualTarget = caster;

        logger.Debug("Spell {Spell} reflected from {Target} back to {Caster}",
            spell?.GetType().Name ?? "Unknown", target.Name, caster.Name);

        // Fire reflection event
        SphereEvents.RaiseSpellReflected(caster, target, spell?.GetType().Name ?? "Unknown");

        return (true, actualTarget);
    }

    /// <summary>
    /// Gets the reflection status for logging purposes.
    /// </summary>
    /// <param name="target">The target to check</param>
    /// <returns>Reflection status string</returns>
    public static string GetReflectionStatus(Mobile target)
    {
        return HasSpellReflection(target) ? "REFLECTED" : "NORMAL";
    }
}
