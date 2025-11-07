using Server.Multis;
using Server.Regions;
using Server.Targeting;

namespace Server.Engines.DuelArena;

/// <summary>
/// Custom target for transparent duel arena placement that emulates house placement behavior
/// Uses client-side preview rendering with MultiTarget instead of world objects
/// </summary>
public class ArenaTarget : MultiTarget
{
    private readonly DuelArenaDeed _deed;

    public ArenaTarget(DuelArenaDeed deed)
        : base(DuelArenaDeed.ArenaMultiID, DuelArenaDeed.ArenaOffset)
    {
        _deed = deed;
        CheckLOS = false;
    }

    protected override void OnTarget(Mobile from, object targeted)
    {
        if (_deed.Deleted || !_deed.IsChildOf(from.Backpack))
        {
            return;
        }

        if (targeted is not IPoint3D point)
        {
            from.SendMessage("That is not a valid location.");
            return;
        }

        var p = new Point3D(point);
        var map = from.Map;

        if (map == null || map == Map.Internal)
        {
            from.SendMessage("You cannot place that here.");
            return;
        }

        var reg = Region.Find(p, map);

        if (from.AccessLevel >= AccessLevel.GameMaster || reg.AllowHousing(from, p))
        {
            OnPlacement(from, p);
        }
        else if (reg.IsPartOf<TempNoHousingRegion>())
        {
            from.SendLocalizedMessage(501270); // Lord British has decreed a 'no build' period, thus you cannot build this house at this time.
        }
        else if (reg.IsPartOf<TreasureRegion>() || reg.IsPartOf<HouseRegion>())
        {
            from.SendLocalizedMessage(1043287); // The house could not be created here.  Either something is blocking the house, or the house would not be on valid terrain.
        }
        else if (reg.IsPartOf<HouseRaffleRegion>())
        {
            from.SendLocalizedMessage(1150493); // You must have a deed for this plot of land in order to build here.
        }
        else
        {
            from.SendLocalizedMessage(501265); // Housing can not be created in this area.
        }
    }

    protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
    {
        from.SendMessage("Arena placement cancelled.");
    }

    public void OnPlacement(Mobile from, Point3D p)
    {
        if (_deed.Deleted || !_deed.IsChildOf(from.Backpack))
        {
            return;
        }

        if (from.AccessLevel < AccessLevel.GameMaster && BaseHouse.HasAccountHouse(from))
        {
            from.SendLocalizedMessage(501271); // You already own a house, you may not place another!
            return;
        }

        var multi = MultiData.GetComponents(DuelArenaDeed.ArenaMultiID);
        var center = new Point3D(p.X - DuelArenaDeed.ArenaOffset.X - multi.Center.X, p.Y - DuelArenaDeed.ArenaOffset.Y - multi.Center.Y, p.Z - DuelArenaDeed.ArenaOffset.Z);

        // Create and place the duel arena addon directly
        var addon = new DuelArenaAddon();
        addon.MoveToWorld(center, from.Map);

        from.SendMessage("Duel arena placed successfully!");
        from.SendMessage("Single 8x8 duel pit with one duel stone on the east side.");

        _deed.Delete(); // Consume the deed
    }
}
