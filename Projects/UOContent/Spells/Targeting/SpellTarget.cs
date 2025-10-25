using System;
using Server.Targeting;

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

    //Sphere-style edit: Handle post-target cast delay
    protected override void OnTarget(Mobile from, object o)
    {
        var target = o as T;

        // Check if Sphere-style immediate targeting with post-target delay is enabled
        if (Systems.Combat.SphereStyle.SphereConfig.IsEnabled() &&
            Systems.Combat.SphereStyle.SphereConfig.ImmediateSpellTarget &&
            Systems.Combat.SphereStyle.SphereConfig.CastDelayAfterTarget &&
            _spell is Spell spell)
        {
            //Sphere-style edit: Set this spell as the active spell BEFORE canceling others
            // This prevents Disturb() from clearing Caster.Spell and breaking the current cast
            from.Spell = spell;

            //Sphere-style edit: Cancel any spell that this one replaced when it was selected
            // This handles the case where player queued Spell A, then Spell B, then selected target with B
            // In that case, we stored A in B.ReplacedSpell, now we cancel A
            if (spell.ReplacedSpell is Spell replacedSpell)
            {
                // Cancel the spell that was replaced (fizzle, consume resources)
                replacedSpell.Disturb(DisturbType.NewCast);
                spell.ReplacedSpell = null; // Clear the reference
            }

            //Sphere-style edit: NOW the spell is actually being cast (target selected)
            // Notify Sphere system of cast begin (cancels bandage/swing)
            Systems.Combat.SphereStyle.SphereSpellHelper.OnCastBegin(from, spell);

            //Sphere-style edit: Clear hands after target selection (always in Sphere mode)
            // In Sphere 0.51a, weapons drop to backpack when spell is cast (target selected)
            if (spell.ClearHandsOnCast)
            {
                from.ClearHands();
            }

            // Get the stored cast delay from the spell
            var castDelay = spell.SpherePostTargetDelay;

            if (castDelay > TimeSpan.Zero)
            {
                //Sphere-style edit: Entering cast delay phase (post-target, pre-effect)
                Systems.Combat.SphereStyle.SphereSpellHelper.OnEnterCastDelay(from);

                // Start cast animations and delay
                spell.SayMantra();

                //Sphere-style edit: Add casting animation during post-target delay
                if (spell.ShowHandMovement && (from.Body.IsHuman || from.Player && from.Body.IsMonster))
                {
                    // Play hand effects
                    if (spell.Info.LeftHandEffect > 0)
                    {
                        from.FixedParticles(0, 10, 5, spell.Info.LeftHandEffect, EffectLayer.LeftHand);
                    }

                    if (spell.Info.RightHandEffect > 0)
                    {
                        from.FixedParticles(0, 10, 5, spell.Info.RightHandEffect, EffectLayer.RightHand);
                    }

                    // Play casting animation
                    if (!from.Mounted && spell.Info.Action >= 0)
                    {
                        if (from.Body.IsHuman)
                        {
                            from.Animate(spell.Info.Action, 7, 1, true, false, 0);
                        }
                        else if (from.Player && from.Body.IsMonster)
                        {
                            from.Animate(12, 7, 1, true, false, 0);
                        }
                    }
                }

                // Schedule the actual spell effect after the cast delay
                Timer.StartTimer(castDelay, () =>
                {
                    //Sphere-style edit: Only check if caster is alive and spell exists
                    if (from.Deleted || !from.Alive || from.Spell != spell)
                    {
                        // Spell was interrupted, finish sequence
                        _spell?.FinishSequence();
                        return;
                    }

                    // Execute spell effect
                    _spell.Target(target);

                    // Finish sequence after spell executes
                    _spell?.FinishSequence();
                });

                // Don't finish sequence yet - wait for timer
                return;
            }
            else
            {
                // Sphere mode with zero cast delay (e.g., some scrolls)
                // Execute immediately but FinishSequence is called by OnTargetFinish
                _spell.Target(target);
                return;
            }
        }

        // Default behavior: immediate effect (FinishSequence called by OnTargetFinish)
        // For non-Sphere or non-immediate-target mode, hands are already cleared in Cast()
        _spell.Target(target);
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

    protected override void OnTargetFinish(Mobile from)
    {
        // Sphere-style edit: Don't finish sequence if using post-target delay
        // The timer callback will handle finishing the sequence
        if (Systems.Combat.SphereStyle.SphereConfig.IsEnabled() &&
            Systems.Combat.SphereStyle.SphereConfig.ImmediateSpellTarget &&
            Systems.Combat.SphereStyle.SphereConfig.CastDelayAfterTarget &&
            _spell is Spell)
        {
            // Don't finish sequence - let timer handle it
            return;
        }

        // Default behavior: finish sequence immediately
        _spell?.FinishSequence();
    }
}
