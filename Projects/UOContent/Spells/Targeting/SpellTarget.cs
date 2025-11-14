using System;
using Server.Targeting;
using Server.Items;
using Server.Modules.Sphere51a.Configuration;
using Server.Modules.Sphere51a.Events;
using Server.Modules.Sphere51a.Spells;

namespace Server.Spells;

public class SpellTarget<T> : Target, ISpellTarget<T> where T : class, IPoint3D
{
    private static readonly bool _canTargetStatic = typeof(T).IsAssignableFrom(typeof(StaticTarget));
    private static readonly bool _canTargetMobile = typeof(T).IsAssignableFrom(typeof(Mobile));
    private static readonly bool _canTargetItem = typeof(T).IsAssignableFrom(typeof(Item));

    private readonly bool _retryOnLos;
    protected readonly ITargetingSpell<T> _spell;

    public SpellTarget(
        ITargetingSpell<T> spell,
        TargetFlags flags,
        bool retryOnLos = false
    ) : this(spell, false, flags, retryOnLos)
    {
    }

    public SpellTarget(
        ITargetingSpell<T> spell,
        bool allowGround = false,
        TargetFlags flags = TargetFlags.None,
        bool retryOnLos = false
    ) : base(spell.TargetRange, allowGround, flags)
    {
        _spell = spell;
        _retryOnLos = retryOnLos;
    }

    public ITargetingSpell<T> Spell => _spell;

    protected override bool CanTarget(Mobile from, StaticTarget staticTarget, ref Point3D loc, ref Map map)
        => base.CanTarget(from, staticTarget, ref loc, ref map) && _canTargetStatic;

    protected override bool CanTarget(Mobile from, Mobile mobile, ref Point3D loc, ref Map map) =>
        base.CanTarget(from, mobile, ref loc, ref map) && _canTargetMobile;

    protected override bool CanTarget(Mobile from, Item item, ref Point3D loc, ref Map map) =>
        base.CanTarget(from, item, ref loc, ref map) && _canTargetItem;

    protected override void OnCantSeeTarget(Mobile from, object o)
    {
        from.SendLocalizedMessage(500237); // Target can not be seen.
    }

    protected override void OnTarget(Mobile from, object o)
    {
        if (SphereConfiguration.Enabled && _spell is Spell spell)
        {
            // SPHERE51A: All casting logic happens AFTER target selection

            // STEP 1: Set spell state (was skipped in Cast() for Sphere51a)
            spell.State = SpellState.Casting;
            from.Spell = spell;

            // STEP 2: Speak mantra and trigger effects (now that target is selected)
            spell.SayMantra();

            // STEP 3: Fire OnSpellCastBegin event
            SphereEvents.RaiseSpellCastBegin(from, spell);

            // STEP 4: Consume reagents NOW (before delay)
            if (!spell.ConsumeReagents())
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x22, 502630); // More reagents are needed
                spell.DoFizzle();
                return;
            }

            // STEP 4b: Consume mana NOW (before delay)
            var requiredMana = spell.ScaleMana(spell.GetMana());
            if (from.Mana < requiredMana)
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x22, 502625); // Insufficient mana
                spell.DoFizzle();
                return;
            }
            from.Mana -= requiredMana;

            // STEP 5: Get cast delay parameters
            var skillValue = from.Skills[spell.CastSkill].Value;
            var fromScroll = spell.Scroll != null && !(spell.Scroll is BaseWand);
            var delayMs = SpellTimingProvider.GetCastDelay(spell, skillValue, fromScroll);
            var castDelay = TimeSpan.FromMilliseconds(delayMs);

            // STEP 6: Start animations
            spell.StartCastAnimation(castDelay);

            // STEP 7: Set paralyze flag if movement should be blocked
            from.Delta(MobileDelta.Flags);

            // STEP 8: Start cast delay timer
            if (delayMs > 0)
            {
                Timer.DelayCall(castDelay, () =>
                {
                    // STEP 9: After delay completes, check LOS and mana, then apply effect

                    // Check LOS one more time (target may have moved during delay)
                    if (o is Mobile m && !from.InLOS(m))
                    {
                        from.SendLocalizedMessage(500237); // Target can not be seen.
                        spell.Disturb(DisturbType.Hurt, false, true);
                        return;
                    }

                    // Transition to Sequencing state (required for CheckSequence() validation)
                    spell.State = SpellState.Sequencing;

                    // Fire OnSpellCast event
                    SphereEvents.RaiseSpellCast(from, spell);

                    // Apply spell effect
                    _spell.Target(o as T);
                });
            }
            else
            {
                // Instant cast - check LOS and mana, then apply effect immediately

                // Check LOS (target should be in sight for instant cast)
                if (!from.InLOS(o as IPoint3D))
                {
                    from.SendLocalizedMessage(500237); // Target can not be seen.
                    spell.DoFizzle();
                    return;
                }

                // Transition to Sequencing state (required for CheckSequence() validation)
                spell.State = SpellState.Sequencing;

                SphereEvents.RaiseSpellCast(from, spell);
                _spell.Target(o as T);
            }
        }
        else
        {
            // ModernUO default: immediate effect
            _spell.Target(o as T);
        }
    }

    protected override void OnTargetOutOfLOS(Mobile from, object o)
    {
        if (!_retryOnLos)
        {
            return;
        }

        from.SendLocalizedMessage(501943); // Target cannot be seen. Try again.
        from.Target = new SpellTarget<T>(_spell, AllowGround, Flags, true);
        from.Target.BeginTimeout(from, TimeoutTime - Core.TickCount);
    }

    protected override void OnTargetFinish(Mobile from) => _spell?.FinishSequence();
}
