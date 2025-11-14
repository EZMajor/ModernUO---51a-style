/*************************************************************************
 * ModernUO - Sphere 51a Spell School Enumeration
 * File: SpellSchool.cs
 *
 * Description: Defines spell schools for independent timer management.
 *              Used by spell tests to verify cross-school isolation.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

namespace Server.Modules.Sphere51a.Spells;

/// <summary>
/// Spell schools for Sphere51a testing.
/// Each school maintains independent timers and cooldowns.
/// </summary>
public enum SpellSchool
{
    /// <summary>
    /// Magery spells (basic magic system).
    /// </summary>
    Magery,

    /// <summary>
    /// Necromancy spells (undead magic).
    /// </summary>
    Necromancy,

    /// <summary>
    /// Chivalry spells (paladin abilities).
    /// </summary>
    Chivalry,

    /// <summary>
    /// Bushido spells (samurai abilities).
    /// </summary>
    Bushido,

    /// <summary>
    /// Ninjitsu spells (ninja abilities).
    /// </summary>
    Ninjitsu
}
