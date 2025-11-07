using Server.Gumps;
using Server.Network;

namespace Server.Engines.DuelArena;

public class DuelWagerGump : Gump
{
    private readonly DuelStoneComponent _stone;
    private int _selectedWager = -1;
    private bool _isLootSelected = false;

    public DuelWagerGump(DuelStoneComponent stone) : base(50, 50)
    {
        _stone = stone;

        Closable = true;
        Disposable = true;
        Draggable = true;
        Resizable = false;

        BuildGump();
    }

    private void BuildGump()
    {
        AddPage(0);

        AddBackground(0, 0, 400, 380, 9200);
        AddImageTiled(10, 10, 380, 360, 2624);
        AddAlphaRegion(10, 10, 380, 360);

        AddHtml(10, 20, 380, 25, "<center><basefont color=#FFFFFF size=7>Duel Challenge</basefont></center>", false, false);
        AddHtml(30, 60, 340, 30, "<basefont color=#FFFFFF>Select your wager amount:</basefont>", false, false);

        // Wager buttons - 5k, 10k, 25k, 50k
        AddButton(50, 100, 4005, 4007, 1, GumpButtonType.Reply, 0);
        AddHtml(90, 100, 200, 25, "<basefont color=#FFFFFF>5,000 gold</basefont>", false, false);

        AddButton(50, 135, 4005, 4007, 2, GumpButtonType.Reply, 0);
        AddHtml(90, 135, 200, 25, "<basefont color=#FFFFFF>10,000 gold</basefont>", false, false);

        AddButton(50, 170, 4005, 4007, 3, GumpButtonType.Reply, 0);
        AddHtml(90, 170, 200, 25, "<basefont color=#FFFFFF>25,000 gold</basefont>", false, false);

        AddButton(50, 205, 4005, 4007, 4, GumpButtonType.Reply, 0);
        AddHtml(90, 205, 200, 25, "<basefont color=#FFFFFF>50,000 gold</basefont>", false, false);

        // Loot option
        AddButton(50, 250, 4005, 4007, 5, GumpButtonType.Reply, 0);
        AddHtml(90, 250, 300, 40, "<basefont color=#FFFF00>Loot Only</basefont><br><basefont color=#CCCCCC size=1>(No gold - winner loots corpse)</basefont>", false, false);

        // Ok and Cancel buttons
        AddButton(80, 320, 4005, 4007, 100, GumpButtonType.Reply, 0);
        AddHtml(120, 320, 100, 25, "<basefont color=#00FF00>Ok</basefont>", false, false);

        AddButton(230, 320, 4005, 4007, 0, GumpButtonType.Reply, 0);
        AddHtml(270, 320, 100, 25, "<basefont color=#FF0000>Cancel</basefont>", false, false);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (from == null || _stone == null || _stone.Deleted)
        {
            return;
        }

        // Cancel button or close
        if (info.ButtonID == 0)
        {
            from.SendMessage("Duel challenge cancelled.");
            return;
        }

        // Wager selection buttons (1-5)
        if (info.ButtonID >= 1 && info.ButtonID <= 5)
        {
            switch (info.ButtonID)
            {
                case 1:
                    _selectedWager = 5000;
                    _isLootSelected = false;
                    break;
                case 2:
                    _selectedWager = 10000;
                    _isLootSelected = false;
                    break;
                case 3:
                    _selectedWager = 25000;
                    _isLootSelected = false;
                    break;
                case 4:
                    _selectedWager = 50000;
                    _isLootSelected = false;
                    break;
                case 5:
                    _selectedWager = 0;
                    _isLootSelected = true;
                    break;
            }

            // Reopen the gump with selection highlighted
            from.SendGump(new DuelWagerGumpWithSelection(_stone, _selectedWager, _isLootSelected));
            return;
        }

        // Ok button (100)
        if (info.ButtonID == 100)
        {
            if (_selectedWager < 0 && !_isLootSelected)
            {
                from.SendMessage("Please select a wager amount first.");
                from.SendGump(new DuelWagerGump(_stone));
                return;
            }

            // Proceed to targeting
            _stone.OnWagerSelected(from, _selectedWager, _isLootSelected);
        }
    }
}

// Gump variant that shows the current selection
public class DuelWagerGumpWithSelection : Gump
{
    private readonly DuelStoneComponent _stone;
    private readonly int _selectedWager;
    private readonly bool _isLootSelected;

    public DuelWagerGumpWithSelection(DuelStoneComponent stone, int selectedWager, bool isLootSelected) : base(50, 50)
    {
        _stone = stone;
        _selectedWager = selectedWager;
        _isLootSelected = isLootSelected;

        Closable = true;
        Disposable = true;
        Draggable = true;
        Resizable = false;

        BuildGump();
    }

    private void BuildGump()
    {
        AddPage(0);

        AddBackground(0, 0, 400, 380, 9200);
        AddImageTiled(10, 10, 380, 360, 2624);
        AddAlphaRegion(10, 10, 380, 360);

        AddHtml(10, 20, 380, 25, "<center><basefont color=#FFFFFF size=7>Duel Challenge</basefont></center>", false, false);
        AddHtml(30, 60, 340, 30, "<basefont color=#FFFFFF>Select your wager amount:</basefont>", false, false);

        // Wager buttons with selection highlighting
        AddButton(50, 100, 4005, 4007, 1, GumpButtonType.Reply, 0);
        AddHtml(90, 100, 200, 25, _selectedWager == 5000 ? "<basefont color=#00FF00>5,000 gold [SELECTED]</basefont>" : "<basefont color=#FFFFFF>5,000 gold</basefont>", false, false);

        AddButton(50, 135, 4005, 4007, 2, GumpButtonType.Reply, 0);
        AddHtml(90, 135, 200, 25, _selectedWager == 10000 ? "<basefont color=#00FF00>10,000 gold [SELECTED]</basefont>" : "<basefont color=#FFFFFF>10,000 gold</basefont>", false, false);

        AddButton(50, 170, 4005, 4007, 3, GumpButtonType.Reply, 0);
        AddHtml(90, 170, 200, 25, _selectedWager == 25000 ? "<basefont color=#00FF00>25,000 gold [SELECTED]</basefont>" : "<basefont color=#FFFFFF>25,000 gold</basefont>", false, false);

        AddButton(50, 205, 4005, 4007, 4, GumpButtonType.Reply, 0);
        AddHtml(90, 205, 200, 25, _selectedWager == 50000 ? "<basefont color=#00FF00>50,000 gold [SELECTED]</basefont>" : "<basefont color=#FFFFFF>50,000 gold</basefont>", false, false);

        // Loot option
        AddButton(50, 250, 4005, 4007, 5, GumpButtonType.Reply, 0);
        AddHtml(90, 250, 300, 40, _isLootSelected ? "<basefont color=#00FF00>Loot Only [SELECTED]</basefont><br><basefont color=#CCCCCC size=1>(No gold - winner loots corpse)</basefont>" : "<basefont color=#FFFF00>Loot Only</basefont><br><basefont color=#CCCCCC size=1>(No gold - winner loots corpse)</basefont>", false, false);

        // Ok and Cancel buttons
        AddButton(80, 320, 4005, 4007, 100, GumpButtonType.Reply, 0);
        AddHtml(120, 320, 100, 25, "<basefont color=#00FF00>Ok</basefont>", false, false);

        AddButton(230, 320, 4005, 4007, 0, GumpButtonType.Reply, 0);
        AddHtml(270, 320, 100, 25, "<basefont color=#FF0000>Cancel</basefont>", false, false);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (from == null || _stone == null || _stone.Deleted)
        {
            return;
        }

        // Cancel button or close
        if (info.ButtonID == 0)
        {
            from.SendMessage("Duel challenge cancelled.");
            return;
        }

        // Wager selection buttons (1-5) - change selection
        if (info.ButtonID >= 1 && info.ButtonID <= 5)
        {
            int newWager = _selectedWager;
            bool newIsLoot = _isLootSelected;

            switch (info.ButtonID)
            {
                case 1:
                    newWager = 5000;
                    newIsLoot = false;
                    break;
                case 2:
                    newWager = 10000;
                    newIsLoot = false;
                    break;
                case 3:
                    newWager = 25000;
                    newIsLoot = false;
                    break;
                case 4:
                    newWager = 50000;
                    newIsLoot = false;
                    break;
                case 5:
                    newWager = 0;
                    newIsLoot = true;
                    break;
            }

            // Reopen with new selection
            from.SendGump(new DuelWagerGumpWithSelection(_stone, newWager, newIsLoot));
            return;
        }

        // Ok button (100)
        if (info.ButtonID == 100)
        {
            // Proceed to targeting
            _stone.OnWagerSelected(from, _selectedWager, _isLootSelected);
        }
    }
}
