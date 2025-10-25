using Server.Commands;
using Server.Items;
using Server.Targeting;

namespace Server.Commands;

/// <summary>
/// Command handler for creating Beta Test Stones in-game.
/// </summary>
/// <remarks>
/// This command allows administrators to place Beta Test Stones at targeted locations.
/// Beta Test Stones grant players maximum skills, stats, and starter equipment when used.
/// </remarks>
public static class BetaTestStoneCommand
{
    /// <summary>
    /// Registers the AddBetaTestStone command with the command system.
    /// </summary>
    public static void Configure()
    {
        CommandSystem.Register("AddBetaTestStone", AccessLevel.Administrator, AddBetaTestStone_OnCommand);
    }

    /// <summary>
    /// Handler for the [AddBetaTestStone command.
    /// </summary>
    /// <param name="e">Command event arguments containing the executing mobile.</param>
    /// <remarks>
    /// Usage: [AddBetaTestStone
    ///
    /// Initiates targeting mode to place a Beta Test Stone at the selected location.
    /// Only accessible to administrators.
    ///
    /// The created stone will:
    /// - Be non-movable
    /// - Have a blue color (Hue 0x486)
    /// - Grant beta tester benefits to players who interact with it
    /// </remarks>
    [Usage("AddBetaTestStone")]
    [Description("Creates a Beta Test Stone at the targeted location.")]
    private static void AddBetaTestStone_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendMessage("Target a location to place the Beta Test Stone.");
        e.Mobile.Target = new BetaTestStoneTarget();
    }

    /// <summary>
    /// Target handler for placing Beta Test Stones.
    /// </summary>
    private class BetaTestStoneTarget : Target
    {
        /// <summary>
        /// Initializes a new instance of the BetaTestStoneTarget class.
        /// </summary>
        public BetaTestStoneTarget() : base(-1, true, TargetFlags.None)
        {
        }

        /// <summary>
        /// Handles the targeting of a location to place the Beta Test Stone.
        /// </summary>
        /// <param name="from">The mobile performing the targeting.</param>
        /// <param name="targeted">The targeted object or location.</param>
        /// <remarks>
        /// Creates a new Beta Test Stone at the targeted location and logs the action.
        /// If the target is invalid, sends an error message to the mobile.
        /// </remarks>
        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is IPoint3D point)
            {
                var loc = new Point3D(point);
                var stone = new BetaTestStone();

                stone.MoveToWorld(loc, from.Map);

                from.SendMessage("Beta Test Stone created successfully!");

                CommandLogging.WriteLine(
                    from,
                    $"{from.AccessLevel} {CommandLogging.Format(from)} created a Beta Test Stone at {loc} in {from.Map}"
                );
            }
            else
            {
                from.SendMessage("Invalid target. Please target a location.");
            }
        }
    }
}
