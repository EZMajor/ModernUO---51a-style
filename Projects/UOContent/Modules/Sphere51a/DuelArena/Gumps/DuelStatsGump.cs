using Server.Gumps;
using Server.Network;

namespace Server.Engines.DuelArena;

public class DuelStatsGump : Gump
{
    private readonly DuelContext _context;
    private readonly DuelParticipant _participant;

    public DuelStatsGump(DuelContext context, DuelParticipant participant) : base(50, 50)
    {
        _context = context;
        _participant = participant;

        Closable = true;
        Disposable = true;
        Draggable = true;
        Resizable = false;

        AddPage(0);

        AddBackground(0, 0, 400, 350, 9200);
        AddImageTiled(10, 10, 380, 330, 2624);
        AddAlphaRegion(10, 10, 380, 330);

        AddHtml(10, 20, 380, 25, "<center><basefont color=#FFFFFF size=7>Duel Statistics</basefont></center>", false, false);

        var y = 60;

        AddHtml(30, y, 150, 25, "<basefont color=#FFFFFF>Duel Type:</basefont>", false, false);
        AddHtml(200, y, 150, 25, $"<basefont color=#00FF00>{_context.DuelType}</basefont>", false, false);
        y += 30;

        AddHtml(30, y, 150, 25, "<basefont color=#FFFFFF>State:</basefont>", false, false);
        var stateColor = _context.State switch
        {
            DuelState.Waiting => "#FFFF00",
            DuelState.Countdown => "#FF9900",
            DuelState.InProgress => "#00FF00",
            DuelState.Ending => "#FF6600",
            DuelState.Completed => "#888888",
            _ => "#FFFFFF"
        };
        AddHtml(200, y, 150, 25, $"<basefont color={stateColor}>{_context.State}</basefont>", false, false);
        y += 30;

        if (_context.State == DuelState.InProgress || _context.State == DuelState.Ending)
        {
            AddHtml(30, y, 150, 25, "<basefont color=#FFFFFF>Duration:</basefont>", false, false);
            AddHtml(200, y, 150, 25, $"<basefont color=#00FF00>{_context.Elapsed.Minutes}:{_context.Elapsed.Seconds:D2}</basefont>", false, false);
            y += 30;
        }

        AddHtml(30, y, 150, 25, "<basefont color=#FFFFFF>Participants:</basefont>", false, false);
        AddHtml(200, y, 150, 25, $"<basefont color=#00FF00>{_context.Participants.Count}</basefont>", false, false);
        y += 40;

        AddHtml(30, y, 340, 25, "<basefont color=#FFFF00>Your Statistics:</basefont>", false, false);
        y += 30;

        AddHtml(30, y, 150, 25, "<basefont color=#FFFFFF>Team:</basefont>", false, false);
        AddHtml(200, y, 150, 25, $"<basefont color=#00FF00>{_participant.TeamId + 1}</basefont>", false, false);
        y += 30;

        AddHtml(30, y, 150, 25, "<basefont color=#FFFFFF>Kills:</basefont>", false, false);
        AddHtml(200, y, 150, 25, $"<basefont color=#00FF00>{_participant.Kills}</basefont>", false, false);
        y += 30;

        AddHtml(30, y, 150, 25, "<basefont color=#FFFFFF>Deaths:</basefont>", false, false);
        AddHtml(200, y, 150, 25, $"<basefont color=#FF0000>{_participant.Deaths}</basefont>", false, false);
        y += 30;

        AddHtml(30, y, 150, 25, "<basefont color=#FFFFFF>Status:</basefont>", false, false);
        var statusColor = _participant.IsEliminated ? "#FF0000" : "#00FF00";
        var statusText = _participant.IsEliminated ? "Eliminated" : "Active";
        AddHtml(200, y, 150, 25, $"<basefont color={statusColor}>{statusText}</basefont>", false, false);

        AddButton(150, 300, 4005, 4007, 0, GumpButtonType.Reply, 0);
        AddHtml(190, 300, 100, 25, "<basefont color=#FFFFFF>Close</basefont>", false, false);
    }
}
