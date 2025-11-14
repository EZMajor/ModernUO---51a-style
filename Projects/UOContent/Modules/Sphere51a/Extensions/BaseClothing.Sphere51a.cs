using Server.Modules.Sphere51a.Configuration;
using Server.Modules.Sphere51a.Extensions;

namespace Server.Items;

/// <summary>
/// Sphere 51a extension for BaseClothing to enable double-click equip behavior.
/// </summary>
public partial class BaseClothing
{
    /// <summary>
    /// Handles double-click on clothing items.
    /// When Sphere51a is enabled, allows equipping from backpack, ground, or containers.
    /// </summary>
    /// <param name="from">The mobile double-clicking the item</param>
    public override void OnDoubleClick(Mobile from)
    {
        // If Sphere51a is not enabled, use default behavior
        if (!SphereConfiguration.Enabled)
        {
            base.OnDoubleClick(from);
            return;
        }

        // Check if item is already equipped
        if (Parent == from && from.FindItemOnLayer(Layer) == this)
        {
            // Already equipped - do nothing (standard UO behavior)
            return;
        }

        // Attempt Sphere-style equip
        EquipmentHelper.TryEquipItem(from, this);
    }
}
