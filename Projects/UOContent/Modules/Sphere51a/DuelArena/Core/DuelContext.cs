using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server;
using Server.Items;

namespace Server.Engines.DuelArena;

public class DuelContext
{
    private DuelArena _arena;
    private List<DuelParticipant> _participants;
    private DuelType _duelType;
    private DuelState _state;
    private DateTime _startTime;
    private DateTime _endTime;
    private int _entryCost;
    private bool _ladderEnabled;

    private TimerExecutionToken _countdownToken;
    private TimerExecutionToken _matchTimerToken;
    private TimerExecutionToken _lootTimerToken;
    private int _countdownRemaining;
    private int _lootSecondsRemaining;
    private DuelParticipant _winner;

    [CommandProperty(AccessLevel.GameMaster)]
    public DuelState State => _state;

    [CommandProperty(AccessLevel.GameMaster)]
    public DuelType DuelType => _duelType;

    [CommandProperty(AccessLevel.GameMaster)]
    public TimeSpan Elapsed => Core.Now - _startTime;

    [CommandProperty(AccessLevel.GameMaster)]
    public int EntryCost => _entryCost;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool LadderEnabled => _ladderEnabled;

    public DuelArena Arena => _arena;

    public IReadOnlyList<DuelParticipant> Participants => _participants;

    public bool IsActive => _state is DuelState.Countdown or DuelState.InProgress;

    public DuelContext(DuelArena arena, DuelType duelType, int entryCost, bool ladderEnabled)
    {
        _arena = arena;
        _duelType = duelType;
        _entryCost = entryCost;
        _ladderEnabled = ladderEnabled;
        _state = DuelState.Waiting;
        _participants = new List<DuelParticipant>();
        _startTime = Core.Now;
        _endTime = DateTime.MinValue;
        _countdownRemaining = 10;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsParticipant(Mobile m)
    {
        foreach (var p in _participants)
        {
            if (p.Mobile == m)
            {
                return true;
            }
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DuelParticipant GetParticipant(Mobile m)
    {
        foreach (var p in _participants)
        {
            if (p.Mobile == m)
            {
                return p;
            }
        }
        return null;
    }

    public bool CanAddParticipant(Mobile m)
    {
        if (m == null || m.Deleted)
        {
            return false;
        }

        if (_state != DuelState.Waiting)
        {
            return false;
        }

        if (IsParticipant(m))
        {
            return false;
        }

        if (_participants.Count >= _arena.MaxPlayers)
        {
            return false;
        }

        return true;
    }

    public bool AddParticipant(Mobile m)
    {
        if (!CanAddParticipant(m))
        {
            return false;
        }

        int teamId = _participants.Count % 2;
        var participant = new DuelParticipant(m, teamId, m.Location, m.Map, _duelType);
        _participants.Add(participant);

        return true;
    }

    public bool RemoveParticipant(Mobile m)
    {
        var participant = GetParticipant(m);
        if (participant == null)
        {
            return false;
        }

        _participants.Remove(participant);
        return true;
    }

    public void StartCountdown()
    {
        if (_state != DuelState.Waiting)
        {
            return;
        }

        _state = DuelState.Countdown;
        _countdownRemaining = 10;

        _countdownToken.Cancel();

        // Freeze all participants during countdown
        FreezeAllParticipants();

        Timer.StartTimer(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            _countdownRemaining,
            OnCountdownTick,
            out _countdownToken
        );

        NotifyParticipants("Duel starting in 10 seconds...");
        BroadcastOverhead("10", 0x3B2);
    }

    private void OnCountdownTick()
    {
        _countdownRemaining--;

        if (_countdownRemaining <= 0)
        {
            BeginDuel();
        }
        else
        {
            BroadcastOverhead($"{_countdownRemaining}", 0x3B2);
        }
    }

    public void BeginDuel()
    {
        if (_state != DuelState.Countdown)
        {
            return;
        }

        _state = DuelState.InProgress;
        _startTime = Core.Now;

        TeleportParticipantsToArena();
        RestoreAllParticipants();
        UnfreezeAllParticipants();

        NotifyParticipants("Fight!");
        BroadcastOverhead("Fight!", 0x26);

        StartMatchTimer();
    }

    private void StartMatchTimer()
    {
        _matchTimerToken.Cancel();

        Timer.StartTimer(
            TimeSpan.FromMinutes(30),
            OnMatchTimeout,
            out _matchTimerToken
        );
    }

    private void OnMatchTimeout()
    {
        if (_state != DuelState.InProgress)
        {
            return;
        }

        NotifyParticipants("Duel has timed out! All participants will be returned.");
        EndDuel(null);
    }

    public void EndDuel(DuelParticipant winner)
    {
        if (_state is DuelState.Ending or DuelState.LootPhase or DuelState.Completed)
        {
            return;
        }

        _state = DuelState.Ending;
        _endTime = Core.Now;
        _winner = winner;

        StopTimers();

        if (winner != null)
        {
            PayoutWinnings(winner);
            NotifyParticipants($"{winner.Mobile.Name} has won the duel!");
            BroadcastOverhead($"{winner.Mobile.Name} wins!", 0x26);
        }
        else
        {
            NotifyParticipants("Duel ended in a draw!");
            BroadcastOverhead("Draw!", 0x3B2);
        }

        // LOOT DUELS: Enter LootPhase and start 2-minute timer
        if (_duelType is DuelType.Loot1v1 or DuelType.Loot2v2)
        {
            _state = DuelState.LootPhase;
            StartLootTimer(winner);
        }
        else
        {
            // MONEY DUELS: Return immediately after 5 seconds
            Timer.DelayCall(TimeSpan.FromSeconds(5), Cleanup);
            // Note: CleanupContext scheduling is now handled here for consistency
        }
    }

    private void Cleanup()
    {
        // Clear aggressor relationships between all duel participants first
        ClearAllAggressorRelationships();

        // Clear criminal flags BEFORE forcing notoriety update
        RestoreAllParticipants();

        // Force notoriety update AFTER criminal flags are cleared
        foreach (var participant in _participants)
        {
            if (participant.Mobile is { Deleted: false })
            {
                participant.Mobile.Delta(MobileDelta.Noto);
            }
        }

        // Teleport out of arena
        ReturnAllParticipants();

        // Clear player contexts immediately after teleport
        DuelSystem.UnregisterContext(this);

        _state = DuelState.Completed;
    }

    private void RemoveAggressions(Mobile mob)
    {
        if (mob == null || mob.Deleted)
        {
            return;
        }

        // Clear combatant immediately
        mob.Combatant = null;

        // Remove all aggressor/aggressed relationships with other participants
        foreach (var participant in _participants)
        {
            var target = participant.Mobile;
            if (target == null || target.Deleted || target == mob)
            {
                continue;
            }

            mob.RemoveAggressed(target);
            mob.RemoveAggressor(target);
            target.RemoveAggressed(mob);
            target.RemoveAggressor(mob);
        }
    }

    private void ClearAllAggressorRelationships()
    {
        foreach (var participant in _participants)
        {
            if (participant.Mobile is { Deleted: false })
            {
                RemoveAggressions(participant.Mobile);
            }
        }
    }

    private void TeleportParticipantsToArena()
    {
        var spawnPoints = _arena.SpawnPoints;
        for (int i = 0; i < _participants.Count && i < spawnPoints.Length; i++)
        {
            var p = _participants[i];
            if (p.Mobile is { Deleted: false })
            {
                p.Mobile.MoveToWorld(spawnPoints[i], _arena.Map);
            }
        }
    }

    private void ReturnAllParticipants()
    {
        foreach (var p in _participants)
        {
            p.ReturnToOriginalLocation();
        }
    }

    private void RestoreAllParticipants()
    {
        foreach (var p in _participants)
        {
            p.Restore();
        }
    }

    private void FreezeAllParticipants()
    {
        foreach (var p in _participants)
        {
            if (p.Mobile is { Deleted: false })
            {
                p.Mobile.Frozen = true;
            }
        }
    }

    private void UnfreezeAllParticipants()
    {
        foreach (var p in _participants)
        {
            if (p.Mobile is { Deleted: false })
            {
                p.Mobile.Frozen = false;
            }
        }
    }

    private void PayoutWinnings(DuelParticipant winner)
    {
        if (_duelType is DuelType.Loot1v1 or DuelType.Loot2v2)
        {
            return;
        }

        if (_entryCost <= 0)
        {
            return;
        }

        int totalPot = _entryCost * _participants.Count;
        int payout = (int)(totalPot * 0.9);

        if (winner.Mobile is { Deleted: false } && winner.Mobile.Backpack != null)
        {
            winner.Mobile.Backpack.AddItem(new Items.Gold(payout));
            winner.Mobile.SendMessage($"{payout} gold has been added to your backpack.");
        }
    }

    private void NotifyParticipants(string message)
    {
        foreach (var p in _participants)
        {
            if (p.Mobile is { Deleted: false })
            {
                p.Mobile.SendMessage(message);
            }
        }
    }

    private void BroadcastOverhead(string message, int hue)
    {
        foreach (var p in _participants)
        {
            if (p.Mobile is { Deleted: false })
            {
                p.Mobile.PublicOverheadMessage(MessageType.Regular, hue, false, message);
            }
        }
    }

    private void StartLootTimer(DuelParticipant winner)
    {
        _lootSecondsRemaining = 120; // 2 minutes

        if (winner?.Mobile != null)
        {
            winner.Mobile.SendMessage("You have 2 minutes to loot before being teleported out.");
        }

        _lootTimerToken.Cancel();

        // Tick every second
        Timer.StartTimer(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            120,
            OnLootTimerTick,
            out _lootTimerToken
        );
    }

    private void OnLootTimerTick()
    {
        _lootSecondsRemaining--;

        if (_lootSecondsRemaining <= 0)
        {
            // Time's up - cleanup and return winner
            Cleanup();
            return;
        }

        // Display countdown
        bool shouldDisplay = false;

        if (_lootSecondsRemaining <= 10)
        {
            // Every second from 10 to 1
            shouldDisplay = true;
        }
        else if (_lootSecondsRemaining % 30 == 0)
        {
            // Every 30 seconds: 120, 90, 60, 30
            shouldDisplay = true;
        }

        if (shouldDisplay && _winner?.Mobile != null)
        {
            string timeDisplay = FormatTimeRemaining(_lootSecondsRemaining);
            _winner.Mobile.PublicOverheadMessage(
                MessageType.Regular,
                0x3B2,
                false,
                timeDisplay
            );
        }
    }

    private string FormatTimeRemaining(int seconds)
    {
        int minutes = seconds / 60;
        int secs = seconds % 60;

        if (minutes > 0)
        {
            return $"{minutes}:{secs:D2}";
        }
        else
        {
            return $"0:{secs:D2}";
        }
    }

    public void StopTimers()
    {
        _countdownToken.Cancel();
        _matchTimerToken.Cancel();
        _lootTimerToken.Cancel();
    }
}
