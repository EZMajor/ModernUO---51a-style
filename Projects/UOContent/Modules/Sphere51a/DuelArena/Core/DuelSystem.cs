using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Gumps;
using Server.Targeting;

namespace Server.Engines.DuelArena;

public static class DuelSystem
{
    private static readonly Dictionary<Serial, DuelContext> _mobileContexts = new();
    private static readonly List<DuelContext> _activeContexts = new();
    private static readonly Dictionary<Serial, PendingChallenge> _pendingChallenges = new(); // Challenges targeting this player
    private static readonly Dictionary<Serial, PendingChallenge> _outgoingChallenges = new(); // Challenges from this player

    public static void Initialize()
    {
        // Don't initialize duel arena system if disabled
        if (!DuelArenaConfig.Enabled)
        {
            return;
        }

        EventSink.Logout += OnLogout;
        EventSink.Disconnected += OnDisconnected;

        CommandSystem.Register("duelstats", AccessLevel.Player, DuelStats_OnCommand);
        CommandSystem.Register("configduelstone", AccessLevel.GameMaster, ConfigDuelStone_OnCommand);
        CommandSystem.Register("add duelarena", AccessLevel.Administrator, PlaceDuelArena_OnCommand);
        CommandSystem.Register("add duelstone", AccessLevel.Administrator, PlaceDuelStone_OnCommand);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DuelContext FindContext(Mobile m)
    {
        if (m == null)
        {
            return null;
        }

        _mobileContexts.TryGetValue(m.Serial, out var context);
        return context;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetPlayerContext(Mobile m, DuelContext context)
    {
        if (m == null || context == null)
        {
            return;
        }

        _mobileContexts[m.Serial] = context;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ClearPlayerContext(Mobile m)
    {
        if (m == null)
        {
            return;
        }

        _mobileContexts.Remove(m.Serial);
    }

    public static void RegisterContext(DuelContext context)
    {
        if (context == null || _activeContexts.Contains(context))
        {
            return;
        }

        _activeContexts.Add(context);
    }

    public static void UnregisterContext(DuelContext context)
    {
        if (context == null)
        {
            return;
        }

        _activeContexts.Remove(context);

        foreach (var p in context.Participants)
        {
            if (p.Mobile != null)
            {
                ClearPlayerContext(p.Mobile);
            }
        }
    }

    private static void OnLogout(Mobile m)
    {
        HandleDisconnect(m);
    }

    private static void OnDisconnected(Mobile m)
    {
        HandleDisconnect(m);
    }


    [Usage("DuelStats")]
    [Description("Displays your duel statistics")]
    private static void DuelStats_OnCommand(CommandEventArgs e)
    {
        var m = e.Mobile;
        var context = FindContext(m);

        if (context == null)
        {
            m.SendMessage("You are not currently in a duel.");
            return;
        }

        var participant = context.GetParticipant(m);
        if (participant == null)
        {
            m.SendMessage("Unable to retrieve your duel information.");
            return;
        }

        m.SendGump(new DuelStatsGump(context, participant));
    }

    [Usage("ConfigDuelStone")]
    [Description("Opens configuration for the targeted duel stone")]
    private static void ConfigDuelStone_OnCommand(CommandEventArgs e)
    {
        var m = e.Mobile;
        m.SendMessage("Target the duel stone you wish to configure.");
        m.Target = new DuelStoneTarget();
    }

    [Usage("Add DuelArena")]
    [Description("Places a duel arena deed in your backpack")]
    private static void PlaceDuelArena_OnCommand(CommandEventArgs e)
    {
        var m = e.Mobile;

        if (m.Backpack == null)
        {
            m.SendMessage("You need a backpack to receive the arena deed.");
            return;
        }

        var deed = new DuelArenaDeed();
        m.Backpack.DropItem(deed);
        m.SendMessage("A duel arena deed has been placed in your backpack.");
        m.SendMessage("Double-click the deed to place a 4-arena complex (13x13 tiles with shared walls).");
    }

    [Usage("Add DuelStone")]
    [Description("Places a standalone duel stone gravestone at your location")]
    private static void PlaceDuelStone_OnCommand(CommandEventArgs e)
    {
        var m = e.Mobile;

        // Create the consolidated duel stone component
        var stoneComponent = new DuelStoneComponent
        {
            Type = DuelType.Money1v1,
            EntryCost = 100,
            Movable = false,
            Name = "Duel Stone"
        };

        stoneComponent.MoveToWorld(m.Location, m.Map);

        m.SendMessage("A duel stone has been placed at your location.");
        m.SendMessage("Use [Props to configure it, or use [ConfigDuelStone.");
    }

    private class DuelStoneTarget : Target
    {
        public DuelStoneTarget() : base(15, false, TargetFlags.None)
        {
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is DuelStoneComponent stone)
            {
                from.SendGump(new DuelConfigGump(stone));
            }
            else
            {
                from.SendMessage("That is not a duel stone.");
            }
        }
    }

    public static int GetActiveContextCount() => _activeContexts.Count;

    public static int GetActiveDuelerCount()
    {
        int count = 0;
        foreach (var context in _activeContexts)
        {
            if (context.IsActive)
            {
                count += context.Participants.Count;
            }
        }
        return count;
    }

    public static void Cleanup()
    {
        foreach (var context in _activeContexts.ToArray())
        {
            if (context.State == DuelState.Completed)
            {
                UnregisterContext(context);
            }
        }
    }

    // Challenge tracking methods
    public static bool HasPendingChallenge(Mobile target)
    {
        if (target == null)
        {
            return false;
        }

        if (!_pendingChallenges.TryGetValue(target.Serial, out var challenge))
        {
            return false;
        }

        // Clean up expired challenges (30 seconds timeout)
        if (challenge.ExpirationTime < Core.Now)
        {
            _pendingChallenges.Remove(target.Serial);
            return false;
        }

        return true;
    }

    public static bool HasOutgoingChallenge(Mobile initiator)
    {
        if (initiator == null)
        {
            return false;
        }

        if (!_outgoingChallenges.TryGetValue(initiator.Serial, out var challenge))
        {
            return false;
        }

        // Clean up expired challenges
        if (challenge.ExpirationTime < Core.Now)
        {
            _outgoingChallenges.Remove(initiator.Serial);
            return false;
        }

        return true;
    }

    public static bool IsChallengePending(Mobile initiator, Mobile target)
    {
        if (initiator == null || target == null)
        {
            return false;
        }

        // Check if there's a pending challenge FROM initiator TO target
        if (_outgoingChallenges.TryGetValue(initiator.Serial, out var outgoing))
        {
            if (outgoing.ExpirationTime >= Core.Now && outgoing.Target.Serial == target.Serial)
            {
                return true;
            }
            else if (outgoing.ExpirationTime < Core.Now)
            {
                _outgoingChallenges.Remove(initiator.Serial);
            }
        }

        return false;
    }

    public static void RegisterChallenge(Mobile initiator, Mobile target, DuelStoneComponent stone, int wager, bool isLoot)
    {
        if (initiator == null || target == null || stone == null)
        {
            return;
        }

        // Clean up any expired challenges for both parties
        CleanupExpiredChallenges();

        var challenge = new PendingChallenge(initiator, target, stone, wager, isLoot);
        _pendingChallenges[target.Serial] = challenge;
        _outgoingChallenges[initiator.Serial] = challenge;
    }

    private static void CleanupExpiredChallenges()
    {
        var expiredTargets = new List<Serial>();
        var expiredInitiators = new List<Serial>();

        foreach (var kvp in _pendingChallenges)
        {
            if (kvp.Value.ExpirationTime < Core.Now)
            {
                expiredTargets.Add(kvp.Key);
            }
        }

        foreach (var kvp in _outgoingChallenges)
        {
            if (kvp.Value.ExpirationTime < Core.Now)
            {
                expiredInitiators.Add(kvp.Key);
            }
        }

        foreach (var serial in expiredTargets)
        {
            _pendingChallenges.Remove(serial);
        }

        foreach (var serial in expiredInitiators)
        {
            _outgoingChallenges.Remove(serial);
        }
    }

    public static PendingChallenge GetPendingChallenge(Mobile target)
    {
        if (target == null)
        {
            return null;
        }

        if (!_pendingChallenges.TryGetValue(target.Serial, out var challenge))
        {
            return null;
        }

        // Clean up expired challenges
        if (challenge.ExpirationTime < Core.Now)
        {
            _pendingChallenges.Remove(target.Serial);
            return null;
        }

        return challenge;
    }

    public static void ClearChallenge(Mobile target)
    {
        if (target == null)
        {
            return;
        }

        if (_pendingChallenges.TryGetValue(target.Serial, out var challenge))
        {
            _outgoingChallenges.Remove(challenge.Initiator.Serial);
            _pendingChallenges.Remove(target.Serial);
        }
    }

    public static void ClearOutgoingChallenge(Mobile initiator)
    {
        if (initiator == null)
        {
            return;
        }

        if (_outgoingChallenges.TryGetValue(initiator.Serial, out var challenge))
        {
            _pendingChallenges.Remove(challenge.Target.Serial);
            _outgoingChallenges.Remove(initiator.Serial);
        }
    }

    private static void HandleDisconnect(Mobile m)
    {
        var context = FindContext(m);
        if (context == null)
        {
            return;
        }

        if (context.State == DuelState.InProgress)
        {
            var participant = context.GetParticipant(m);
            if (participant != null)
            {
                participant.RecordDeath();
                participant.IsEliminated = true;
            }

            context.EndDuel(null);
        }
        else if (context.State is DuelState.Waiting or DuelState.Countdown)
        {
            context.RemoveParticipant(m);
        }

        ClearPlayerContext(m);

        // Clear any pending challenges for disconnecting player
        ClearChallenge(m);
        ClearOutgoingChallenge(m);
    }
}

// Challenge data structure
public class PendingChallenge
{
    public Mobile Initiator { get; }
    public Mobile Target { get; }
    public DuelStoneComponent Stone { get; }
    public int Wager { get; }
    public bool IsLoot { get; }
    public DateTime ExpirationTime { get; }

    public PendingChallenge(Mobile initiator, Mobile target, DuelStoneComponent stone, int wager, bool isLoot)
    {
        Initiator = initiator;
        Target = target;
        Stone = stone;
        Wager = wager;
        IsLoot = isLoot;
        ExpirationTime = Core.Now + TimeSpan.FromSeconds(30); // 30 second timeout
    }
}
