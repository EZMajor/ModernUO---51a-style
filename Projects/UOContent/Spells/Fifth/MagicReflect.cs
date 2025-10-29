using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Engines.BuffIcons;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Fifth
{
    //Sphere-style edit: Converted to targeted spell to match Sphere 0.51a behavior
    public class MagicReflectSpell : MagerySpell, ITargetingSpell<Mobile>
    {
        private static readonly SpellInfo _info = new(
            "Magic Reflection",
            "In Jux Sanct",
            242,
            9012,
            Reagent.Garlic,
            Reagent.MandrakeRoot,
            Reagent.SpidersSilk
        );

        private static readonly Dictionary<Mobile, ResistanceMod[]> _table = new();

        public MagicReflectSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Fifth;

        public override bool CheckCast()
        {
            //Sphere-style edit: Removed pre-cast checks, validation happens on target
            return true;
        }

        //Sphere-style edit: Implement ITargetingSpell<Mobile> interface
        public void Target(Mobile target)
        {
            //Sphere-style edit: Validate target is the caster or a party member
            if (!Caster.CanBeBeneficial(target, false))
            {
                Caster.SendLocalizedMessage(1001018); // You cannot perform negative acts on your target.
                return;
            }

            if (Core.AOS)
            {
                /* The magic reflection spell decreases the caster's physical resistance, while increasing the caster's elemental resistances.
                 * Physical decrease = 25 - (Inscription/20).
                 * Elemental resistance = +10 (-20 physical, +10 elemental at GM Inscription)
                 * The magic reflection spell has an indefinite duration, becoming active when cast, and deactivated when re-cast.
                 * Reactive Armor, Protection, and Magic Reflection will stay on even after logging out,
                 * even after dying, until you turn them off by casting them again.
                 */

                if (CheckBSequence(target))
                {
                    if (_table.Remove(target, out var mods))
                    {
                        //Sphere-style edit: Apply effects to target, not caster
                        target.PlaySound(0x1ED);
                        target.FixedParticles(0x375A, 10, 15, 5037, EffectLayer.Waist);

                        for (var i = 0; i < mods.Length; ++i)
                        {
                            target.RemoveResistanceMod(mods[i]);
                        }

                        (target as PlayerMobile)?.RemoveBuff(BuffIcon.MagicReflection);
                    }
                    else
                    {
                        //Sphere-style edit: Apply effects to target, not caster
                        target.PlaySound(0x1E9);
                        target.FixedParticles(0x375A, 10, 15, 5037, EffectLayer.Waist);

                        var physiMod = -25 + (int)(Caster.Skills.Inscribe.Value / 20);
                        const int otherMod = 10;

                        mods =
                        [
                            new ResistanceMod(ResistanceType.Physical, "PhysicalResistMagicResist", physiMod),
                            new ResistanceMod(ResistanceType.Fire, "FireResistMagicResist", otherMod),
                            new ResistanceMod(ResistanceType.Cold, "ColdResistMagicResist", otherMod),
                            new ResistanceMod(ResistanceType.Poison, "PoisonResistMagicResist", otherMod),
                            new ResistanceMod(ResistanceType.Energy, "EnergyResistMagicResist", otherMod)
                        ];

                        _table[target] = mods;

                        for (var i = 0; i < mods.Length; ++i)
                        {
                            target.AddResistanceMod(mods[i]);
                        }

                        var buffFormat = $"{physiMod}\t+{otherMod}\t+{otherMod}\t+{otherMod}\t+{otherMod}";

                        (target as PlayerMobile)?.AddBuff(
                            new BuffInfo(BuffIcon.MagicReflection, 1075817, args: buffFormat, retainThroughDeath: true)
                        );
                    }
                }
            }
            else
            {
                if (target.MagicDamageAbsorb > 0)
                {
                    Caster.SendLocalizedMessage(1005559); // This spell is already in effect.
                }
                else if (!target.CanBeginAction<DefensiveSpell>())
                {
                    Caster.SendLocalizedMessage(1005385); // The spell will not adhere to you at this time.
                }
                else if (CheckBSequence(target))
                {
                    if (target.BeginAction<DefensiveSpell>())
                    {
                        var value = (int)(Caster.Skills.Magery.Value + Caster.Skills.Inscribe.Value);
                        value = (int)(8 + value / 200.0 * 7.0); // absorb from 8 to 15 "circles"

                        target.MagicDamageAbsorb = value;

                        target.FixedParticles(0x375A, 10, 15, 5037, EffectLayer.Waist);
                        target.PlaySound(0x1E9);
                    }
                    else
                    {
                        Caster.SendLocalizedMessage(1005385); // The spell will not adhere to you at this time.
                    }
                }
            }
        }

        //Sphere-style edit: Add OnCast to create target cursor
        public override void OnCast()
        {
            Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Beneficial);
        }

        [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
        public static void EndReflect(Mobile m)
        {
            if (!_table.Remove(m, out var mods))
            {
                return;
            }

            for (var i = 0; i < mods?.Length; ++i)
            {
                m.RemoveResistanceMod(mods[i]);
            }

            (m as PlayerMobile)?.RemoveBuff(BuffIcon.MagicReflection);
        }
    }
}
