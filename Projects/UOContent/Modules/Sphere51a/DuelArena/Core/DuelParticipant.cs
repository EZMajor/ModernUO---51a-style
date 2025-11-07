using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.DuelArena;

public class DuelParticipant
{
    private Mobile _mobile;
    private int _teamId;
    private int _kills;
    private int _deaths;
    private bool _ready;
    private Point3D _returnLocation;
    private Map _returnMap;
    private DuelType _duelType;

    public Mobile Mobile
    {
        get => _mobile;
        set => _mobile = value;
    }

    public int TeamId
    {
        get => _teamId;
        set => _teamId = value;
    }

    public int Kills
    {
        get => _kills;
        set => _kills = value;
    }

    public int Deaths
    {
        get => _deaths;
        set => _deaths = value;
    }

    public bool Ready
    {
        get => _ready;
        set => _ready = value;
    }

    public Point3D ReturnLocation
    {
        get => _returnLocation;
        set => _returnLocation = value;
    }

    public Map ReturnMap
    {
        get => _returnMap;
        set => _returnMap = value;
    }

    public DuelType DuelType
    {
        get => _duelType;
        set => _duelType = value;
    }

    public bool IsEliminated { get; set; }

    public DuelParticipant(Mobile mobile, int teamId, Point3D returnLocation, Map returnMap, DuelType duelType)
    {
        Mobile = mobile;
        TeamId = teamId;
        ReturnLocation = returnLocation;
        ReturnMap = returnMap;
        DuelType = duelType;
        Kills = 0;
        Deaths = 0;
        Ready = false;
        IsEliminated = false;
    }

    public void SetReady(bool ready)
    {
        Ready = ready;
    }

    public void RecordKill()
    {
        Kills++;
    }

    public void RecordDeath()
    {
        Deaths++;
        IsEliminated = true;
    }

    public void Restore()
    {
        if (Mobile is not { Deleted: false })
        {
            return;
        }

        Mobile.Hits = Mobile.HitsMax;
        Mobile.Stam = Mobile.StamMax;
        Mobile.Mana = Mobile.ManaMax;
        Mobile.Frozen = false;
        Mobile.Combatant = null;

        RemoveDeathRobe();
        ClearCriminalFlags();

        if (Mobile is PlayerMobile pm)
        {
            pm.SendEverything();
        }
    }

    // Deprecated: Use DuelContext.RemoveAggressions instead
    // This method is kept for compatibility but should not be called directly
    public void ClearAggressionAgainstParticipants(List<DuelParticipant> allParticipants)
    {
        // Aggressive clearing logic moved to DuelContext.RemoveAggressions()
        // which handles this more efficiently
    }

    private void RemoveDeathRobe()
    {
        if (Mobile is not PlayerMobile pm)
        {
            return;
        }

        // Check for death robe on outer torso layer
        var outerTorso = pm.FindItemOnLayer(Layer.OuterTorso);
        if (outerTorso != null)
        {
            // Delete if it's a robe (death robes are created as generic robes with specific properties)
            // or if it looks like a death robe
            var typeName = outerTorso.GetType().Name;
            if (typeName == "DeathRobe" || typeName == "Robe")
            {
                // For safety, check it's actually a death robe by checking the hue/name
                if (outerTorso.Name == null || outerTorso.Hue == 0)
                {
                    outerTorso.Delete();
                }
            }
        }
    }

    private void ClearCriminalFlags()
    {
        if (Mobile is not PlayerMobile pm)
        {
            return;
        }

        // Clear criminal action flag to remove grey/criminal status
        pm.Criminal = false;
    }

    private static void RestoreCorpseItemsToOwner(Corpse corpse, Mobile owner)
    {
        if (corpse == null || owner == null)
            return;

        var items = corpse.Items.ToArray();
        foreach (var item in items)
        {
            if (owner.Backpack != null)
            {
                owner.Backpack.DropItem(item);
            }
            else
            {
                item.MoveToWorld(owner.Location, owner.Map);
            }
        }

        corpse.Delete();
    }

    public void ReturnToOriginalLocation()
    {
        if (Mobile is not { Deleted: false })
        {
            return;
        }

        // LOOT DUELS: Teleport dead body first, then resurrect
        if (DuelType is DuelType.Loot1v1 or DuelType.Loot2v2)
        {
            // Teleport while dead
            if (ReturnMap != null && ReturnLocation != Point3D.Zero)
            {
                Mobile.MoveToWorld(ReturnLocation, ReturnMap);
            }

            // Then resurrect (corpse stays in arena)
            if (!Mobile.Alive)
            {
                Mobile.Resurrect();
            }
        }
        // MONEY DUELS: Resurrect with full gear restoration
        else
        {
            // Resurrect first
            if (!Mobile.Alive)
            {
                Mobile.Resurrect();
            }

            // Restore HP/mana after resurrection
            Mobile.Hits = Mobile.HitsMax;
            Mobile.Stam = Mobile.StamMax;
            Mobile.Mana = Mobile.ManaMax;

            // Restore all items from corpse
            var corpse = Mobile.Corpse as Items.Corpse;
            if (corpse != null && !corpse.Deleted)
            {
                RestoreCorpseItemsToOwner(corpse, Mobile);
            }

            // Then teleport
            if (ReturnMap != null && ReturnLocation != Point3D.Zero)
            {
                Mobile.MoveToWorld(ReturnLocation, ReturnMap);
            }
        }
    }
}
