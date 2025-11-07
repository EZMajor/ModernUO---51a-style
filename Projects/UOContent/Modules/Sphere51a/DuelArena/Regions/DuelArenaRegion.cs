using Server.Mobiles;
using Server.Regions;

namespace Server.Engines.DuelArena;

/// <summary>
/// Custom region for duel arenas that prevents karma loss, murder counts, and criminal flagging
/// during consensual duels while blocking all other harmful actions.
/// </summary>
public class DuelArenaRegion : Region
{
    public DuelArena Arena { get; }

    public DuelArenaRegion(DuelArena arena) : base(
        arena.Name,
        arena.Map,
        Region.DefaultPriority,
        arena.Bounds
    )
    {
        Arena = arena;
        Register();
    }

    /// <summary>
    /// Allow harmful actions without criminal flagging only between active duel participants.
    /// All other harmful actions are blocked.
    /// </summary>
    public override bool AllowHarmful(Mobile from, Mobile target)
    {
        // Only allow harm between active duel participants
        var fromContext = DuelSystem.FindContext(from);
        var targetContext = DuelSystem.FindContext(target);

        if (fromContext != null && targetContext != null && fromContext == targetContext)
        {
            if (fromContext.State == DuelState.InProgress)
            {
                // Allow combat without criminal flagging
                return true;
            }
        }

        // Block all other harmful actions
        from.SendMessage("You cannot attack in this arena unless you're in an active duel.");
        return false;
    }

    /// <summary>
    /// Prevent murder counts and karma loss for duel deaths.
    /// </summary>
    public override bool OnBeforeDeath(Mobile m)
    {
        var context = DuelSystem.FindContext(m);

        if (context?.State == DuelState.InProgress)
        {
            // Find the killer
            var killer = m.FindMostRecentDamager(false);

            if (killer is PlayerMobile pm && m is PlayerMobile victim)
            {
                // Clear any criminal/aggressor status from this duel
                victim.CriminalAction(false);
                pm.CriminalAction(false);

                // Clear aggressor relationships between the two duelists
                ClearDuelAggressors(victim, pm);
            }
        }

        return base.OnBeforeDeath(m);
    }

    private void ClearDuelAggressors(Mobile victim, Mobile killer)
    {
        // Remove aggressor entries between these two duelists
        if (victim.Aggressors != null)
        {
            var toRemove = new System.Collections.Generic.List<AggressorInfo>();
            foreach (var aI in victim.Aggressors)
            {
                if ((aI.Attacker == victim && aI.Defender == killer) ||
                    (aI.Attacker == killer && aI.Defender == victim))
                {
                    toRemove.Add(aI);
                }
            }
            foreach (var aI in toRemove)
            {
                victim.Aggressors.Remove(aI);
            }
        }

        if (victim.Aggressed != null)
        {
            var toRemove = new System.Collections.Generic.List<AggressorInfo>();
            foreach (var aI in victim.Aggressed)
            {
                if ((aI.Attacker == victim && aI.Defender == killer) ||
                    (aI.Attacker == killer && aI.Defender == victim))
                {
                    toRemove.Add(aI);
                }
            }
            foreach (var aI in toRemove)
            {
                victim.Aggressed.Remove(aI);
            }
        }

        if (killer.Aggressors != null)
        {
            var toRemove = new System.Collections.Generic.List<AggressorInfo>();
            foreach (var aI in killer.Aggressors)
            {
                if ((aI.Attacker == victim && aI.Defender == killer) ||
                    (aI.Attacker == killer && aI.Defender == victim))
                {
                    toRemove.Add(aI);
                }
            }
            foreach (var aI in toRemove)
            {
                killer.Aggressors.Remove(aI);
            }
        }

        if (killer.Aggressed != null)
        {
            var toRemove = new System.Collections.Generic.List<AggressorInfo>();
            foreach (var aI in killer.Aggressed)
            {
                if ((aI.Attacker == victim && aI.Defender == killer) ||
                    (aI.Attacker == killer && aI.Defender == victim))
                {
                    toRemove.Add(aI);
                }
            }
            foreach (var aI in toRemove)
            {
                killer.Aggressed.Remove(aI);
            }
        }
    }

    /// <summary>
    /// Prevent karma loss during duel combat by not propagating combatant changes
    /// to parent regions during active duels.
    /// </summary>
    public override bool OnCombatantChange(Mobile m, Mobile old, Mobile newMobile)
    {
        var context = DuelSystem.FindContext(m);

        if (context?.State == DuelState.InProgress)
        {
            // Allow combatant change but don't propagate to parent region
            // This prevents karma loss from combat
            return true;
        }

        return base.OnCombatantChange(m, old, newMobile);
    }

    /// <summary>
    /// Notify players when they enter the arena.
    /// </summary>
    public override void OnEnter(Mobile m)
    {
        base.OnEnter(m);
        m.SendMessage("You have entered a duel arena. Only consensual duels are permitted here.");
    }

    /// <summary>
    /// Handle players leaving during active duels.
    /// </summary>
    public override void OnExit(Mobile m)
    {
        base.OnExit(m);

        var context = DuelSystem.FindContext(m);
        if (context?.State == DuelState.InProgress)
        {
            // Player is leaving during an active duel - this should be handled by the duel system
            // The duel death/forfeit logic will handle cleanup
            m.SendMessage(0x22, "Leaving the arena during a duel counts as forfeit!");
        }
    }

    /// <summary>
    /// Cleanup the region by unregistering it.
    /// </summary>
    public void Delete()
    {
        Unregister();
    }
}
