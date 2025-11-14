/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: BaseWeapon.Sphere51a.cs
 *
 * Description: Partial class extension for BaseWeapon to implement Sphere51a-style
 *              combat mechanics without modifying core ModernUO files.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using Server.Mobiles;
using Server.Network;
using Server.SkillHandlers;
using Server.Spells;
using Server.Spells.Bushido;
using Server.Spells.Chivalry;
using Server.Spells.Necromancy;
using Server.Spells.Ninjitsu;
using Server.Spells.Spellweaving;
using Server.Modules.Sphere51a.Configuration;
using Server.Modules.Sphere51a.Extensions;

namespace Server.Items;

public partial class BaseWeapon
{
    /// <summary>
    /// Handles double-click on weapon items.
    /// When Sphere51a is enabled, allows equipping from backpack, ground, or containers.
    /// </summary>
    /// <param name="from">The mobile double-clicking the item</param>
    public override void OnDoubleClick(Mobile from)
    {
        // If Sphere51a is not enabled, use default behavior
        if (!SphereConfiguration.Enabled)
        {
            base.OnDoubleClick(from);
            return;
        }

        // Check if item is already equipped
        if (Parent == from && from.FindItemOnLayer(Layer) == this)
        {
            // Already equipped - do nothing (standard UO behavior)
            return;
        }

        // Attempt Sphere-style equip
        EquipmentHelper.TryEquipItem(from, this);
    }

    /// <summary>
    /// Sphere51a override for GetDelay method.
    /// Only active when SphereConfiguration.Enabled is true.
    /// </summary>
    public virtual TimeSpan GetDelay_Sphere51a(Mobile m)
    {
        // Sphere51a integration: Use Sphere51a timing calculations when enabled
        if (Server.Modules.Sphere51a.Configuration.SphereConfiguration.Enabled)
        {
            var timingProvider = Server.Modules.Sphere51a.SphereInitializer.ActiveTimingProvider;
            if (timingProvider != null)
            {
                int delayMs = timingProvider.GetAttackIntervalMs(m, this);
                return TimeSpan.FromMilliseconds(delayMs);
            }
        }

        // Fallback to base implementation if Sphere51a is disabled
        return GetDelay_Base(m);
    }

    /// <summary>
    /// Sphere51a override for OnSwing method.
    /// Intercepts weapon swings to prevent double-hits when independent timers are active.
    /// </summary>

    /// <summary>
    /// Sphere51a override for OnSwing method.
    /// Only active when SphereConfiguration.Enabled is true.
    /// </summary>
    public virtual TimeSpan OnSwing_Sphere51a(Mobile attacker, Mobile defender, double damageBonus = 1.0)
    {
        // Raise weapon swing event for modular Sphere system
        var eventArgs = Server.Modules.Sphere51a.Events.SphereEvents.RaiseWeaponSwing(attacker, defender, this);

        // If Sphere cancelled the event (handled the attack), return the delay without executing ModernUO logic
        if (eventArgs.Cancelled)
        {
            return eventArgs.Delay != TimeSpan.Zero ? eventArgs.Delay : GetDelay_Sphere51a(attacker);
        }

        // Sphere didn't handle it, continue with normal ModernUO attack logic
        var canSwing = true;

        if (Core.AOS)
        {
            canSwing = !attacker.Paralyzed && !attacker.Frozen;

            if (canSwing)
            {
                canSwing = attacker.Spell is not Spell sp || !sp.IsCasting || !sp.BlocksMovement;
            }

            if (canSwing)
            {
                canSwing = attacker is not PlayerMobile p || p.PeacedUntil <= Core.Now;
            }
        }

        if ((attacker as PlayerMobile)?.DuelContext?.CheckItemEquip(attacker, this) == false)
        {
            canSwing = false;
        }

        if (canSwing && attacker.HarmfulCheck(defender))
        {
            attacker.DisruptiveAction();

            attacker.NetState?.SendSwing(attacker.Serial, defender.Serial);

            if (attacker is BaseCreature bc)
            {
                if (bc.TriggerAbility(MonsterAbilityTrigger.CombatAction, defender))
                {
                    return GetDelay_Sphere51a(attacker);
                }

                // Only change direction if they are not a player.
                attacker.Direction = attacker.GetDirectionTo(defender);
                var ab = bc.GetWeaponAbility();

                if (ab != null)
                {
                    if (bc.WeaponAbilityChance > Utility.RandomDouble())
                    {
                        WeaponAbility.SetCurrentAbility(bc, ab);
                    }
                    else
                    {
                        WeaponAbility.ClearCurrentAbility(bc);
                    }
                }
            }

            if (CheckHit(attacker, defender))
            {
                OnHit(attacker, defender, damageBonus);
            }
            else
            {
                OnMiss(attacker, defender);
            }
        }

        var delay = GetDelay_Sphere51a(attacker);

        // Raise weapon swing complete event for modular Sphere system
        Server.Modules.Sphere51a.Events.SphereEvents.RaiseWeaponSwingComplete(attacker, defender, this, delay);

        return delay;
    }

    /// <summary>
    /// Base implementation of GetDelay (extracted from original BaseWeapon.cs).
    /// This is the fallback when Sphere51a is disabled.
    /// </summary>
    private TimeSpan GetDelay_Base(Mobile m)
    {
        double speed = Speed;

        if (speed == 0)
        {
            return TimeSpan.FromHours(1.0);
        }

        double delayInSeconds;

        if (Core.SE)
        {
            /*
             * This is likely true for Core.AOS as well... both guides report the same
             * formula, and both are wrong.
             * The old formula left in for AOS for legacy & because we aren't quite 100%
             * Sure that AOS has THIS formula
             */
            var bonus = AosAttributes.GetValue(m, AosAttribute.WeaponSpeed);

            bonus += DivineFurySpell.GetWeaponSpeed(m);

            // Bonus granted by successful use of Honorable Execution.
            bonus += HonorableExecution.GetSwingBonus(m);

            if (DualWield.Registry.TryGetValue(m, out var duelWield))
            {
                bonus += duelWield.BonusSwingSpeed;
            }

            var context = TransformationSpellHelper.GetContext(m);

            if (context?.Spell is ReaperFormSpell spell)
            {
                bonus += spell.SwingSpeedBonus;
            }

            var discordanceEffect = 0;

            // Discordance gives a malus of -0/-28% to swing speed.
            if (Discordance.GetEffect(m, ref discordanceEffect))
            {
                bonus -= discordanceEffect;
            }

            if (EssenceOfWindSpell.IsDebuffed(m))
            {
                bonus -= EssenceOfWindSpell.GetSSIMalus(m);
            }

            if (bonus > 60)
            {
                bonus = 60;
            }

            double ticks;

            if (Core.ML)
            {
                var stamTicks = m.Stam / 30;

                ticks = speed * 4;
                ticks = Math.Floor((ticks - stamTicks) * (100.0 / (100 + bonus)));
            }
            else
            {
                speed = Math.Floor(speed * (bonus + 100.0) / 100.0);

                if (speed <= 0)
                {
                    speed = 1;
                }

                ticks = Math.Floor(80000.0 / ((m.Stam + 100) * speed) - 2);
            }

            // Swing speed currently capped at one swing every 1.25 seconds (5 ticks).
            if (ticks < 5)
            {
                ticks = 5;
            }

            delayInSeconds = ticks * 0.25;
        }
        else if (Core.AOS)
        {
            var v = (m.Stam + 100) * (int)speed;

            var bonus = AosAttributes.GetValue(m, AosAttribute.WeaponSpeed);

            if (DivineFurySpell.UnderEffect(m))
            {
                bonus += 10;
            }

            var discordanceEffect = 0;

            // Discordance gives a malus of -0/-28% to swing speed.
            if (Discordance.GetEffect(m, ref discordanceEffect))
            {
                bonus -= discordanceEffect;
            }

            v += AOS.Scale(v, bonus);

            if (v <= 0)
            {
                v = 1;
            }

            delayInSeconds = Math.Floor(40000.0 / v) * 0.5;

            // Maximum swing rate capped at one swing per second
            // OSI dev said that it has and is supposed to be 1.25
            if (delayInSeconds < 1.25)
            {
                delayInSeconds = 1.25;
            }
        }
        else
        {
            var v = (m.Stam + 100) * (int)speed;

            if (v <= 0)
            {
                v = 1;
            }

            delayInSeconds = 15000.0 / v;
        }

        return TimeSpan.FromSeconds(delayInSeconds);
    }

    /// <summary>
    /// Base implementation of OnSwing (extracted from original BaseWeapon.cs).
    /// This is the fallback when Sphere51a is disabled.
    /// </summary>
    private TimeSpan OnSwing_Base(Mobile attacker, Mobile defender, double damageBonus = 1.0)
    {
        var canSwing = true;

        if (Core.AOS)
        {
            canSwing = !attacker.Paralyzed && !attacker.Frozen;

            if (canSwing)
            {
                canSwing = attacker.Spell is not Spell sp || !sp.IsCasting || !sp.BlocksMovement;
            }

            if (canSwing)
            {
                canSwing = attacker is not PlayerMobile p || p.PeacedUntil <= Core.Now;
            }
        }

        if ((attacker as PlayerMobile)?.DuelContext?.CheckItemEquip(attacker, this) == false)
        {
            canSwing = false;
        }

        if (canSwing && attacker.HarmfulCheck(defender))
        {
            attacker.DisruptiveAction();

            attacker.NetState?.SendSwing(attacker.Serial, defender.Serial);

            if (attacker is BaseCreature bc)
            {
                if (bc.TriggerAbility(MonsterAbilityTrigger.CombatAction, defender))
                {
                    return GetDelay_Base(attacker);
                }

                // Only change direction if they are not a player.
                attacker.Direction = attacker.GetDirectionTo(defender);
                var ab = bc.GetWeaponAbility();

                if (ab != null)
                {
                    if (bc.WeaponAbilityChance > Utility.RandomDouble())
                    {
                        WeaponAbility.SetCurrentAbility(bc, ab);
                    }
                    else
                    {
                        WeaponAbility.ClearCurrentAbility(bc);
                    }
                }
            }

            if (CheckHit(attacker, defender))
            {
                OnHit(attacker, defender, damageBonus);
            }
            else
            {
                OnMiss(attacker, defender);
            }
        }

        return GetDelay_Base(attacker);
    }
}
