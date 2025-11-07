/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: SphereDuelStoneComponent.cs
 *
 * Description: Duel stone component for Sphere 51a mechanics.
 *              Extends base DuelStoneComponent with Sphere configuration.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Logging;
using Server.Network;
using Server.Engines.DuelArena;
using Server.Modules.Sphere51a.Configuration;

namespace Server.Modules.Sphere51a.DuelArena;

/// <summary>
/// Duel stone component that supports Sphere 51a combat mechanics.
/// Extends the base DuelStoneComponent with Sphere-specific configuration.
/// </summary>
[SerializationGenerator(0)]
public partial class SphereDuelStoneComponent : DuelStoneComponent
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(SphereDuelStoneComponent));

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _useSphereMechanics = true;

    /// <summary>
    /// Whether this duel stone uses Sphere 51a mechanics.
    /// </summary>
    public bool SphereMechanicsEnabled
    {
        get => _useSphereMechanics && SphereConfiguration.Enabled;
        set => _useSphereMechanics = value;
    }

    /// <summary>
    /// Gets the display name for this duel stone.
    /// </summary>
    public override string DefaultName => SphereMechanicsEnabled
        ? "Sphere Duel Stone"
        : "Duel Stone";

    [Constructible]
    public SphereDuelStoneComponent() : base()
    {
        Hue = 0x0AD6; // Custom color for Sphere stones
        Name = DefaultName;
    }

    /// <summary>
    /// Handles double-click interaction with the Sphere duel stone.
    /// </summary>
    public override void OnDoubleClick(Mobile from)
    {
        if (from?.AccessLevel >= AccessLevel.GameMaster)
        {
            // GMs get configuration options
            from.SendGump(new SphereDuelConfigGump(this));
            return;
        }

        // Players get the standard duel interface
        base.OnDoubleClick(from);
    }



    /// <summary>
    /// Gets the single-click label for this duel stone.
    /// </summary>
    public override void OnSingleClick(Mobile from)
    {
        if (SphereMechanicsEnabled)
        {
            if (Type == DuelType.Money1v1)
            {
                LabelTo(from, $"[Sphere] 1vs1 for [{EntryCost}]");
            }
            else if (Type == DuelType.Money2v2)
            {
                LabelTo(from, $"[Sphere] 2vs2 for [{EntryCost}]");
            }
            else if (Type == DuelType.Loot1v1)
            {
                LabelTo(from, "[Sphere] 1vs1 for [loot]");
            }
            else if (Type == DuelType.Loot2v2)
            {
                LabelTo(from, "[Sphere] 2vs2 for [loot]");
            }
            else
            {
                LabelTo(from, "[Sphere] Stone error, page a GM!");
            }
        }
        else
        {
            base.OnSingleClick(from);
        }
    }
}

/// <summary>
/// Configuration gump for Sphere duel stones.
/// Allows GMs to configure Sphere mechanics settings.
/// </summary>
public class SphereDuelConfigGump : Gump
{
    private readonly SphereDuelStoneComponent _stone;

    public SphereDuelConfigGump(SphereDuelStoneComponent stone) : base(50, 50)
    {
        _stone = stone;

        Closable = true;
        Disposable = true;
        Draggable = true;
        Resizable = false;

        AddPage(0);

        // Main background
        AddBackground(0, 0, 400, 300, 5120);

        // Title
        AddBackground(20, 15, 360, 30, 5054);
        AddHtml(40, 22, 320, 20, "<basefont color=#0481>Sphere Duel Stone Configuration</basefont>", false, false);

        // Sphere mechanics toggle
        AddBackground(20, 55, 360, 60, 3000);
        AddHtml(40, 65, 320, 20, "<basefont color=#44>Sphere 51a Mechanics:</basefont>", false, false);

        // Enable/Disable button
        if (_stone.SphereMechanicsEnabled)
        {
            AddButton(40, 90, 4005, 4007, 1); // Active button
            AddHtml(80, 93, 200, 20, "<basefont color=#44>Enabled</basefont>", false, false);
        }
        else
        {
            AddButton(40, 90, 4005, 4006, 1); // Inactive button
            AddHtml(80, 93, 200, 20, "<basefont color=#33>Disabled</basefont>", false, false);
        }

        // Duel type configuration
        AddBackground(20, 125, 360, 80, 3000);
        AddHtml(40, 135, 320, 20, "<basefont color=#44>Duel Configuration:</basefont>", false, false);

        // Type selection
        AddHtml(40, 160, 100, 20, "<basefont color=#44>Type:</basefont>", false, false);
        AddButton(140, 160, 4005, 4007, 2); // Money 1v1
        AddHtml(180, 163, 100, 20, "<basefont color=#44>Money 1v1</basefont>", false, false);

        AddButton(140, 185, 4005, 4007, 3); // Loot 1v1
        AddHtml(180, 188, 100, 20, "<basefont color=#44>Loot 1v1</basefont>", false, false);

        // Entry cost
        AddHtml(40, 210, 100, 20, "<basefont color=#44>Cost:</basefont>", false, false);
        AddTextEntry(140, 210, 100, 20, 0, 0, _stone.EntryCost.ToString());

        // Save button
        AddButton(150, 250, 4005, 4007, 4);
        AddHtml(190, 253, 100, 20, "<basefont color=#44>Save Settings</basefont>", false, false);
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

            case 1: // Toggle Sphere mechanics
                _stone.SphereMechanicsEnabled = !_stone.SphereMechanicsEnabled;
                from.SendMessage($"Sphere mechanics {( _stone.SphereMechanicsEnabled ? "enabled" : "disabled" )} for this duel stone.");
                from.SendGump(new SphereDuelConfigGump(_stone));
                break;

            case 2: // Set to Money 1v1
                _stone.Type = DuelType.Money1v1;
                from.SendMessage("Duel type set to Money 1v1.");
                from.SendGump(new SphereDuelConfigGump(_stone));
                break;

            case 3: // Set to Loot 1v1
                _stone.Type = DuelType.Loot1v1;
                from.SendMessage("Duel type set to Loot 1v1.");
                from.SendGump(new SphereDuelConfigGump(_stone));
                break;

            case 4: // Save settings
                var costEntry = info.GetTextEntry(0);
                if (costEntry != null && int.TryParse(costEntry, out int newCost))
                {
                    _stone.EntryCost = Math.Max(0, newCost);
                    from.SendMessage($"Entry cost set to {_stone.EntryCost}.");
                }
                from.SendGump(new SphereDuelConfigGump(_stone));
                break;
        }
    }
}
