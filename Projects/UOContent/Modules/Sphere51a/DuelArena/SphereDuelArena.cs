/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: SphereDuelArena.cs
 *
 * Description: Duel Arena system for Sphere 51a mechanics.
 *              Provides commands and integration for Sphere-style duels.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using Server.Commands;
using Server.Engines.DuelArena;
using Server.Logging;
using Server.Modules.Sphere51a.Configuration;

namespace Server.Modules.Sphere51a.DuelArena;

/// <summary>
/// Duel Arena system for Sphere 51a combat mechanics.
/// Provides commands and integration for Sphere-style duels.
/// </summary>
public static class SphereDuelArena
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(SphereDuelArena));

    /// <summary>
    /// Whether the duel arena system has been initialized.
    /// </summary>
    public static bool IsInitialized { get; private set; }

    /// <summary>
    /// Initializes the duel arena system.
    /// </summary>
    public static void Initialize()
    {
        if (IsInitialized)
        {
            logger.Warning("Sphere duel arena already initialized");
            return;
        }

        if (!SphereConfiguration.Enabled)
        {
            logger.Information("Sphere duel arena not initialized - Sphere system disabled");
            return;
        }

        IsInitialized = true;
        logger.Information("Sphere duel arena initialized");
    }

    /// <summary>
    /// Configures the duel arena system during the Configure phase.
    /// </summary>
    public static void Configure()
    {
        if (!SphereConfiguration.Enabled)
        {
            return;
        }

        // Register Sphere duel commands
        CommandSystem.Register("SphereDuel", AccessLevel.Player, SphereDuel_OnCommand);
        CommandSystem.Register("AddSphereDuelStone", AccessLevel.Administrator, AddSphereDuelStone_OnCommand);

        logger.Debug("Sphere duel arena configured");
    }

    /// <summary>
    /// Performs final initialization during the Initialize phase.
    /// </summary>
    public static void InitializePhase()
    {
        if (!SphereConfiguration.Enabled)
        {
            return;
        }

        // Final initialization phase
        logger.Debug("Sphere duel arena initialization phase complete");
    }

    /// <summary>
    /// Gets the duel arena system status for diagnostics.
    /// </summary>
    public static string GetStatus()
    {
        if (!SphereConfiguration.Enabled)
        {
            return "Disabled (Sphere system disabled)";
        }

        return IsInitialized ? "Initialized" : "Not Initialized";
    }

    [Usage("SphereDuel")]
    [Description("Initiates a Sphere-style duel with another player.")]
    private static void SphereDuel_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (!SphereConfiguration.Enabled)
        {
            from.SendMessage("Sphere 51a combat system is disabled.");
            return;
        }

        from.SendMessage("Target the player you wish to duel with Sphere mechanics.");
        from.Target = new SphereDuelTarget();
    }

    [Usage("AddSphereDuelStone")]
    [Description("Places a Sphere duel stone at your location.")]
    private static void AddSphereDuelStone_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (!SphereConfiguration.Enabled)
        {
            from.SendMessage("Sphere 51a combat system is disabled.");
            return;
        }

        // Create Sphere duel stone
        var stone = new SphereDuelStoneComponent
        {
            Type = DuelType.Money1v1,
            EntryCost = 1000,
            Movable = false,
            Name = "Sphere Duel Stone"
        };

        stone.MoveToWorld(from.Location, from.Map);

        from.SendMessage("A Sphere duel stone has been placed at your location.");
        from.SendMessage("Use [Props to configure it, or double-click as a GM for options.");
    }
}

/// <summary>
/// Target for initiating Sphere duels.
/// </summary>
public class SphereDuelTarget : Server.Targeting.Target
{
    public SphereDuelTarget() : base(15, false, Server.Targeting.TargetFlags.None)
    {
    }

    protected override void OnTarget(Mobile from, object targeted)
    {
        if (targeted is not Mobile target)
        {
            from.SendMessage("You must target a player.");
            return;
        }

        if (target == from)
        {
            from.SendMessage("You cannot duel yourself.");
            return;
        }

        if (!target.Player)
        {
            from.SendMessage("You can only duel other players.");
            return;
        }

        // Find nearby Sphere duel stone
        var stone = FindNearbySphereDuelStone(from);
        if (stone == null)
        {
            from.SendMessage("You must be near a Sphere duel stone to initiate a duel.");
            from.SendMessage("Use [AddSphereDuelStone to place one.");
            return;
        }

        // Sphere duels use the standard duel system but with Sphere mechanics enabled globally
        // The SphereCombatSystem will handle the mechanics during the duel
        from.SendMessage("Sphere duel system uses standard duel stones with Sphere mechanics enabled.");
        from.SendMessage("Use a regular duel stone - Sphere mechanics will be active if enabled globally.");
    }

    private SphereDuelStoneComponent FindNearbySphereDuelStone(Mobile from)
    {
        var map = from.Map;
        if (map == null)
        {
            return null;
        }

        // Search in a 10-tile radius
        foreach (var item in map.GetItemsInRange(from.Location, 10))
        {
            if (item is SphereDuelStoneComponent stone && stone.UseSphereMechanics)
            {
                return stone;
            }
        }

        return null;
    }
}
