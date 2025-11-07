using Server.Targeting;

namespace Server.Engines.DuelArena;

public class DuelChallengeTarget : Target
{
    private readonly DuelStoneComponent _stone;
    private readonly int _wager;
    private readonly bool _isLoot;

    public DuelChallengeTarget(DuelStoneComponent stone, int wager, bool isLoot)
        : base(12, false, TargetFlags.None)
    {
        _stone = stone;
        _wager = wager;
        _isLoot = isLoot;
    }

    protected override void OnTarget(Mobile from, object targeted)
    {
        if (from == null || _stone == null || _stone.Deleted)
        {
            return;
        }

        if (targeted is not Mobile target)
        {
            from.SendMessage("You must target a player.");
            return;
        }

        if (target == from)
        {
            from.SendMessage("You cannot challenge yourself!");
            return;
        }

        // Validate target
        if (!ValidateTarget(from, target, out string errorMessage))
        {
            from.SendMessage(errorMessage);
            return;
        }

        // Process the challenge
        _stone.ProcessChallenge(from, target, _wager, _isLoot);
    }

    protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
    {
        from.SendMessage("Duel challenge cancelled.");
    }

    private bool ValidateTarget(Mobile from, Mobile target, out string errorMessage)
    {
        errorMessage = null;

        // Check if target is a player
        if (target.AccessLevel != AccessLevel.Player)
        {
            errorMessage = "You can only challenge players!";
            return false;
        }

        // Check if initiator already has an outgoing challenge
        if (DuelSystem.HasOutgoingChallenge(from))
        {
            errorMessage = "You already have a pending duel challenge. Wait for a response or let it expire.";
            return false;
        }

        // Check if there's already a pending challenge between these two players
        if (DuelSystem.IsChallengePending(from, target))
        {
            errorMessage = "You have already challenged this player!";
            return false;
        }

        // Check if target is alive
        if (!target.Alive)
        {
            errorMessage = "That player is dead!";
            return false;
        }

        // Check if target has full health
        if (target.Hits != target.HitsMax)
        {
            errorMessage = "That player must have full health to duel!";
            return false;
        }

        // Check if target is mounted
        if (target.Mounted)
        {
            errorMessage = "That player cannot duel while mounted!";
            return false;
        }

        // Check if target is already in a duel
        var targetContext = DuelSystem.FindContext(target);
        if (targetContext != null)
        {
            errorMessage = "That player is already in a duel!";
            return false;
        }

        // Check if target is in combat
        if (target.Combatant != null)
        {
            errorMessage = "That player is currently in combat!";
            return false;
        }

        // Check if target has a pending challenge
        if (DuelSystem.HasPendingChallenge(target))
        {
            errorMessage = "That player already has a pending duel invitation!";
            return false;
        }

        // Check if initiator has sufficient gold (if not loot)
        if (!_isLoot && _wager > 0)
        {
            var totalGold = from.Backpack?.TotalGold ?? 0;
            var bankGold = from.BankBox?.TotalGold ?? 0;

            if (totalGold + bankGold < _wager)
            {
                errorMessage = $"You do not have enough gold! You need {_wager} gold.";
                return false;
            }
        }

        // Check if target has sufficient gold (if not loot)
        if (!_isLoot && _wager > 0)
        {
            var totalGold = target.Backpack?.TotalGold ?? 0;
            var bankGold = target.BankBox?.TotalGold ?? 0;

            if (totalGold + bankGold < _wager)
            {
                errorMessage = "That player does not have enough gold for this wager!";
                return false;
            }
        }

        return true;
    }
}
