/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: SphereDuelContext.cs
 *
 * Description: Duel context implementation for Sphere 51a mechanics.
 *              Extends base DuelContext with Sphere-specific combat rules.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using Server.Logging;
using Server.Engines.DuelArena;
using Server.Modules.Sphere51a.Configuration;

namespace Server.Modules.Sphere51a.DuelArena;

/// <summary>
/// Duel context that enforces Sphere 51a-style combat mechanics during duels.
/// Extends the base DuelContext to override combat behavior.
/// </summary>
public class SphereDuelContext : DuelContext
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(SphereDuelContext));

    /// <summary>
    /// Whether this duel context uses Sphere mechanics.
    /// </summary>
    public bool UsesSphereMechanics { get; private set; }

    /// <summary>
    /// Initializes a new instance of the SphereDuelContext class.
    /// </summary>
    /// <param name="arena">The duel arena this context belongs to.</param>
    /// <param name="duelType">The type of duel being conducted.</param>
    /// <param name="entryCost">The entry cost for the duel.</param>
    /// <param name="ladderEnabled">Whether ladder rankings are enabled.</param>
    /// <param name="useSphereMechanics">Whether to use Sphere mechanics for this duel.</param>
    public SphereDuelContext(Server.Engines.DuelArena.DuelArena arena, DuelType duelType, int entryCost, bool ladderEnabled, bool useSphereMechanics = true)
        : base(arena, duelType, entryCost, ladderEnabled)
    {
        UsesSphereMechanics = useSphereMechanics && SphereConfiguration.Enabled;

        if (UsesSphereMechanics)
        {
            logger.Debug("Sphere duel context created for arena {ArenaName}", arena?.Name ?? "Unknown");
        }
    }

    /// <summary>
    /// Begins the duel with Sphere mechanics applied.
    /// Note: This method shadows the base implementation since BeginDuel is not virtual.
    /// </summary>
    public new void BeginDuel()
    {
        if (State != DuelState.Countdown)
        {
            return;
        }

        // Apply Sphere mechanics to all participants if enabled
        if (UsesSphereMechanics)
        {
            PrepareSphereParticipants();
        }

        // Call base implementation which handles the rest
        base.BeginDuel();

        if (UsesSphereMechanics)
        {
            logger.Information("Sphere duel begun with {Count} participants", Participants.Count);
        }
    }

    /// <summary>
    /// Prepares participants for Sphere-style combat.
    /// </summary>
    private void PrepareSphereParticipants()
    {
        foreach (var participant in Participants)
        {
            if (participant.Mobile is { Deleted: false } pm)
            {
                // Ensure Sphere mechanics are active for this mobile during the duel
                // The SphereCombatSystem will handle the actual mechanics
                pm.SendMessage(0x44, "Sphere 51a combat mechanics active for this duel!");
            }
        }
    }



    /// <summary>
    /// Gets a string representation of this duel context.
    /// </summary>
    /// <returns>A string describing the duel context.</returns>
    public override string ToString()
    {
        return UsesSphereMechanics
            ? $"Sphere Duel Context - {DuelType} - {Participants.Count} participants"
            : base.ToString();
    }
}
