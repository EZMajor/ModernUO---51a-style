using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

/// <summary>
/// Beta Test Stone - An interactive stone that grants players beta tester status with maximum skills, stats, and starter items.
/// </summary>
/// <remarks>
/// When double-clicked by a player, this stone presents a confirmation gump. Upon acceptance:
/// - Sets all skills to 100.0 (maximum)
/// - Sets all stats (Str, Dex, Int) to 100
/// - Provides starter equipment and resources in the player's bank box
/// - Marks the player with the BetaTester flag to prevent duplicate setups
///
/// The stone is non-movable and uses a blue-colored gravestone appearance.
/// </remarks>
[SerializationGenerator(0)]
public partial class BetaTestStone : Item
{
    /// <summary>
    /// Initializes a new instance of the BetaTestStone class.
    /// </summary>
    [Constructible]
    public BetaTestStone() : base(0xED4) // Gravestone ItemID
    {
        Movable = false;
        Hue = 0x486; // Blue color
    }

    /// <summary>
    /// Gets the default name displayed for this stone.
    /// </summary>
    public override string DefaultName => "Beta Test Stone";

    /// <summary>
    /// Handles double-click interaction with the stone.
    /// </summary>
    /// <param name="from">The mobile double-clicking the stone.</param>
    /// <remarks>
    /// Only players within range can interact with the stone.
    /// Players who have already used the stone (BetaTester flag set) will receive a message and cannot use it again.
    /// </remarks>
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

        // Check if already a beta tester
        if (from is PlayerMobile pm && pm.BetaTester)
        {
            from.SendMessage(0x44, "You are already a Beta Tester.");
            return;
        }

        from.SendGump(new BetaTestStoneGump(from, this));
    }
}

/// <summary>
/// Gump displayed when a player interacts with the Beta Test Stone.
/// </summary>
/// <remarks>
/// Presents two options:
/// - Accept: Activates beta tester status and grants all bonuses
/// - Deny: Closes the gump without changes
/// </remarks>
public class BetaTestStoneGump : Gump
{
    private readonly BetaTestStone _stone;

    /// <summary>
    /// Initializes a new instance of the BetaTestStoneGump class.
    /// </summary>
    /// <param name="from">The mobile viewing the gump.</param>
    /// <param name="stone">The Beta Test Stone that spawned this gump.</param>
    public BetaTestStoneGump(Mobile from, BetaTestStone stone) : base(50, 50)
    {
        _stone = stone;

        Closable = true;
        Disposable = true;
        Draggable = true;
        Resizable = false;

        AddPage(0);

        // Main background
        AddBackground(0, 0, 440, 280, 5120);

        // Title background
        AddBackground(20, 15, 400, 30, 5054);
        AddHtml(40, 22, 360, 20, "<basefont color=#0481>Beta Test Mode Active</basefont>", false, false);

        // Content background
        AddBackground(20, 55, 400, 170, 3000);

        // Content text
        AddHtml(40, 70, 360, 150,
            "<basefont color=#44>" +
            "Welcome to the Beta Test Phase!<br><br>" +
            "This will set ALL your skills and stats to maximum.<br>" +
            "You will receive starter items in your bank box.<br><br>" +
            "Thank you for testing and providing feedback!<br>" +
            "Enjoy your adventure, brave tester!" +
            "</basefont>",
            false, false);

        // Accept button (left) - Blue
        AddButton(180, 230, 4005, 4007, 1);
        AddHtml(220, 233, 100, 20, "<basefont color=#44>Accept</basefont>", false, false);

        // Deny button (right) - Red
        AddButton(280, 230, 4005, 4006, 0);
        AddHtml(320, 233, 100, 20, "<basefont color=#33>Deny</basefont>", false, false);
    }

    /// <summary>
    /// Handles button responses from the gump.
    /// </summary>
    /// <param name="sender">The NetState of the responding mobile.</param>
    /// <param name="info">Information about which button was clicked.</param>
    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (from == null)
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 0: // Deny
                from.SendMessage("You can always come back and use this stone later.");
                break;

            case 1: // Accept
                SetupBetaTester(from);
                break;
        }
    }

    /// <summary>
    /// Configures a mobile as a beta tester with maximum skills, stats, and starter items.
    /// </summary>
    /// <param name="from">The mobile to configure as a beta tester.</param>
    /// <remarks>
    /// This method performs the following actions:
    /// - Sets the BetaTester flag to prevent duplicate setups
    /// - Maximizes all skills to 100.0
    /// - Sets all stats (Str, Dex, Int) to 100
    /// - Fully restores health, stamina, and mana
    /// - Adds the following to the player's bank box:
    ///   * Weapons and armor (Katana, Plate Gorget, Plate Arms)
    ///   * 5000 gold
    ///   * Full spellbook with all spells
    ///   * Runebook
    ///   * Reagents bag (1000 of each reagent)
    ///   * Tools bag (crafting tools with charges)
    ///   * Resources bag (1000 ingots, logs, cloth, leather)
    ///   * Consumables bag (100 potions and scrolls)
    /// </remarks>
    private void SetupBetaTester(Mobile from)
    {
        // Mark as beta tester
        if (from is PlayerMobile pm)
        {
            pm.BetaTester = true;
        }

        // Set all skills to 100.0
        var skills = from.Skills;
        for (var i = 0; i < skills.Length; i++)
        {
            skills[i].Base = 100.0;
        }

        // Set stats to 100
        from.RawStr = 100;
        from.RawDex = 100;
        from.RawInt = 100;

        // Refresh stats
        from.Hits = from.HitsMax;
        from.Stam = from.StamMax;
        from.Mana = from.ManaMax;

        var bankBox = from.BankBox;

        if (bankBox != null)
        {
            // Add weapon
            var katana = new Katana();
            bankBox.AddItem(katana);

            // Add armor
            var gorget = new PlateGorget();
            var arms = new PlateArms();
            bankBox.AddItem(gorget);
            bankBox.AddItem(arms);

            // Add gold
            var gold = new Gold(5000);
            bankBox.AddItem(gold);

            // Add spellbook (all spells)
            var spellbook = new Spellbook
            {
                Content = 0xFFFFFFFFFFFFFFFF // All spells
            };
            bankBox.AddItem(spellbook);

            // Add runebook
            var runebook = new Runebook();
            bankBox.AddItem(runebook);

            // Add reagents bag
            var reagentBag = new Bag
            {
                Name = "Beta Tester Reagents"
            };

            // Add 1000 of each reagent
            reagentBag.AddItem(new BlackPearl(1000));
            reagentBag.AddItem(new Bloodmoss(1000));
            reagentBag.AddItem(new Garlic(1000));
            reagentBag.AddItem(new Ginseng(1000));
            reagentBag.AddItem(new MandrakeRoot(1000));
            reagentBag.AddItem(new Nightshade(1000));
            reagentBag.AddItem(new SulfurousAsh(1000));
            reagentBag.AddItem(new SpidersSilk(1000));

            bankBox.AddItem(reagentBag);

            // Add tools bag
            var toolsBag = new Bag
            {
                Name = "Beta Tester Tools"
            };

            toolsBag.AddItem(new SmithHammer(500));
            toolsBag.AddItem(new Tongs());
            toolsBag.AddItem(new Saw());
            toolsBag.AddItem(new Scissors());
            toolsBag.AddItem(new SewingKit(500));
            toolsBag.AddItem(new TinkerTools(500));

            bankBox.AddItem(toolsBag);

            // Add resources bag
            var resourcesBag = new Bag
            {
                Name = "Beta Tester Resources"
            };

            resourcesBag.AddItem(new IronIngot(1000));
            resourcesBag.AddItem(new Log(1000));
            resourcesBag.AddItem(new Cloth(1000));
            resourcesBag.AddItem(new Leather(1000));

            bankBox.AddItem(resourcesBag);

            // Add consumables bag
            var consumablesBag = new Bag
            {
                Name = "Beta Tester Consumables"
            };

            // Add potions - create multiple individual items
            for (int i = 0; i < 100; i++)
            {
                consumablesBag.AddItem(new GreaterHealPotion());
            }
            for (int i = 0; i < 100; i++)
            {
                consumablesBag.AddItem(new HealPotion());
            }
            for (int i = 0; i < 100; i++)
            {
                consumablesBag.AddItem(new TotalRefreshPotion());
            }

            // Add scrolls - create multiple individual items
            for (int i = 0; i < 100; i++)
            {
                consumablesBag.AddItem(new LightningScroll());
            }
            for (int i = 0; i < 100; i++)
            {
                consumablesBag.AddItem(new GreaterHealScroll());
            }
            for (int i = 0; i < 100; i++)
            {
                consumablesBag.AddItem(new FlamestrikeScroll());
            }
            for (int i = 0; i < 100; i++)
            {
                consumablesBag.AddItem(new MagicReflectScroll());
            }

            bankBox.AddItem(consumablesBag);
        }

        from.SendMessage(0x44, "All skills set to maximum and stats boosted!");
        from.SendMessage(0x44, "Check your bank box for starter items!");
    }
}
