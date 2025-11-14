using System;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.DuelArena;

public class DuelInviteGump : Gump
{
    private readonly Mobile _target;
    private readonly int _wager;
    private readonly bool _isLoot;
    private readonly int _remainingSeconds;
    private TimerExecutionToken _countdownToken;

    public DuelInviteGump(Mobile target, int wager, bool isLoot, int remainingSeconds = 30) : base(50, 50)
    {
        _target = target;
        _wager = wager;
        _isLoot = isLoot;
        _remainingSeconds = remainingSeconds;

        Closable = true;
        Disposable = true;
        Draggable = true;
        Resizable = false;

        BuildGump(target);
        StartCountdown(target);
    }

    private void BuildGump(Mobile target)
    {
        var challenge = DuelSystem.GetPendingChallenge(target);

        // Always add Page(0) first - required for valid gump
        AddPage(0);
        AddBackground(0, 0, 400, 300, 9200);
        AddImageTiled(10, 10, 380, 280, 2624);
        AddAlphaRegion(10, 10, 380, 280);

        if (challenge == null)
        {
            // Challenge not found - show error message
            AddHtml(10, 20, 380, 25, "<center><basefont color=#FF0000 size=7>Error</basefont></center>", false, false);
            AddHtml(30, 60, 340, 150,
                "<basefont color=#FFFFFF>The duel challenge is no longer valid.<br><br>This gump will close automatically.</basefont>",
                false, false);
            return;
        }

        AddHtml(10, 20, 380, 25, "<center><basefont color=#FFFFFF size=7>Duel Challenge</basefont></center>", false, false);

        string wagerText = _isLoot ? "Loot Only" : $"{_wager:N0} gold";

        // Determine color based on remaining time
        string timeColor = _remainingSeconds > 10 ? "#FFFF00" : _remainingSeconds > 5 ? "#FFA500" : "#FF0000";

        AddHtml(30, 60, 340, 150,
            $"<basefont color=#FFFFFF>{challenge.Initiator.Name} challenges you to a duel!<br><br>" +
            $"Wager: {wagerText}<br><br>" +
            $"Do you accept?<br><br>" +
            $"<center><basefont color={timeColor} size=6>Time Remaining: {_remainingSeconds} seconds</basefont></center></basefont>",
            false, false);

        AddButton(80, 220, 4005, 4007, 1, GumpButtonType.Reply, 0);
        AddHtml(120, 220, 100, 25, "<basefont color=#00FF00>Accept</basefont>", false, false);

        AddButton(230, 220, 4005, 4007, 0, GumpButtonType.Reply, 0);
        AddHtml(270, 220, 100, 25, "<basefont color=#FF0000>Decline</basefont>", false, false);
    }

    private void StartCountdown(Mobile target)
    {
        // Start countdown timer that fires every second
        if (_remainingSeconds > 0)
        {
            Timer.StartTimer(TimeSpan.FromSeconds(1), () => OnCountdownTick(target), out _countdownToken);
        }
        else
        {
            // Time's up - trigger timeout immediately
            OnTimeout(target);
        }
    }

    private void OnCountdownTick(Mobile target)
    {
        // Check if challenge is still valid
        var challenge = DuelSystem.GetPendingChallenge(target);
        if (challenge == null || target == null || target.Deleted)
        {
            // Challenge cancelled - stop countdown
            CancelTimers();
            return;
        }

        int newRemaining = _remainingSeconds - 1;

        if (newRemaining <= 0)
        {
            // Time's up - trigger timeout
            OnTimeout(target);
        }
        else
        {
            // Send notification for critical seconds
            if (newRemaining <= 5)
            {
                target.SendMessage(0x22, $"Duel invitation expires in {newRemaining} seconds!");
            }

            // Refresh gump with updated countdown
            target.CloseGump<DuelInviteGump>();
            target.SendGump(new DuelInviteGump(target, _wager, _isLoot, newRemaining));
        }
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
        target.CloseGump<DuelInviteGump>();
    }

    private void CancelTimers()
    {
        _countdownToken.Cancel();
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (from == null)
        {
            return;
        }

        // Cancel both timers
        CancelTimers();

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

        // Cancel both timers
        CancelTimers();

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
