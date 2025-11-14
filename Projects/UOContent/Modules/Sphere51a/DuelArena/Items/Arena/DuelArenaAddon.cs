using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Gumps;

namespace Server.Engines.DuelArena;

// Simple addon component wrapper for static tiles
[SerializationGenerator(0)]
public partial class DuelArenaComponent : AddonComponent
{
    [Constructible]
    public DuelArenaComponent(int itemID) : base(itemID)
    {
    }
}

/// <summary>
/// Consolidated duel stone component that handles all duel functionality directly.
/// This is the clickable gravestone (0xED7) that players interact with to start duels.
/// </summary>
[SerializationGenerator(1)]
public partial class DuelStoneComponent : AddonComponent
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DuelType _type = DuelType.Money1v1;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _entryCost = 1000;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.Administrator)]
    private bool _ladderEnabled;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _idleTimeSeconds = 15;

    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _maxIdleTimeSeconds = 30;

    // Arena is NOT serialized via SerializableField because it's not an ISerializable
    // Instead, we manually serialize it in the Deserialize method
    private DuelArena _arena;

    private DuelContext _activeContext;

    [CommandProperty(AccessLevel.GameMaster)]
    public DuelArena Arena
    {
        get => _arena;
        set => _arena = value;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public DuelContext ActiveContext => _activeContext;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsActive => _activeContext?.IsActive ?? false;

    [CommandProperty(AccessLevel.GameMaster)]
    public int ParticipantCount => _activeContext?.Participants.Count ?? 0;

    [Constructible]
    public DuelStoneComponent() : base(0xED7) // Gravestone visual
    {
        Movable = false;
        Hue = 1109;
        Name = "a duel stone";
        Weight = 0;

        _arena = DuelArena.CreateDefault();
    }

    private void Serialize(IGenericWriter writer, int version)
    {
        // Write arena manually (version 1)
        if (_arena != null)
        {
            writer.Write(true); // Arena exists
            writer.Write(_arena.Name);
            writer.Write(_arena.SpawnPoints.Length);
            foreach (var spawn in _arena.SpawnPoints)
            {
                writer.Write(spawn);
            }
            writer.Write(_arena.Bounds);
            writer.Write(_arena.Map);
            writer.Write(_arena.ExitLocation);
            writer.Write(_arena.MaxPlayers);
        }
        else
        {
            writer.Write(false); // No arena
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        switch (version)
        {
            case 1:
                // Read arena manually
                bool hasArena = reader.ReadBool();
                if (hasArena)
                {
                    string name = reader.ReadString();
                    int spawnCount = reader.ReadInt();
                    Point3D[] spawns = new Point3D[spawnCount];
                    for (int i = 0; i < spawnCount; i++)
                    {
                        spawns[i] = reader.ReadPoint3D();
                    }
                    Rectangle2D bounds = reader.ReadRect2D();
                    Map map = reader.ReadMap();
                    Point3D exitLoc = reader.ReadPoint3D();
                    int maxPlayers = reader.ReadInt();

                    _arena = new DuelArena(name, spawns, bounds, map, exitLoc, maxPlayers);
                }
                else
                {
                    _arena = DuelArena.CreateDefault();
                }
                break;
            case 0:
                // Version 0 didn't have arena serialization
                _arena = DuelArena.CreateDefault();
                break;
        }

        // Ensure arena is created if null after deserialization
        _arena ??= DuelArena.CreateDefault();
    }

    public override void OnSingleClick(Mobile from)
    {
        if (Type == DuelType.Money1v1)
        {
            LabelTo(from, $"1vs1 for [{EntryCost}]");
        }
        else if (Type == DuelType.Money2v2)
        {
            LabelTo(from, $"2vs2 for [{EntryCost}]");
        }
        else if (Type == DuelType.Loot1v1)
        {
            LabelTo(from, "1vs1 for [loot]");
        }
        else if (Type == DuelType.Loot2v2)
        {
            LabelTo(from, "2vs2 for [loot]");
        }
        else
        {
            LabelTo(from, "Stone error, page a GM!");
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        // Range check using component's world location (FIXES THE BUG!)
        if (!from.InRange(GetWorldLocation(), 4))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that
            return;
        }

        if (from.AccessLevel != AccessLevel.Player)
        {
            from.SendMessage("Only players can join duels!");
            return;
        }

        if (!CanInitiateChallenge(from, out string message))
        {
            from.SendMessage(message);
            return;
        }

        // Show wager selection gump
        from.SendGump(new DuelWagerGump(this));
    }

    private bool CanInitiateChallenge(Mobile from, out string message)
    {
        message = null;

        if (_arena == null)
        {
            message = "This stone has not been configured. Page a GM.";
            return false;
        }

        if (_arena.SpawnPoints == null || _arena.SpawnPoints.Length < 2)
        {
            message = "This stone has invalid spawn points. Page a GM.";
            return false;
        }

        if (from.Mounted)
        {
            message = "You cannot challenge while mounted!";
            return false;
        }

        if (!from.Alive)
        {
            message = "You must be alive to challenge someone!";
            return false;
        }

        if (from.Hits != from.HitsMax)
        {
            message = "You must have full health to challenge someone!";
            return false;
        }

        if (from.Combatant != null)
        {
            message = "You cannot challenge while in combat!";
            return false;
        }

        // Check if player is already in a duel
        var existingContext = DuelSystem.FindContext(from);
        if (existingContext != null)
        {
            message = "You are already in a duel!";
            return false;
        }

        // Check if stone is busy
        if (_activeContext != null && _activeContext.State is DuelState.Countdown or DuelState.InProgress or DuelState.Ending or DuelState.LootPhase)
        {
            message = "This stone is busy!";
            return false;
        }

        return true;
    }

    public void OnWagerSelected(Mobile from, int wager, bool isLoot)
    {
        if (from == null || from.Deleted)
        {
            return;
        }

        // Show target cursor
        from.LocalOverheadMessage(MessageType.Regular, 0x3B2, false, "Target a challenger");
        from.Target = new DuelChallengeTarget(this, wager, isLoot);
    }

    public void ProcessChallenge(Mobile initiator, Mobile target, int wager, bool isLoot)
    {
        if (initiator == null || target == null || Deleted)
        {
            return;
        }

        // Re-validate initiator and target
        if (!ValidateChallengeParticipants(initiator, target, wager, isLoot, true, out string errorMessage))
        {
            initiator.SendMessage(errorMessage);
            return;
        }

        // Charge gold from initiator (if not loot)
        if (!isLoot && wager > 0)
        {
            if (!ChargeGold(initiator, wager))
            {
                initiator.SendMessage($"You do not have enough gold! You need {wager} gold.");
                return;
            }

            initiator.SendMessage($"{wager} gold has been withdrawn from your account.");
        }

        // Register the challenge
        DuelSystem.RegisterChallenge(initiator, target, this, wager, isLoot);

        // Send invite gump to target
        target.SendGump(new DuelInviteGump(target, wager, isLoot));

        // Notify initiator
        initiator.SendMessage($"Challenge sent to {target.Name}. Waiting for response...");
    }

    private bool ValidateChallengeParticipants(Mobile initiator, Mobile target, int wager, bool isLoot, bool checkInitiatorGold, out string errorMessage)
    {
        errorMessage = null;

        // Check initiator state
        if (initiator == null || initiator.Deleted)
        {
            errorMessage = "You are no longer in a valid state to duel!";
            return false;
        }

        if (!initiator.Alive)
        {
            errorMessage = "You are no longer in a valid state to duel!";
            return false;
        }

        if (initiator.Hits != initiator.HitsMax)
        {
            errorMessage = "You must have full health to duel!";
            return false;
        }

        if (initiator.Mounted)
        {
            errorMessage = "You cannot duel while mounted!";
            return false;
        }

        if (initiator.Combatant != null)
        {
            errorMessage = "You cannot challenge while in combat!";
            return false;
        }

        // Check if initiator is already in a duel
        if (DuelSystem.FindContext(initiator) != null)
        {
            errorMessage = "You are already in a duel!";
            return false;
        }

        // Check target state
        if (target == null || target.Deleted)
        {
            errorMessage = "That player is not in a valid state to duel!";
            return false;
        }

        if (!target.Alive)
        {
            errorMessage = "That player is not in a valid state to duel!";
            return false;
        }

        if (target.Hits != target.HitsMax)
        {
            errorMessage = "That player must have full health to duel!";
            return false;
        }

        if (target.Mounted)
        {
            errorMessage = "That player cannot duel while mounted!";
            return false;
        }

        if (target.Combatant != null)
        {
            errorMessage = "That player is currently in combat!";
            return false;
        }

        // Check if target is already in a duel
        if (DuelSystem.FindContext(target) != null)
        {
            errorMessage = "That player is already in a duel!";
            return false;
        }

        // Check for pending challenges on target
        if (DuelSystem.HasPendingChallenge(target))
        {
            errorMessage = "That player already has a pending duel invitation!";
            return false;
        }

        // Check if initiator has an outgoing challenge
        if (DuelSystem.HasOutgoingChallenge(initiator))
        {
            errorMessage = "You already have a pending duel challenge!";
            return false;
        }

        // Verify gold one more time before accepting (state may have changed)
        if (!isLoot && wager > 0)
        {
            if (checkInitiatorGold)
            {
                var initiatorTotal = (initiator.Backpack?.TotalGold ?? 0) + (initiator.BankBox?.TotalGold ?? 0);
                if (initiatorTotal < wager)
                {
                    errorMessage = "You no longer have enough gold for this wager!";
                    return false;
                }
            }

            var targetTotal = (target.Backpack?.TotalGold ?? 0) + (target.BankBox?.TotalGold ?? 0);
            if (targetTotal < wager)
            {
                errorMessage = "That player no longer has enough gold for this wager!";
                return false;
            }
        }

        return true;
    }

    public void OnTargetAccepted(Mobile initiator, Mobile target, int wager, bool isLoot)
    {
        if (initiator == null || target == null || Deleted)
        {
            return;
        }

        // Re-validate both players
        if (!ValidateChallengeParticipants(initiator, target, wager, isLoot, false, out string errorMessage))
        {
            initiator.SendMessage($"Duel cancelled: {errorMessage}");
            target.SendMessage($"Duel cancelled: {errorMessage}");

            // Refund initiator if gold was charged
            if (!isLoot && wager > 0)
            {
                RefundGoldToPlayer(initiator, wager);
            }
            return;
        }

        // Charge gold from target (if not loot)
        if (!isLoot && wager > 0)
        {
            if (!ChargeGold(target, wager))
            {
                target.SendMessage($"You do not have enough gold! You need {wager} gold.");
                initiator.SendMessage($"{target.Name} does not have enough gold for the wager.");

                // Refund initiator
                RefundGoldToPlayer(initiator, wager);
                return;
            }

            target.SendMessage($"{wager} gold has been withdrawn from your account.");
        }

        // Notify both players
        target.SendMessage($"You have accepted {initiator.Name}'s duel challenge!");
        initiator.SendMessage($"{target.Name} has accepted your duel challenge!");

        // Start pre-teleport countdown for both players
        StartPreTeleportCountdown(initiator, target, wager, isLoot);
    }

    public void OnTargetDeclined(Mobile initiator, Mobile target, int wager, bool isLoot)
    {
        if (initiator == null || target == null)
        {
            return;
        }

        target.SendMessage("You have declined the duel invitation.");
        initiator.SendMessage($"{target.Name} has declined your duel challenge.");

        // Refund initiator if gold was charged
        if (!isLoot && wager > 0)
        {
            RefundGoldToPlayer(initiator, wager);
        }
    }

    private void StartPreTeleportCountdown(Mobile initiator, Mobile target, int wager, bool isLoot)
    {
        var countdown = new CountdownHelper(5);

        Timer.StartTimer(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            5,
            () =>
            {
                countdown.Remaining--;

                if (countdown.Remaining > 0)
                {
                    // Show private overhead messages
                    initiator.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, $"Teleporting in {countdown.Remaining}...");
                    target.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, $"Teleporting in {countdown.Remaining}...");
                }
                else
                {
                    // Teleport and start duel
                    InitiateDuel(initiator, target, wager, isLoot);
                }
            },
            out _
        );
    }

    private class CountdownHelper
    {
        public int Remaining { get; set; }
        public CountdownHelper(int initial) => Remaining = initial;
    }

    private void InitiateDuel(Mobile initiator, Mobile target, int wager, bool isLoot)
    {
        // Final validation before teleport
        if (!initiator.Alive || initiator.Deleted || !target.Alive || target.Deleted)
        {
            initiator?.SendMessage("Duel cancelled - one of the participants is no longer available.");
            target?.SendMessage("Duel cancelled - one of the participants is no longer available.");

            // Refund both
            if (!isLoot && wager > 0)
            {
                RefundGoldToPlayer(initiator, wager);
                RefundGoldToPlayer(target, wager);
            }
            return;
        }

        // Create duel context
        var duelType = isLoot ? DuelType.Loot1v1 : DuelType.Money1v1;
        _activeContext = new DuelContext(_arena, duelType, wager, LadderEnabled);
        DuelSystem.RegisterContext(_activeContext);

        // Add both participants
        _activeContext.AddParticipant(initiator);
        _activeContext.AddParticipant(target);

        DuelSystem.SetPlayerContext(initiator, _activeContext);
        DuelSystem.SetPlayerContext(target, _activeContext);

        // Teleport to arena
        var spawnPoints = _arena.SpawnPoints;
        initiator.MoveToWorld(spawnPoints[0], _arena.Map);
        target.MoveToWorld(spawnPoints[1], _arena.Map);

        // Start 10-second frozen countdown
        _activeContext.StartCountdown();
    }

    private bool ChargeGold(Mobile from, int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        var totalGold = from.Backpack?.TotalGold ?? 0;
        var bankGold = from.BankBox?.TotalGold ?? 0;

        if (totalGold + bankGold < amount)
        {
            return false;
        }

        if (totalGold >= amount)
        {
            from.Backpack.ConsumeTotal(typeof(Gold), amount);
        }
        else
        {
            from.Backpack.ConsumeTotal(typeof(Gold), totalGold);
            from.BankBox.ConsumeTotal(typeof(Gold), amount - totalGold);
        }

        return true;
    }

    public void RefundGoldToPlayer(Mobile from, int amount)
    {
        if (amount <= 0 || from == null || from.Deleted)
        {
            return;
        }

        // Refund to bank (safe, prevents weight issues, matches UO standards)
        if (from.BankBox != null)
        {
            from.BankBox.DropItem(new Gold(amount));
            from.SendMessage($"{amount} gold has been refunded to your bank.");
        }
        else if (from.Backpack != null)
        {
            // Fallback only if bank is somehow unavailable
            from.Backpack.DropItem(new Gold(amount));
            from.SendMessage($"{amount} gold has been refunded to your backpack.");
        }
    }

    private void CleanupContext()
    {
        if (_activeContext != null)
        {
            _activeContext.StopTimers();
            // Player contexts are now cleared in DuelContext.Cleanup
            // DuelSystem.UnregisterContext(_activeContext);
            _activeContext = null;
        }
    }

    public void OnParticipantDeath(Mobile dead, Mobile killer)
    {
        if (_activeContext == null || !_activeContext.IsParticipant(dead))
        {
            return;
        }

        var deadParticipant = _activeContext.GetParticipant(dead);
        if (deadParticipant != null)
        {
            deadParticipant.RecordDeath();
        }

        if (killer != null && _activeContext.IsParticipant(killer))
        {
            var killerParticipant = _activeContext.GetParticipant(killer);
            if (killerParticipant != null)
            {
                killerParticipant.RecordKill();
            }
        }

        Timer.DelayCall(TimeSpan.FromSeconds(5), () => HandlePostDeath(dead));

        CheckForWinner();
    }

    private void HandlePostDeath(Mobile dead)
    {
        if (_activeContext == null || !_activeContext.IsParticipant(dead))
        {
            return;
        }

        var participant = _activeContext.GetParticipant(dead);
        if (participant == null)
        {
            return;
        }

        // ReturnToOriginalLocation handles resurrection differently for Money vs Loot duels
        participant.ReturnToOriginalLocation();
        participant.Restore();
        DuelSystem.ClearPlayerContext(dead);
    }

    private void CheckForWinner()
    {
        if (_activeContext == null || _activeContext.State != DuelState.InProgress)
        {
            return;
        }

        DuelParticipant winner = null;
        int aliveCount = 0;

        foreach (var p in _activeContext.Participants)
        {
            if (!p.IsEliminated && p.Mobile is { Alive: true, Deleted: false })
            {
                winner = p;
                aliveCount++;
            }
        }

        if (aliveCount == 1 && winner != null)
        {
            _activeContext.EndDuel(winner);
            // CleanupContext is now scheduled in EndDuel for money duels
        }
        else if (aliveCount == 0)
        {
            _activeContext.EndDuel(null);
            // CleanupContext is now scheduled in EndDuel for money duels
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();
        CleanupContext();
    }
}

[TypeAlias("DuelArena", "duelarena")]
[SerializationGenerator(0)]
public partial class DuelArenaAddon : BaseAddon
{
    [SerializableField(0)]
    private List<DuelStoneComponent> _duelStoneComponents = new();

    [Constructible]
    public DuelArenaAddon()
    {
        BuildArenaComplex();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        // Reconfigure all stones after deserialization to ensure
        // arena coordinates match the addon's current location
        ReconfigureAllStones();
    }

    private void BuildArenaComplex()
    {
        // Build single 8x8 duel pit
        // Interior: 8x8
        // Total footprint: 10x10 (including walls)
        // Wall IDs: 223 (0xDF) = NW corner, 220 (0xDC) = SE corner

        // Build the outer perimeter
        BuildOuterWalls();

        // Add floor, spawns, and single duel stone
        // Arena interior starts at (1,1) and is 8x8
        BuildArenaInterior(1, 1, DuelType.Money1v1, 1000);
    }

    private void BuildOuterWalls()
    {
        // Single 10x10 footprint (8x8 interior + 2 wall tiles)
        // Wall coordinates: 0-9 for both X and Y
        // Interior coordinates: 1-8 for both X and Y
        // Verified from StaticTarget reference:
        //   221 (0xDD) = VERTICAL wall segments
        //   222 (0xDE) = HORIZONTAL wall segments
        //   223 (0xDF) = NW/SW corner "stone post"
        //   220 (0xDC) = NE/SE corner

        // Northwest corner - 223 (0xDF) "stone post"
        AddComponent(new DuelArenaComponent(0xDF), 0, 0, 0);

        // North wall (horizontal) - 222 (0xDE)
        for (int x = 1; x <= 9; x++)
        {
            AddComponent(new DuelArenaComponent(0xDE), x, 0, 0);
        }

        // West wall (vertical) - 221 (0xDD)
        for (int y = 1; y <= 9; y++)
        {
            AddComponent(new DuelArenaComponent(0xDD), 0, y, 0);
        }

        // East wall (vertical) - 221 (0xDD)
        for (int y = 1; y <= 8; y++)
        {
            AddComponent(new DuelArenaComponent(0xDD), 9, y, 0);
        }

        // Southwest corner - 223 (0xDF) "stone post"
        AddComponent(new DuelArenaComponent(0xDF), 0, 9, 0);

        // South wall (horizontal) - 222 (0xDE)
        for (int x = 1; x <= 8; x++)
        {
            AddComponent(new DuelArenaComponent(0xDE), x, 9, 0);
        }

        // Southeast corner - THIS WAS CORRECT in first screenshot
        AddComponent(new DuelArenaComponent(0xDC), 9, 9, 0);
    }

    private void BuildArenaInterior(int startX, int startY, DuelType type, int cost)
    {
        // Single 8x8 interior
        int width = 9;
        int height = 9;

        // Add floor tiles (8x8 interior from startX=1, startY=1 to 8,8)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                AddComponent(new DuelArenaComponent(0x0519), startX + x, startY + y, 0);
            }
        }

        // Add spawn markers - opposite corners
        // Spawn 1: Top-left corner of interior (x=1, y=1)
        AddComponent(new DuelArenaComponent(0x1822), startX + 0, startY + 0, 0);
        // Spawn 2: Bottom-right corner of interior (x=8, y=8)
        AddComponent(new DuelArenaComponent(0x1823), startX + width - 2, startY + height - 2, 0);

        // Place single duel stone on EAST side at midpoint
        // East wall is at x=9, midpoint of 8x8 interior is y=4.5, so use y=4 or y=5
        // Using y = startY + 3 (which is y=4 in world coords) for midpoint
        int stoneX = 10;  // One tile outside east wall (wall is at x=9)
        int stoneY = startY + 4;  // Midpoint of 8-tile height (0-7, so middle is 3.5, use 3)

        // Create consolidated DuelStoneComponent directly
        var stoneComponent = new DuelStoneComponent
        {
            Type = type,
            EntryCost = cost,
            Movable = false,
            Name = "Duel Stone"
        };

        // Configure arena (use interior bounds for region)
        ConfigureStoneArena(stoneComponent, startX, startY, width, height);

        // Add the component to the addon (this is the visible and clickable gravestone)
        AddComponent(stoneComponent, stoneX, stoneY, 0);
        _duelStoneComponents.Add(stoneComponent);
    }

    private void ConfigureStoneArena(DuelStoneComponent stone, int interiorStartX, int interiorStartY, int width, int height)
    {
        // Get the addon's world location
        var addonLoc = Location;
        var addonMap = Map;

        if (addonMap == null || addonMap == Map.Internal)
        {
            addonMap = Map.Felucca; // Default to Felucca
        }

        // Spawn points are at opposite corners within the interior
        // Spawn 1: Top-left corner (1,1)
        // Spawn 2: Bottom-right corner (8,8)
        Point3D spawn1 = new Point3D(addonLoc.X + interiorStartX + 0, addonLoc.Y + interiorStartY + 0, addonLoc.Z);
        Point3D spawn2 = new Point3D(addonLoc.X + interiorStartX + width - 2, addonLoc.Y + interiorStartY + height - 2, addonLoc.Z);
        Point3D exitLoc = new Point3D(addonLoc.X + interiorStartX + 0, addonLoc.Y + interiorStartY + height - 1, addonLoc.Z);

        // Create arena bounds (interior area)
        var bounds = new Rectangle2D(
            addonLoc.X + interiorStartX,
            addonLoc.Y + interiorStartY,
            width,
            height
        );

        // Create and configure arena
        var arena = new DuelArena(
            "Arena",
            [spawn1, spawn2],
            bounds,
            addonMap,
            exitLoc,
            2 // Max 2 players for this arena
        );

        stone.Arena = arena;

        // Create protected region for this arena to prevent karma/murder/criminal penalties
        arena.CreateRegion();
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        // Delete all arena regions first
        foreach (var component in _duelStoneComponents)
        {
            component.Arena?.DeleteRegion();
        }

        // Clean up duel stone components
        foreach (var component in _duelStoneComponents)
        {
            if (component != null && !component.Deleted)
            {
                component.Delete();
            }
        }

        _duelStoneComponents.Clear();
    }

    public override void OnLocationChange(Point3D oldLocation)
    {
        base.OnLocationChange(oldLocation);

        // Reconfigure all stones with new locations
        ReconfigureAllStones();
    }

    public override void OnMapChange()
    {
        base.OnMapChange();

        // Reconfigure all stones with new map
        ReconfigureAllStones();
    }

    private void ReconfigureAllStones()
    {
        // This is called when addon is moved
        // We need to rebuild the arena configuration for the single 8x8 arena
        // Interior starts at (1,1) and is 9x9

        if (_duelStoneComponents.Count > 0)
        {
            var stone = _duelStoneComponents[0];
            if (stone != null && !stone.Deleted)
            {
                ConfigureStoneArena(stone, 1, 1, 9, 9);
            }
        }
    }
}
