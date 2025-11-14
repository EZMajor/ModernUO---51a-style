using Server.Items;
using Server.Mobiles;
using Server.Modules.Sphere51a.Configuration;

namespace Server.Modules.Sphere51a.Extensions;

/// <summary>
/// Helper class for Sphere 51a equipment double-click behavior.
/// Handles equip/unequip logic with layer conflict resolution and overweight handling.
/// </summary>
public static class EquipmentHelper
{
    /// <summary>
    /// Attempts to equip an item using Sphere 51a mechanics.
    /// Handles accessibility checks, layer conflicts, and two-handed weapon logic.
    /// </summary>
    /// <param name="from">The mobile attempting to equip the item</param>
    /// <param name="item">The item to equip</param>
    /// <returns>True if successfully equipped, false otherwise</returns>
    public static bool TryEquipItem(Mobile from, Item item)
    {
        if (from == null || item == null || item.Deleted)
            return false;

        // Check if Sphere51a is enabled
        if (!SphereConfiguration.Enabled)
            return false;

        // Check if item is already equipped on this mobile
        if (item.Parent == from && from.FindItemOnLayer(item.Layer) == item)
        {
            // Already equipped - do nothing (standard UO behavior)
            return false;
        }

        // Check accessibility (range, container access, etc.)
        if (!IsAccessible(from, item))
        {
            from.SendLocalizedMessage(500295); // You are too far away to do that.
            return false;
        }

        // Resolve layer conflicts (unequip existing item in same slot)
        if (!ResolveLayerConflict(from, item.Layer, item))
            return false; // Failed to clear the slot

        // Resolve two-handed weapon conflicts
        if (!ResolveTwoHandedConflict(from, item))
            return false; // Failed to resolve conflicts

        // Attempt to equip using standard Mobile.EquipItem
        // This will call CanEquip and handle all validation (str, race, etc.)
        return from.EquipItem(item);
    }

    /// <summary>
    /// Checks if an item is accessible to a mobile for equipping.
    /// Item must be in backpack, on ground within range, or in an accessible container.
    /// </summary>
    /// <param name="from">The mobile checking accessibility</param>
    /// <param name="item">The item to check</param>
    /// <returns>True if accessible, false otherwise</returns>
    public static bool IsAccessible(Mobile from, Item item)
    {
        if (from == null || item == null || item.Deleted)
            return false;

        // Check if item is a child of the mobile (in backpack or equipped)
        if (item.IsChildOf(from))
            return true;

        // Check if item is on the ground within range (2 tiles)
        if (item.Parent == null)
        {
            return from.InRange(item.GetWorldLocation(), 2);
        }

        // Check if item is in an accessible container
        var root = item.RootParent;

        // If root is the mobile themselves, already handled above
        if (root == from)
            return true;

        // If root is a container, check if it's accessible
        if (root is Container container)
        {
            // Container must be accessible and within range
            return container.IsAccessibleTo(from) &&
                   from.InRange(container.GetWorldLocation(), 2);
        }

        // If root is another mobile, the item is not accessible
        if (root is Mobile)
            return false;

        // Item is in an inaccessible location
        return false;
    }

    /// <summary>
    /// Unequips an item to the mobile's backpack.
    /// If backpack is full or mobile is overweight, drops item to ground.
    /// </summary>
    /// <param name="from">The mobile to unequip from</param>
    /// <param name="item">The item to unequip</param>
    /// <returns>True if successfully handled (backpack or ground), false on error</returns>
    public static bool UnequipToBackpack(Mobile from, Item item)
    {
        if (from == null || item == null || item.Deleted)
            return false;

        var backpack = from.Backpack;

        // Try to add to backpack first
        if (backpack != null && backpack.CheckHold(from, item, false, true))
        {
            backpack.DropItem(item);
            return true;
        }

        // Backpack full or overweight - drop to ground
        item.MoveToWorld(from.Location, from.Map);
        from.SendMessage("You are overweight. The item has been dropped at your feet.");
        return true;
    }

    /// <summary>
    /// Resolves layer conflicts by unequipping any existing item in the same layer.
    /// </summary>
    /// <param name="from">The mobile to check</param>
    /// <param name="layer">The layer to clear</param>
    /// <param name="newItem">The new item being equipped (to avoid unequipping itself)</param>
    /// <returns>True if layer is clear or successfully cleared, false on failure</returns>
    public static bool ResolveLayerConflict(Mobile from, Layer layer, Item newItem)
    {
        if (from == null || layer == Layer.Invalid)
            return true;

        var existing = from.FindItemOnLayer(layer);

        // No conflict if nothing equipped or it's the same item
        if (existing == null || existing == newItem)
            return true;

        // Unequip existing item to backpack (or ground if overweight)
        return UnequipToBackpack(from, existing);
    }

    /// <summary>
    /// Resolves two-handed weapon conflicts.
    /// When equipping a 2H weapon, unequips 1H weapon and shield.
    /// When equipping a 1H weapon, unequips conflicting 2H weapon.
    /// Shields are exempt from 2H weapon rules.
    /// </summary>
    /// <param name="from">The mobile equipping the item</param>
    /// <param name="newItem">The item being equipped</param>
    /// <returns>True if conflicts resolved successfully, false on failure</returns>
    public static bool ResolveTwoHandedConflict(Mobile from, Item newItem)
    {
        if (from == null || newItem == null)
            return false;

        var layer = newItem.Layer;

        // Equipping a two-handed weapon (but not a shield)
        if (layer == Layer.TwoHanded && newItem is not BaseShield)
        {
            // Unequip one-handed weapon if present
            var oneHanded = from.FindItemOnLayer(Layer.OneHanded);
            if (oneHanded != null && !UnequipToBackpack(from, oneHanded))
                return false;

            // Unequip shield if present
            var shield = from.FindItemOnLayer(Layer.TwoHanded);
            if (shield != null && shield != newItem && !UnequipToBackpack(from, shield))
                return false;
        }
        // Equipping a one-handed weapon
        else if (layer == Layer.OneHanded)
        {
            // Check for conflicting two-handed weapon (not shield)
            var twoHanded = from.FindItemOnLayer(Layer.TwoHanded);
            if (twoHanded is BaseWeapon && !UnequipToBackpack(from, twoHanded))
                return false;
        }
        // Equipping a shield
        else if (layer == Layer.TwoHanded && newItem is BaseShield)
        {
            // Check for conflicting two-handed weapon
            var twoHanded = from.FindItemOnLayer(Layer.TwoHanded);
            if (twoHanded is BaseWeapon && twoHanded != newItem && !UnequipToBackpack(from, twoHanded))
                return false;
        }

        return true;
    }
}
