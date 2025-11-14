using Server;
using Server.Items;
using Server.Mobiles;
using Server.Modules.Sphere51a.Configuration;
using Server.Modules.Sphere51a.Extensions;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Modules.Sphere51a;

[Collection("Sequential")]
public class EquipmentIntegrationTests : IClassFixture<ServerFixture>
{
    [Fact]
    public void Test_EquipArmor_FromBackpack()
    {
        // Arrange
        SphereConfiguration.Enabled = true;
        var mobile = new PlayerMobile();
        var map = Map.Felucca;
        mobile.MoveToWorld(new Point3D(1000, 1000, 0), map);

        var plateChest = new PlateChest();
        mobile.Backpack?.AddItem(plateChest);

        // Act
        var result = EquipmentHelper.TryEquipItem(mobile, plateChest);

        // Assert
        Assert.True(result, "Equipment should succeed from backpack");
        Assert.Equal(mobile, plateChest.Parent);
        Assert.Equal(plateChest, mobile.FindItemOnLayer(Layer.InnerTorso));

        // Cleanup
        mobile.Delete();
        SphereConfiguration.Enabled = false;
    }

    [Fact]
    public void Test_EquipWeapon_FromGround_WithinRange()
    {
        // Arrange
        SphereConfiguration.Enabled = true;
        var mobile = new PlayerMobile();
        var map = Map.Felucca;
        var location = new Point3D(1000, 1000, 0);
        mobile.MoveToWorld(location, map);

        var longsword = new Longsword();
        longsword.MoveToWorld(location, map); // Same location as mobile

        // Act
        var result = EquipmentHelper.TryEquipItem(mobile, longsword);

        // Assert
        Assert.True(result, "Equipment should succeed from ground within range");
        Assert.Equal(mobile, longsword.Parent);
        Assert.Equal(longsword, mobile.FindItemOnLayer(Layer.OneHanded));

        // Cleanup
        mobile.Delete();
        SphereConfiguration.Enabled = false;
    }

    [Fact]
    public void Test_EquipArmor_FromGround_OutOfRange()
    {
        // Arrange
        SphereConfiguration.Enabled = true;
        var mobile = new PlayerMobile();
        var map = Map.Felucca;
        mobile.MoveToWorld(new Point3D(1000, 1000, 0), map);

        var plateChest = new PlateChest();
        plateChest.MoveToWorld(new Point3D(1005, 1005, 0), map); // 5+ tiles away

        // Act
        var result = EquipmentHelper.TryEquipItem(mobile, plateChest);

        // Assert
        Assert.False(result, "Equipment should fail when out of range");
        Assert.Null(plateChest.Parent);

        // Cleanup
        mobile.Delete();
        plateChest.Delete();
        SphereConfiguration.Enabled = false;
    }

    [Fact]
    public void Test_EquipArmor_AutoReplace_Existing()
    {
        // Arrange
        SphereConfiguration.Enabled = true;
        var mobile = new PlayerMobile();
        var map = Map.Felucca;
        mobile.MoveToWorld(new Point3D(1000, 1000, 0), map);

        var oldHelm = new CloseHelm();
        var newHelm = new PlateHelm();

        mobile.EquipItem(oldHelm); // Equip first helmet
        mobile.Backpack?.AddItem(newHelm);

        // Act
        var result = EquipmentHelper.TryEquipItem(mobile, newHelm);

        // Assert
        Assert.True(result, "New helmet should replace old helmet");
        Assert.Equal(newHelm, mobile.FindItemOnLayer(Layer.Helm));
        Assert.Equal(mobile.Backpack, oldHelm.Parent);

        // Cleanup
        mobile.Delete();
        SphereConfiguration.Enabled = false;
    }

    [Fact]
    public void Test_EquipTwoHandedWeapon_AutoUnequip_OneHandedAndShield()
    {
        // Arrange
        SphereConfiguration.Enabled = true;
        var mobile = new PlayerMobile();
        var map = Map.Felucca;
        mobile.MoveToWorld(new Point3D(1000, 1000, 0), map);

        var longsword = new Longsword();
        var shield = new MetalShield();
        var bardiche = new Bardiche(); // Two-handed weapon

        mobile.EquipItem(longsword);
        mobile.EquipItem(shield);
        mobile.Backpack?.AddItem(bardiche);

        // Act
        var result = EquipmentHelper.TryEquipItem(mobile, bardiche);

        // Assert
        Assert.True(result, "Two-handed weapon should equip");
        Assert.Equal(bardiche, mobile.FindItemOnLayer(Layer.TwoHanded));
        Assert.Null(mobile.FindItemOnLayer(Layer.OneHanded)); // Longsword unequipped
        Assert.Equal(mobile.Backpack, longsword.Parent);
        Assert.Equal(mobile.Backpack, shield.Parent);

        // Cleanup
        mobile.Delete();
        SphereConfiguration.Enabled = false;
    }

    [Fact]
    public void Test_EquipShield_WithOneHandedWeapon_Allowed()
    {
        // Arrange
        SphereConfiguration.Enabled = true;
        var mobile = new PlayerMobile();
        var map = Map.Felucca;
        mobile.MoveToWorld(new Point3D(1000, 1000, 0), map);

        var longsword = new Longsword();
        var shield = new MetalShield();

        mobile.EquipItem(longsword);
        mobile.Backpack?.AddItem(shield);

        // Act
        var result = EquipmentHelper.TryEquipItem(mobile, shield);

        // Assert
        Assert.True(result, "Shield should equip with one-handed weapon");
        Assert.Equal(longsword, mobile.FindItemOnLayer(Layer.OneHanded));
        Assert.Equal(shield, mobile.FindItemOnLayer(Layer.TwoHanded));

        // Cleanup
        mobile.Delete();
        SphereConfiguration.Enabled = false;
    }

    [Fact]
    public void Test_EquipOneHandedWeapon_AutoUnequip_TwoHandedWeapon()
    {
        // Arrange
        SphereConfiguration.Enabled = true;
        var mobile = new PlayerMobile();
        var map = Map.Felucca;
        mobile.MoveToWorld(new Point3D(1000, 1000, 0), map);

        var bardiche = new Bardiche(); // Two-handed weapon
        var longsword = new Longsword();

        mobile.EquipItem(bardiche);
        mobile.Backpack?.AddItem(longsword);

        // Act
        var result = EquipmentHelper.TryEquipItem(mobile, longsword);

        // Assert
        Assert.True(result, "One-handed weapon should equip");
        Assert.Equal(longsword, mobile.FindItemOnLayer(Layer.OneHanded));
        Assert.Null(mobile.FindItemOnLayer(Layer.TwoHanded));
        Assert.Equal(mobile.Backpack, bardiche.Parent);

        // Cleanup
        mobile.Delete();
        SphereConfiguration.Enabled = false;
    }

    [Fact]
    public void Test_EquipClothing_FromBackpack()
    {
        // Arrange
        SphereConfiguration.Enabled = true;
        var mobile = new PlayerMobile();
        var map = Map.Felucca;
        mobile.MoveToWorld(new Point3D(1000, 1000, 0), map);

        var robe = new Robe();
        mobile.Backpack?.AddItem(robe);

        // Act
        var result = EquipmentHelper.TryEquipItem(mobile, robe);

        // Assert
        Assert.True(result, "Clothing should equip from backpack");
        Assert.Equal(mobile, robe.Parent);

        // Cleanup
        mobile.Delete();
        SphereConfiguration.Enabled = false;
    }

    [Fact]
    public void Test_EquipJewelry_Ring_FromBackpack()
    {
        // Arrange
        SphereConfiguration.Enabled = true;
        var mobile = new PlayerMobile();
        var map = Map.Felucca;
        mobile.MoveToWorld(new Point3D(1000, 1000, 0), map);

        var ring = new GoldRing();
        mobile.Backpack?.AddItem(ring);

        // Act
        var result = EquipmentHelper.TryEquipItem(mobile, ring);

        // Assert
        Assert.True(result, "Ring should equip from backpack");
        Assert.Equal(mobile, ring.Parent);
        Assert.Equal(ring, mobile.FindItemOnLayer(Layer.Ring));

        // Cleanup
        mobile.Delete();
        SphereConfiguration.Enabled = false;
    }

    [Fact]
    public void Test_EquipJewelry_ReplaceExisting()
    {
        // Arrange
        SphereConfiguration.Enabled = true;
        var mobile = new PlayerMobile();
        var map = Map.Felucca;
        mobile.MoveToWorld(new Point3D(1000, 1000, 0), map);

        var oldRing = new GoldRing();
        var newRing = new SilverRing();

        mobile.EquipItem(oldRing);
        mobile.Backpack?.AddItem(newRing);

        // Act
        var result = EquipmentHelper.TryEquipItem(mobile, newRing);

        // Assert
        Assert.True(result, "New ring should replace old ring");
        Assert.Equal(newRing, mobile.FindItemOnLayer(Layer.Ring));
        Assert.Equal(mobile.Backpack, oldRing.Parent);

        // Cleanup
        mobile.Delete();
        SphereConfiguration.Enabled = false;
    }

    [Fact]
    public void Test_EquipFromContainer_Accessible()
    {
        // Arrange
        SphereConfiguration.Enabled = true;
        var mobile = new PlayerMobile();
        var map = Map.Felucca;
        mobile.MoveToWorld(new Point3D(1000, 1000, 0), map);

        var pouch = new Bag();
        var plateChest = new PlateChest();

        mobile.Backpack?.AddItem(pouch);
        pouch.AddItem(plateChest);

        // Act
        var result = EquipmentHelper.TryEquipItem(mobile, plateChest);

        // Assert
        Assert.True(result, "Equipment from nested container should succeed");
        Assert.Equal(mobile, plateChest.Parent);

        // Cleanup
        mobile.Delete();
        SphereConfiguration.Enabled = false;
    }

    [Fact]
    public void Test_Overweight_UnequipToGround()
    {
        // Arrange
        SphereConfiguration.Enabled = true;
        var mobile = new PlayerMobile
        {
            Str = 10 // Very low strength for easy overweight
        };
        var map = Map.Felucca;
        var location = new Point3D(1000, 1000, 0);
        mobile.MoveToWorld(location, map);

        // Fill backpack with heavy items to make mobile overweight
        for (int i = 0; i < 50; i++)
        {
            mobile.Backpack?.AddItem(new PlateChest());
        }

        var oldHelm = new PlateHelm();
        var newHelm = new CloseHelm();

        mobile.EquipItem(oldHelm);
        mobile.Backpack?.AddItem(newHelm);

        // Act
        var result = EquipmentHelper.TryEquipItem(mobile, newHelm);

        // Assert
        Assert.True(result, "New helmet should still equip");
        Assert.Equal(newHelm, mobile.FindItemOnLayer(Layer.Helm));

        // Old helmet should be dropped to ground (not in backpack)
        Assert.NotEqual(mobile.Backpack, oldHelm.Parent);
        Assert.Null(oldHelm.Parent); // On ground
        Assert.Equal(location, oldHelm.Location);

        // Cleanup
        mobile.Delete();
        oldHelm.Delete();
        SphereConfiguration.Enabled = false;
    }

    [Fact]
    public void Test_SphereDisabled_NoEquip()
    {
        // Arrange
        SphereConfiguration.Enabled = false;
        var mobile = new PlayerMobile();
        var map = Map.Felucca;
        mobile.MoveToWorld(new Point3D(1000, 1000, 0), map);

        var plateChest = new PlateChest();
        mobile.Backpack?.AddItem(plateChest);

        // Act
        var result = EquipmentHelper.TryEquipItem(mobile, plateChest);

        // Assert
        Assert.False(result, "Equipment should fail when Sphere51a is disabled");
        Assert.NotEqual(mobile, plateChest.Parent);

        // Cleanup
        mobile.Delete();
    }

    [Fact]
    public void Test_IsAccessible_BackpackItem()
    {
        // Arrange
        var mobile = new PlayerMobile();
        var item = new PlateChest();
        mobile.Backpack?.AddItem(item);

        // Act
        var result = EquipmentHelper.IsAccessible(mobile, item);

        // Assert
        Assert.True(result, "Item in backpack should be accessible");

        // Cleanup
        mobile.Delete();
    }

    [Fact]
    public void Test_IsAccessible_GroundItemWithinRange()
    {
        // Arrange
        var mobile = new PlayerMobile();
        var map = Map.Felucca;
        var location = new Point3D(1000, 1000, 0);
        mobile.MoveToWorld(location, map);

        var item = new PlateChest();
        item.MoveToWorld(new Point3D(1001, 1001, 0), map); // 1 tile away

        // Act
        var result = EquipmentHelper.IsAccessible(mobile, item);

        // Assert
        Assert.True(result, "Item on ground within 2 tiles should be accessible");

        // Cleanup
        mobile.Delete();
        item.Delete();
    }

    [Fact]
    public void Test_IsAccessible_GroundItemOutOfRange()
    {
        // Arrange
        var mobile = new PlayerMobile();
        var map = Map.Felucca;
        mobile.MoveToWorld(new Point3D(1000, 1000, 0), map);

        var item = new PlateChest();
        item.MoveToWorld(new Point3D(1010, 1010, 0), map); // Far away

        // Act
        var result = EquipmentHelper.IsAccessible(mobile, item);

        // Assert
        Assert.False(result, "Item on ground far away should not be accessible");

        // Cleanup
        mobile.Delete();
        item.Delete();
    }

    [Fact]
    public void Test_UnequipToBackpack_Success()
    {
        // Arrange
        var mobile = new PlayerMobile();
        var map = Map.Felucca;
        mobile.MoveToWorld(new Point3D(1000, 1000, 0), map);

        var helm = new PlateHelm();
        mobile.EquipItem(helm);

        // Act
        var result = EquipmentHelper.UnequipToBackpack(mobile, helm);

        // Assert
        Assert.True(result, "Unequip to backpack should succeed");
        Assert.Equal(mobile.Backpack, helm.Parent);

        // Cleanup
        mobile.Delete();
    }
}
