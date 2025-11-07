using Server.Gumps;
using Server.Network;
using Server.Prompts;

namespace Server.Engines.DuelArena;

public class DuelConfigGump : Gump
{
    private readonly DuelStoneComponent _stone;

    public DuelConfigGump(DuelStoneComponent stone) : base(50, 50)
    {
        _stone = stone;

        Closable = true;
        Disposable = true;
        Draggable = true;
        Resizable = false;

        AddPage(0);

        AddBackground(0, 0, 500, 450, 9200);
        AddImageTiled(10, 10, 480, 430, 2624);
        AddAlphaRegion(10, 10, 480, 430);

        AddHtml(10, 20, 480, 25, "<center><basefont color=#FFFFFF size=7>Duel Stone Configuration</basefont></center>", false, false);

        var y = 60;

        AddHtml(30, y, 200, 25, "<basefont color=#FFFFFF>Duel Type:</basefont>", false, false);
        AddButton(250, y, 4005, 4007, 1, GumpButtonType.Reply, 0);
        AddHtml(290, y, 150, 25, $"<basefont color=#00FF00>{_stone.Type}</basefont>", false, false);
        y += 35;

        AddHtml(30, y, 200, 25, "<basefont color=#FFFFFF>Entry Cost:</basefont>", false, false);
        AddButton(250, y, 4005, 4007, 2, GumpButtonType.Reply, 0);
        AddHtml(290, y, 150, 25, $"<basefont color=#00FF00>{_stone.EntryCost}</basefont>", false, false);
        y += 35;

        AddHtml(30, y, 200, 25, "<basefont color=#FFFFFF>Ladder Enabled:</basefont>", false, false);
        AddButton(250, y, 4005, 4007, 3, GumpButtonType.Reply, 0);
        AddHtml(290, y, 150, 25, $"<basefont color=#00FF00>{_stone.LadderEnabled}</basefont>", false, false);
        y += 35;

        AddHtml(30, y, 200, 25, "<basefont color=#FFFFFF>Idle Time (sec):</basefont>", false, false);
        AddButton(250, y, 4005, 4007, 4, GumpButtonType.Reply, 0);
        AddHtml(290, y, 150, 25, $"<basefont color=#00FF00>{_stone.IdleTimeSeconds}</basefont>", false, false);
        y += 35;

        AddHtml(30, y, 200, 25, "<basefont color=#FFFFFF>Max Idle Time (sec):</basefont>", false, false);
        AddButton(250, y, 4005, 4007, 5, GumpButtonType.Reply, 0);
        AddHtml(290, y, 150, 25, $"<basefont color=#00FF00>{_stone.MaxIdleTimeSeconds}</basefont>", false, false);
        y += 50;

        AddHtml(30, y, 440, 25, "<basefont color=#FFFF00>Arena Configuration:</basefont>", false, false);
        y += 30;

        if (_stone.Arena != null)
        {
            AddHtml(30, y, 200, 25, "<basefont color=#FFFFFF>Arena Name:</basefont>", false, false);
            AddHtml(250, y, 200, 25, $"<basefont color=#00FF00>{_stone.Arena.Name}</basefont>", false, false);
            y += 30;

            AddHtml(30, y, 200, 25, "<basefont color=#FFFFFF>Max Players:</basefont>", false, false);
            AddHtml(250, y, 200, 25, $"<basefont color=#00FF00>{_stone.Arena.MaxPlayers}</basefont>", false, false);
            y += 30;

            AddHtml(30, y, 200, 25, "<basefont color=#FFFFFF>Spawn Points:</basefont>", false, false);
            AddHtml(250, y, 200, 25, $"<basefont color=#00FF00>{_stone.Arena.SpawnPoints?.Length ?? 0}</basefont>", false, false);
        }
        else
        {
            AddHtml(30, y, 400, 25, "<basefont color=#FF0000>No arena configured!</basefont>", false, false);
        }

        AddButton(200, 400, 4005, 4007, 0, GumpButtonType.Reply, 0);
        AddHtml(240, 400, 100, 25, "<basefont color=#FFFFFF>Close</basefont>", false, false);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (from == null || _stone == null || from.AccessLevel < AccessLevel.GameMaster)
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 1: // Change Duel Type
                from.SendMessage("Cycling duel type...");
                _stone.Type = _stone.Type switch
                {
                    DuelType.Money1v1 => DuelType.Loot1v1,
                    DuelType.Loot1v1 => DuelType.Money2v2,
                    DuelType.Money2v2 => DuelType.Loot2v2,
                    DuelType.Loot2v2 => DuelType.Money1v1,
                    _ => DuelType.Money1v1
                };
                from.SendGump(new DuelConfigGump(_stone));
                break;

            case 2: // Change Entry Cost
                from.SendMessage("Enter the new entry cost:");
                from.Prompt = new EntryCostPrompt(_stone);
                break;

            case 3: // Toggle Ladder
                _stone.LadderEnabled = !_stone.LadderEnabled;
                from.SendMessage($"Ladder {(_stone.LadderEnabled ? "enabled" : "disabled")}.");
                from.SendGump(new DuelConfigGump(_stone));
                break;

            case 4: // Change Idle Time
                from.SendMessage("Enter the new idle time in seconds:");
                from.Prompt = new IdleTimePrompt(_stone);
                break;

            case 5: // Change Max Idle Time
                from.SendMessage("Enter the new max idle time in seconds:");
                from.Prompt = new MaxIdleTimePrompt(_stone);
                break;
        }
    }

    private class EntryCostPrompt : Prompt
    {
        private readonly DuelStoneComponent _stone;

        public EntryCostPrompt(DuelStoneComponent stone)
        {
            _stone = stone;
        }

        public override void OnResponse(Mobile from, string text)
        {
            if (int.TryParse(text, out var cost) && cost >= 0)
            {
                _stone.EntryCost = cost;
                from.SendMessage($"Entry cost set to {cost} gold.");
            }
            else
            {
                from.SendMessage("Invalid entry cost. Please enter a positive number.");
            }

            from.SendGump(new DuelConfigGump(_stone));
        }
    }

    private class IdleTimePrompt : Prompt
    {
        private readonly DuelStoneComponent _stone;

        public IdleTimePrompt(DuelStoneComponent stone)
        {
            _stone = stone;
        }

        public override void OnResponse(Mobile from, string text)
        {
            if (int.TryParse(text, out var time) && time >= 5 && time <= 60)
            {
                _stone.IdleTimeSeconds = time;
                from.SendMessage($"Idle time set to {time} seconds.");
            }
            else
            {
                from.SendMessage("Invalid idle time. Please enter a value between 5 and 60.");
            }

            from.SendGump(new DuelConfigGump(_stone));
        }
    }

    private class MaxIdleTimePrompt : Prompt
    {
        private readonly DuelStoneComponent _stone;

        public MaxIdleTimePrompt(DuelStoneComponent stone)
        {
            _stone = stone;
        }

        public override void OnResponse(Mobile from, string text)
        {
            if (int.TryParse(text, out var time) && time >= 5 && time <= 60)
            {
                _stone.MaxIdleTimeSeconds = time;
                from.SendMessage($"Max idle time set to {time} seconds.");
            }
            else
            {
                from.SendMessage("Invalid max idle time. Please enter a value between 5 and 60.");
            }

            from.SendGump(new DuelConfigGump(_stone));
        }
    }
}
