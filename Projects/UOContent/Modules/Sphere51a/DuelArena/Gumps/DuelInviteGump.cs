using System;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.DuelArena;

public class DuelInviteGump : Gump
{
    private readonly Mobile _target;
    private readonly int _wager;
    private readonly bool _isLoot;
    private TimerExecutionToken _timeoutToken;

    public DuelInviteGump(Mobile target, int wager, bool isLoot) : base(50, 50)
    {
        _target = target;
        _wager = wager;
        _isLoot = isLoot;

        Closable = true;
        Disposable = true;
        Draggable = true;
        Resizable = false;

        BuildGump(target);
        StartTimeout(target);
    }

    private void BuildGump(Mobile target)
    {
        var challenge = DuelSystem.GetPendingChallenge(target);
        if (challenge == null)
        {
            return;
        }

        AddPage(0);

        AddBackground(0, 0, 400, 300, 9200);
        AddImageTiled(10, 10, 380, 280, 2624);
        AddAlphaRegion(10, 10, 380, 280);

        AddHtml(10, 20, 380, 25, "<center><basefont color=#FFFFFF size=7>Duel Challenge</basefont></center>", false, false);

        string wagerText = _isLoot ? "Loot Only" : $"{_wager:N0} gold";

        AddHtml(30, 60, 340, 120,
            $"<basefont color=#FFFFFF>{challenge.Initiator.Name} challenges you to a duel!<br><br>" +
            $"Wager: {wagerText}<br><br>" +
            $"Do you accept?<br><br>" +
            $"<basefont color=#CCCCCC size=1>(This invitation expires in 30 seconds)</basefont></basefont>",
            false, false);

        AddButton(80, 220, 4005, 4007, 1, GumpButtonType.Reply, 0);
        AddHtml(120, 220, 100, 25, "<basefont color=#00FF00>Accept</basefont>", false, false);

        AddButton(230, 220, 4005, 4007, 0, GumpButtonType.Reply, 0);
        AddHtml(270, 220, 100, 25, "<basefont color=#FF0000>Decline</basefont>", false, false);
    }

    private void StartTimeout(Mobile target)
    {
        // Set 30 second timeout
        Timer.StartTimer(TimeSpan.FromSeconds(30), () => OnTimeout(target), out _timeoutToken);
    }

    private void OnTimeout(Mobile target)
    {
        var challenge = DuelSystem.GetPendingChallenge(target);
        if (challenge == null)
        {
            return;
        }

        // Timeout - notify both players and clean up
        target.SendMessage("The duel invitation has expired.");
        challenge.Initiator.SendMessage($"{target.Name} did not respond to your duel challenge.");

        // Refund initiator if gold was charged
        if (!challenge.IsLoot && challenge.Wager > 0)
        {
            challenge.Stone.RefundGoldToPlayer(challenge.Initiator, challenge.Wager);
        }

        DuelSystem.ClearChallenge(target);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (from == null)
        {
            return;
        }

        // Cancel the timeout timer
        _timeoutToken.Cancel();

        var challenge = DuelSystem.GetPendingChallenge(from);
        if (challenge == null)
        {
            from.SendMessage("The duel challenge is no longer valid.");
            return;
        }

        if (info.ButtonID == 1) // Accept
        {
            // Clear challenge BEFORE calling OnTargetAccepted to prevent validation from seeing pending challenge
            DuelSystem.ClearChallenge(from);
            challenge.Stone.OnTargetAccepted(challenge.Initiator, from, challenge.Wager, challenge.IsLoot);
        }
        else // Decline or close
        {
            challenge.Stone.OnTargetDeclined(challenge.Initiator, from, challenge.Wager, challenge.IsLoot);
            DuelSystem.ClearChallenge(from);
        }
    }

    public override void OnServerClose(NetState owner)
    {
        base.OnServerClose(owner);

        // Ensure timeout timer is cancelled
        _timeoutToken.Cancel();

        // Ensure challenge is cleared even if gump was closed without response
        if (_target != null)
        {
            var challenge = DuelSystem.GetPendingChallenge(_target);
            if (challenge != null)
            {
                _target.SendMessage("The duel invitation has expired.");
                challenge.Initiator.SendMessage($"{_target.Name} did not respond to your duel challenge.");

                // Refund initiator if gold was charged
                if (!challenge.IsLoot && challenge.Wager > 0)
                {
                    challenge.Stone.RefundGoldToPlayer(challenge.Initiator, challenge.Wager);
                }

                DuelSystem.ClearChallenge(_target);
            }
        }
    }
}
