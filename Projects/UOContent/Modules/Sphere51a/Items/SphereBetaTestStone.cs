/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: SphereBetaTestStone.cs
 *
 * Description: Beta Test Stone item for Sphere 51a mechanics.
 *              Provides testing interface for combat configurations.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using ModernUO.Serialization;
using Server.Commands;
using Server.Gumps;
using Server.Logging;
using Server.Mobiles;
using Server.Network;
using Server.Modules.Sphere51a.Configuration;

namespace Server.Modules.Sphere51a.Items;

/// <summary>
/// Beta Test Stone for testing Sphere 51a combat mechanics.
/// Provides interface to test different combat configurations.
/// </summary>
[SerializationGenerator(0)]
public partial class SphereBetaTestStone : Item
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(SphereBetaTestStone));

    /// <summary>
    /// Whether the beta test stone system has been initialized.
    /// </summary>
    public static bool IsInitialized { get; private set; }

    [Constructible]
    public SphereBetaTestStone() : base(0xED4) // Gravestone ItemID
    {
        Movable = false;
        Hue = 0x0AD6; // Custom color for Sphere stones
        Name = "Sphere Beta Test Stone";
    }

    /// <summary>
    /// Gets the default name displayed for this stone.
    /// </summary>
    public override string DefaultName => "Sphere Beta Test Stone";

    /// <summary>
    /// Handles double-click interaction with the stone.
    /// </summary>
    public override void OnDoubleClick(Mobile from)
    {
        if (from?.Player != true)
        {
            return;
        }

        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that
            return;
        }

        if (!SphereConfiguration.Enabled)
        {
            from.SendMessage("Sphere 51a combat system is disabled.");
            return;
        }

        from.SendGump(new SphereBetaTestGump(from, this));
    }

    /// <summary>
    /// Initializes the beta test stone system.
    /// </summary>
    public static void Initialize()
    {
        if (IsInitialized)
        {
            logger.Warning("Sphere beta test stone already initialized");
            return;
        }

        if (!SphereConfiguration.Enabled)
        {
            logger.Information("Sphere beta test stone not initialized - Sphere system disabled");
            return;
        }

        IsInitialized = true;
        logger.Information("Sphere beta test stone initialized");
    }

    /// <summary>
    /// Configures the beta test stone system during the Configure phase.
    /// </summary>
    public static void Configure()
    {
        if (!SphereConfiguration.Enabled)
        {
            return;
        }

        // Register command to place beta test stone
        CommandSystem.Register("AddSphereBetaTestStone", AccessLevel.Administrator, AddSphereBetaTestStone_OnCommand);
    }

    [Usage("AddSphereBetaTestStone")]
    [Description("Places a Sphere Beta Test Stone at your location.")]
    private static void AddSphereBetaTestStone_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (!SphereConfiguration.Enabled)
        {
            from.SendMessage("Sphere 51a combat system is disabled.");
            return;
        }

        // Create Sphere beta test stone
        var stone = new SphereBetaTestStone
        {
            Movable = false,
            Name = "Sphere Beta Test Stone"
        };

        stone.MoveToWorld(from.Location, from.Map);

        from.SendMessage("A Sphere Beta Test Stone has been placed at your location.");
        from.SendMessage("Double-click it to access testing and configuration tools.");
    }

    /// <summary>
    /// Gets the beta test stone system status for diagnostics.
    /// </summary>
    public static string GetStatus()
    {
        if (!SphereConfiguration.Enabled)
        {
            return "Disabled (Sphere system disabled)";
        }

        return IsInitialized ? "Initialized" : "Not Initialized";
    }
}

/// <summary>
/// Gump for Sphere Beta Test Stone configuration and testing.
/// </summary>
public class SphereBetaTestGump : Gump
{
    private readonly Mobile _user;
    private readonly SphereBetaTestStone _stone;

    public SphereBetaTestGump(Mobile user, SphereBetaTestStone stone) : base(50, 50)
    {
        _user = user;
        _stone = stone;

        Closable = true;
        Disposable = true;
        Draggable = true;
        Resizable = false;

        AddPage(0);

        // Main background
        AddBackground(0, 0, 600, 500, 5120);

        // Title
        AddBackground(20, 15, 560, 30, 5054);
        AddHtml(40, 22, 520, 20, "<basefont color=#0481>Sphere 51a Beta Test Configuration</basefont>", false, false);

        // Combat Testing Section
        AddBackground(20, 55, 560, 120, 3000);
        AddHtml(40, 65, 520, 20, "<basefont color=#44>Combat Testing Tools:</basefont>", false, false);

        // Test buttons
        AddButton(40, 90, 4005, 4007, 1);
        AddHtml(80, 93, 200, 20, "<basefont color=#44>Test Weapon Swing Speed</basefont>", false, false);

        AddButton(40, 115, 4005, 4007, 2);
        AddHtml(80, 118, 200, 20, "<basefont color=#44>Test Spell Cast Delay</basefont>", false, false);

        AddButton(40, 140, 4005, 4007, 3);
        AddHtml(80, 143, 200, 20, "<basefont color=#44>Test Bandage Timing</basefont>", false, false);

        AddButton(300, 90, 4005, 4007, 4);
        AddHtml(340, 93, 200, 20, "<basefont color=#44>Test Wand Use</basefont>", false, false);

        AddButton(300, 115, 4005, 4007, 5);
        AddHtml(340, 118, 200, 20, "<basefont color=#44>Action Cancellation Test</basefont>", false, false);

        AddButton(300, 140, 4005, 4007, 6);
        AddHtml(340, 143, 200, 20, "<basefont color=#44>Performance Benchmark</basefont>", false, false);

        // Configuration Section
        AddBackground(20, 185, 560, 200, 3000);
        AddHtml(40, 195, 520, 20, "<basefont color=#44>Sphere Configuration Testing:</basefont>", false, false);

        int yOffset = 220;

        // Independent Timers
        AddCheck(40, yOffset, 210, 211, SphereConfiguration.IndependentTimers, 10);
        AddHtml(70, yOffset + 3, 200, 20, "<basefont color=#44>Independent Timers</basefont>", false, false);

        // Remove Global Recovery
        AddCheck(40, yOffset + 25, 210, 211, SphereConfiguration.RemoveGlobalRecovery, 11);
        AddHtml(70, yOffset + 28, 200, 20, "<basefont color=#44>No Global Recovery</basefont>", false, false);

        // Action Cancellation
        AddCheck(40, yOffset + 50, 210, 211, SphereConfiguration.SpellCancelSwing, 12);
        AddHtml(70, yOffset + 53, 200, 20, "<basefont color=#44>Spell Cancels Swing</basefont>", false, false);

        AddCheck(250, yOffset + 50, 210, 211, SphereConfiguration.SwingCancelSpell, 13);
        AddHtml(280, yOffset + 53, 200, 20, "<basefont color=#44>Swing Cancels Spell</basefont>", false, false);

        // Movement During Cast
        AddCheck(40, yOffset + 75, 210, 211, SphereConfiguration.AllowMovementDuringCast, 14);
        AddHtml(70, yOffset + 78, 200, 20, "<basefont color=#44>Movement During Cast</basefont>", false, false);

        // Immediate Damage
        AddCheck(250, yOffset + 75, 210, 211, SphereConfiguration.ImmediateDamageApplication, 15);
        AddHtml(280, yOffset + 78, 200, 20, "<basefont color=#44>Immediate Damage</basefont>", false, false);

        // Apply Configuration button
        AddButton(200, 410, 4005, 4007, 7);
        AddHtml(240, 413, 150, 20, "<basefont color=#44>Apply Configuration</basefont>", false, false);

        // Results display area
        AddBackground(20, 440, 560, 40, 3000);
        AddHtml(40, 450, 520, 20, "<basefont color=#44>Test Results: Ready</basefont>", false, false);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (from == null || _stone.Deleted)
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 0: // Close
                break;

            case 1: // Test Weapon Swing Speed
                RunWeaponSwingTest(from);
                from.SendGump(new SphereBetaTestGump(from, _stone));
                break;

            case 2: // Test Spell Cast Delay
                RunSpellCastTest(from);
                from.SendGump(new SphereBetaTestGump(from, _stone));
                break;

            case 3: // Test Bandage Timing
                RunBandageTest(from);
                from.SendGump(new SphereBetaTestGump(from, _stone));
                break;

            case 4: // Test Wand Use
                RunWandTest(from);
                from.SendGump(new SphereBetaTestGump(from, _stone));
                break;

            case 5: // Action Cancellation Test
                RunCancellationTest(from);
                from.SendGump(new SphereBetaTestGump(from, _stone));
                break;

            case 6: // Performance Benchmark
                RunPerformanceBenchmark(from);
                from.SendGump(new SphereBetaTestGump(from, _stone));
                break;

            case 7: // Apply Configuration
                ApplyConfiguration(from, info);
                from.SendGump(new SphereBetaTestGump(from, _stone));
                break;

            default:
                from.SendGump(new SphereBetaTestGump(from, _stone));
                break;
        }
    }

    private void RunWeaponSwingTest(Mobile from)
    {
        from.SendMessage(0x44, "Weapon swing test initiated. Attack something to measure timing.");
        // The actual timing measurement would be handled by the combat system
        // This is just the UI trigger
    }

    private void RunSpellCastTest(Mobile from)
    {
        from.SendMessage(0x44, "Spell cast test initiated. Cast a spell to measure timing.");
        // The actual timing measurement would be handled by the combat system
    }

    private void RunBandageTest(Mobile from)
    {
        from.SendMessage(0x44, "Bandage test initiated. Use a bandage to measure timing.");
        // The actual timing measurement would be handled by the combat system
    }

    private void RunWandTest(Mobile from)
    {
        from.SendMessage(0x44, "Wand test initiated. Use a wand to measure timing.");
        // The actual timing measurement would be handled by the combat system
    }

    private void RunCancellationTest(Mobile from)
    {
        from.SendMessage(0x44, "Action cancellation test initiated.");
        from.SendMessage(0x44, "Try starting a spell cast, then immediately attacking.");
        from.SendMessage(0x44, "Or start attacking, then immediately cast a spell.");
    }

    private void RunPerformanceBenchmark(Mobile from)
    {
        from.SendMessage(0x44, "Running performance benchmark...");
        // This would measure various timing aspects
        from.SendMessage(0x44, $"Independent Timers: {SphereConfiguration.IndependentTimers}");
        from.SendMessage(0x44, $"Global Recovery Removed: {SphereConfiguration.RemoveGlobalRecovery}");
        from.SendMessage(0x44, $"Immediate Damage: {SphereConfiguration.ImmediateDamageApplication}");
    }

    private void ApplyConfiguration(Mobile from, RelayInfo info)
    {
        // Update configuration based on checkboxes
        SphereConfiguration.IndependentTimers = info.IsSwitched(10);
        SphereConfiguration.RemoveGlobalRecovery = info.IsSwitched(11);
        SphereConfiguration.SpellCancelSwing = info.IsSwitched(12);
        SphereConfiguration.SwingCancelSpell = info.IsSwitched(13);
        SphereConfiguration.AllowMovementDuringCast = info.IsSwitched(14);
        SphereConfiguration.ImmediateDamageApplication = info.IsSwitched(15);

        from.SendMessage(0x44, "Sphere configuration updated!");
        from.SendMessage(0x44, "Changes will take effect immediately.");
    }
}
