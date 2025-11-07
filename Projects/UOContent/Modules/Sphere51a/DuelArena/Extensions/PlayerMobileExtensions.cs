using System;
using ModernUO.CodeGeneratedEvents;
using Server.Engines.DuelArena;

namespace Server.Mobiles;

public static class DuelDeathHelper
{
    [OnEvent(nameof(PlayerMobile.PlayerDeathEvent))]
    public static void OnPlayerDeath(PlayerMobile m)
    {
        if (m == null)
        {
            return;
        }

        var context = DuelSystem.FindContext(m);
        if (context == null)
        {
            return;
        }

        if (context.State != DuelState.InProgress)
        {
            return;
        }

        var participant = context.GetParticipant(m);
        if (participant == null)
        {
            return;
        }

        participant.RecordDeath();

        var killer = m.LastKiller;
        if (killer != null && context.IsParticipant(killer))
        {
            var killerParticipant = context.GetParticipant(killer);
            if (killerParticipant != null)
            {
                killerParticipant.RecordKill();
            }

            // No fame or karma awarded during duels - these are consensual practice fights
            m.Combatant = null;
            killer.Combatant = null;
        }

        CheckForDuelWinner(context);
    }

    private static void CheckForDuelWinner(DuelContext context)
    {
        if (context == null || context.State != DuelState.InProgress)
        {
            return;
        }

        DuelParticipant winner = null;
        int aliveCount = 0;
        int team0Alive = 0;
        int team1Alive = 0;

        foreach (var p in context.Participants)
        {
            if (!p.IsEliminated && p.Mobile is { Alive: true, Deleted: false })
            {
                winner = p;
                aliveCount++;

                if (p.TeamId == 0)
                {
                    team0Alive++;
                }
                else
                {
                    team1Alive++;
                }
            }
        }

        bool teamWin = false;
        if (context.DuelType is DuelType.Money2v2 or DuelType.Loot2v2)
        {
            if (team0Alive == 0 && team1Alive > 0)
            {
                teamWin = true;
            }
            else if (team1Alive == 0 && team0Alive > 0)
            {
                teamWin = true;
            }
        }

        if (aliveCount == 1 && winner != null)
        {
            context.EndDuel(winner);
        }
        else if (aliveCount == 0)
        {
            context.EndDuel(null);
        }
        else if (teamWin && winner != null)
        {
            context.EndDuel(winner);
        }
    }
}
