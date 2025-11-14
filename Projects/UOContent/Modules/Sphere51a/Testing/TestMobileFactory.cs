/*************************************************************************
 * ModernUO - Sphere 51a Test Mobile Factory
 * File: TestMobileFactory.cs
 *
 * Description: Creates synthetic mobiles for headless testing.
 *              Generates test entities with configurable stats without persistence.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Modules.Sphere51a.Testing;

/// <summary>
/// Factory for creating synthetic test mobiles with specific stats.
/// </summary>
public static class TestMobileFactory
{
    /// <summary>
    /// Creates a test combatant mobile with specified stats and weapon.
    /// </summary>
    public static Mobile CreateCombatant(
        string name,
        int str = 100,
        int dex = 100,
        int intel = 100,
        int hits = 100,
        BaseWeapon weapon = null,
        Point3D location = default
    )
    {
        if (location == default)
            location = new Point3D(1000, 1000, 0); // Test location

        var mobile = new TestMobile
        {
            Name = name ?? "TestCombatant",
            Body = 0x190, // Human male
            Hue = 0,
            Location = location,
            Map = Map.Felucca, // Use Felucca for testing

            // Stats
            RawStr = str,
            RawDex = dex,
            RawInt = intel,

            Hits = hits,
            Stam = dex,
            Mana = intel,

            // Flags
            CantWalk = false,
            Frozen = false
        };

        // Set skills after construction
        mobile.Skills.Wrestling.Base = 100.0;
        mobile.Skills.Tactics.Base = 100.0;
        mobile.Skills.Anatomy.Base = 100.0;
        mobile.Skills.Swords.Base = 100.0;
        mobile.Skills.Macing.Base = 100.0;
        mobile.Skills.Fencing.Base = 100.0;
        mobile.Skills.Magery.Base = 100.0;
        mobile.Skills.EvalInt.Base = 100.0;
        mobile.Skills.Meditation.Base = 100.0;
        mobile.Skills.MagicResist.Base = 100.0;

        // Equip weapon if provided
        if (weapon != null)
        {
            mobile.AddItem(weapon);
            mobile.EquipItem(weapon);
        }

        // Give reagents for magic testing
        GiveReagents(mobile);

        // Note: Mobiles are automatically added to world on construction

        return mobile;
    }

    /// <summary>
    /// Creates a test spellcaster mobile.
    /// </summary>
    public static Mobile CreateSpellcaster(
        string name,
        int intel = 100,
        Point3D location = default
    )
    {
        var mobile = CreateCombatant(name, 50, 50, intel, 100, null, location);

        // Boost magic skills
        mobile.Skills.Magery.Base = 120.0;
        mobile.Skills.EvalInt.Base = 120.0;
        mobile.Skills.Meditation.Base = 120.0;

        return mobile;
    }

    /// <summary>
    /// Creates a test dummy (stationary target).
    /// </summary>
    public static Mobile CreateDummy(string name = "TestDummy", Point3D location = default)
    {
        if (location == default)
            location = new Point3D(1001, 1000, 0);

        var mobile = new TestMobile
        {
            Name = name,
            Body = 0x190,
            Location = location,
            Map = Map.Felucca,

            RawStr = 100,
            RawDex = 100,
            RawInt = 100,
            Hits = 1000, // High HP so it doesn't die

            CantWalk = true,
            Frozen = true,
            Blessed = true // Can't be killed
        };

        // Note: Mobile automatically added to world on construction
        return mobile;
    }

    /// <summary>
    /// Creates a specific weapon by type name.
    /// </summary>
    public static BaseWeapon CreateWeapon(string weaponType)
    {
        BaseWeapon weapon = weaponType.ToLowerInvariant() switch
        {
            "katana" => new Katana(),
            "longsword" => new Longsword(),
            "broadsword" => new Broadsword(),
            "waraxe" => new WarAxe(),
            "dagger" => new Dagger(),
            "warhammer" => new WarHammer(),
            "mace" => new Mace(),
            "kryss" => new Kryss(),
            "halberd" => new Halberd(),
            "spear" => new Spear(),
            _ => new Longsword() // Default
        };

        // Ensure weapon is in valid state
        weapon.Identified = true;

        return weapon;
    }

    /// <summary>
    /// Gives a mobile all reagents needed for spell testing.
    /// </summary>
    private static void GiveReagents(Mobile mobile)
    {
        if (mobile.Backpack == null)
        {
            mobile.AddItem(new Backpack());
        }

        // Give 1000 of each reagent
        mobile.Backpack.DropItem(new BlackPearl(1000));
        mobile.Backpack.DropItem(new Bloodmoss(1000));
        mobile.Backpack.DropItem(new Garlic(1000));
        mobile.Backpack.DropItem(new Ginseng(1000));
        mobile.Backpack.DropItem(new MandrakeRoot(1000));
        mobile.Backpack.DropItem(new Nightshade(1000));
        mobile.Backpack.DropItem(new SulfurousAsh(1000));
        mobile.Backpack.DropItem(new SpidersSilk(1000));
    }

    /// <summary>
    /// Cleans up a test mobile (removes from world).
    /// </summary>
    public static void Cleanup(Mobile mobile)
    {
        if (mobile == null || mobile.Deleted)
            return;

        try
        {
            // Remove all items
            if (mobile.Backpack != null)
            {
                mobile.Backpack.Delete();
            }

            var items = mobile.Items;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                items[i].Delete();
            }

            // Delete the mobile
            mobile.Delete();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cleaning up test mobile {mobile.Name}: {ex.Message}");
        }
    }
}

/// <summary>
/// Custom mobile class for testing purposes.
/// Overrides behavior to prevent persistence and simplify cleanup.
/// </summary>
public class TestMobile : PlayerMobile
{
    public TestMobile() : base()
    {
        // Prevent any persistence - test mobiles should not save
    }

    // Note: OnThink removed as it's not a virtual method in PlayerMobile

    /// <summary>
    /// Prevent death for test mobiles (unless explicitly allowed).
    /// </summary>
    public override bool OnBeforeDeath()
    {
        if (Blessed)
        {
            Hits = HitsMax;
            return false; // Prevent death
        }

        return base.OnBeforeDeath();
    }

    /// <summary>
    /// Override serialization to prevent any writes.
    /// </summary>
    public override void Serialize(IGenericWriter writer)
    {
        // Do nothing - test mobiles are never serialized
    }

    /// <summary>
    /// Override deserialization (should never be called).
    /// </summary>
    public TestMobile(Serial serial) : base(serial)
    {
    }

    public override void Deserialize(IGenericReader reader)
    {
        // Do nothing - test mobiles are never deserialized
    }
}
