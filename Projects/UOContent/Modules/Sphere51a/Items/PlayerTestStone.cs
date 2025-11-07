using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Mobiles
{
    /// <summary>
    /// Partial class extension for PlayerMobile to add PlayerTester property.
    /// </summary>
    public partial class PlayerMobile
    {
        /// <summary>
        /// Gets or sets a value indicating whether this player has used the player test stone.
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public bool PlayerTester { get; set; }
    }
}

namespace Server.Items
{

/// <summary>
/// Player Test Stone - An interactive stone that grants players beta tester status with maximum skills, stats, and starter items.
/// </summary>
/// <remarks>
/// When double-clicked by a player, this stone presents a confirmation gump. Upon acceptance:
/// - Sets all skills to 100.0 (maximum)
/// - Sets all stats (Str, Dex, Int) to 100
/// - Provides starter equipment and resources in the player's bank box
/// - Marks the player with the PlayerTester flag to prevent duplicate setups
///
/// The stone is non-movable and uses an orange-colored gravestone appearance.
/// </remarks>
[SerializationGenerator(0)]
public partial class PlayerTestStone : Item
{
    /// <summary>
    /// Initializes a new instance of the PlayerTestStone class.
    /// </summary>
    [Constructible]
    public PlayerTestStone() : base(0xED4) // Gravestone ItemID
    {
        Movable = false;
        Hue = 0x2D; // Orange color
    }

    /// <summary>
    /// Gets the default name displayed for this stone.
    /// </summary>
    public override string DefaultName => "Player Test Stone";

    /// <summary>
    /// Handles double-click interaction with the stone.
    /// </summary>
    /// <param name="from">The mobile double-clicking the stone.</param>
    /// <remarks>
    /// Only players within range can interact with the stone.
    /// Players who have already used the stone (PlayerTester flag set) will receive a message and cannot use it again.
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

        // Check if already a player tester
        var pm = from as PlayerMobile;
        if (pm != null && pm.PlayerTester)
        {
            from.SendMessage(0x44, "You are already a Player Tester.");
            return;
        }

        from.SendGump(new PlayerTestStoneGump(from, this));
    }
}

/// <summary>
/// Gump displayed when a player interacts with the Player Test Stone.
/// </summary>
/// <remarks>
/// Presents two options:
/// - Accept: Activates beta tester status and grants all bonuses
/// - Deny: Closes the gump without changes
/// </remarks>
public class PlayerTestStoneGump : Gump
{
    private readonly PlayerTestStone _stone;

    /// <summary>
    /// Initializes a new instance of the PlayerTestStoneGump class.
    /// </summary>
    /// <param name="from">The mobile viewing the gump.</param>
    /// <param name="stone">The Player Test Stone that spawned this gump.</param>
    public PlayerTestStoneGump(Mobile from, PlayerTestStone stone) : base(50, 50)
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
                SetupPlayerTester(from);
                break;
        }
    }

    /// <summary>
    /// Configures a mobile as a player tester with maximum skills, stats, and starter items.
    /// </summary>
    /// <param name="from">The mobile to configure as a player tester.</param>
    /// <remarks>
    /// This method performs the following actions:
    /// - Sets the PlayerTester flag to prevent duplicate setups
    /// - Maximizes all skills to 100.0
    /// - Sets all stats (Str, Dex, Int) to 100
    /// - Fully restores health, stamina, and mana
    /// - Adds the following to the player's bank box:
    ///   * Weapon (Katana)
    ///   * Armor bag (full plate suit + Heater Shield)
    ///   * 5000 gold
    ///   * Full spellbook with all spells
    ///   * Runebook
    ///   * Reagents bag (1000 of each reagent)
    ///   * Tools bag (crafting tools with charges)
    ///   * Resources bag (1000 ingots, logs, cloth, leather)
    ///   * Consumables bag (100 heal potions, 100 refresh potions, 400 scrolls - all stacked)
    /// </remarks>
    private void SetupPlayerTester(Mobile from)
    {
        // Mark as player tester
        if (from is PlayerMobile pm)
        {
            pm.PlayerTester = true;
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

            // Add full plate armor suit in its own bag
            var armorBag = new Bag
            {
                Name = "Beta Tester Armor"
            };

            armorBag.AddItem(new PlateHelm());
            armorBag.AddItem(new PlateGorget());
            armorBag.AddItem(new PlateArms());
            armorBag.AddItem(new PlateGloves());
            armorBag.AddItem(new PlateChest());
            armorBag.AddItem(new PlateLegs());
            armorBag.AddItem(new HeaterShield());

            bankBox.AddItem(armorBag);

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

            // Add potions - stacked
            var healPotions = new HealPotion();
            healPotions.Amount = 100;
            consumablesBag.AddItem(healPotions);

            var refreshPotions = new TotalRefreshPotion();
            refreshPotions.Amount = 100;
            consumablesBag.AddItem(refreshPotions);

            // Add scrolls - stacked
            var lightningScrolls = new LightningScroll();
            lightningScrolls.Amount = 100;
            consumablesBag.AddItem(lightningScrolls);

            var greaterHealScrolls = new GreaterHealScroll();
            greaterHealScrolls.Amount = 100;
            consumablesBag.AddItem(greaterHealScrolls);

            var flamestrikeScrolls = new FlamestrikeScroll();
            flamestrikeScrolls.Amount = 100;
            consumablesBag.AddItem(flamestrikeScrolls);

            var magicReflectScrolls = new MagicReflectScroll();
            magicReflectScrolls.Amount = 100;
            consumablesBag.AddItem(magicReflectScrolls);

            bankBox.AddItem(consumablesBag);
        }

        from.SendMessage(0x44, "All skills set to maximum and stats boosted!");
        from.SendMessage(0x44, "Check your bank box for starter items!");
    }
}
}
