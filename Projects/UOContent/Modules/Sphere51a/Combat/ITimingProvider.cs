/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: ITimingProvider.cs
 *
 * Description: Interface for weapon timing providers in Sphere 51a system.
 *              Allows different timing implementations (global tick vs legacy).
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Modules.Sphere51a.Combat;

/// <summary>
/// Interface for weapon timing providers.
/// Allows swapping between global tick system and legacy per-mobile timers.
/// </summary>
public interface ITimingProvider
{
    /// <summary>
    /// Gets the attack interval in milliseconds for the given attacker and weapon.
    /// </summary>
    /// <param name="attacker">The mobile attacking</param>
    /// <param name="weapon">The weapon being used</param>
    /// <returns>Attack interval in milliseconds</returns>
    int GetAttackIntervalMs(Mobile attacker, Item weapon);

    /// <summary>
    /// Gets the animation hit offset for the given weapon.
    /// </summary>
    /// <param name="weapon">The weapon being used</param>
    /// <returns>Animation hit offset in milliseconds</returns>
    int GetAnimationHitOffsetMs(Item weapon);

    /// <summary>
    /// Gets the animation duration for the given weapon.
    /// </summary>
    /// <param name="weapon">The weapon being used</param>
    /// <returns>Animation duration in milliseconds</returns>
    int GetAnimationDurationMs(Item weapon);

    /// <summary>
    /// Gets the provider name for logging/debugging.
    /// </summary>
    string ProviderName { get; }
}
