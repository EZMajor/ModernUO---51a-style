using System;
using System.Collections.Generic;
using Server.Engines.ConPVP;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Spells.Bushido;
using Server.Spells.Necromancy;
using Server.Spells.Ninjitsu;
using Server.Spells.Second;
using Server.Spells.Spellweaving;
using Server.Systems.Combat.SphereStyle;
using Server.Targeting;

namespace Server.Spells
{
    public abstract class Spell : ISpell
    {
        private static readonly TimeSpan NextSpellDelay = TimeSpan.FromSeconds(0.75);
        private static readonly TimeSpan AnimateDelay = TimeSpan.FromSeconds(1.5);
        // In reality, it's ANY delayed Damage spell Post-AoS that can't stack, but, only
        // Expo & Magic Arrow have enough delay and a short enough cast time to bring up
        // the possibility of stacking 'em.  Note that a MA & an Explosion will stack, but
        // of course, two MA's won't.

        private static readonly Dictionary<Type, DelayedDamageContextWrapper> _contextTable = new();

        private AnimTimer _animTimer;
        private CastTimer _castTimer;

        //Sphere-style edit: Store original cast delay for post-target casting
        private TimeSpan _spherePostTargetDelay;

        //Sphere-style edit: Track the spell this one replaced (for cancellation on target selection)
        private Spell _replacedSpell;

        //Sphere-style edit: Track if this spell has selected a target (vs just showing cursor)
        // This is used to determine if fizzle should occur when replaced by another spell
        private bool _hasSelectedTarget;

        public Spell(Mobile caster, Item scroll, SpellInfo info)
        {
            Caster = caster;
            Scroll = scroll;
            Info = info;
        }

        public SpellState State { get; set; }

        public Mobile Caster { get; }

        public SpellInfo Info { get; }

        public string Name => Info.Name;
        public string Mantra => Info.Mantra;
        public Type[] Reagents => Info.Reagents;
        public Item Scroll { get; }

        public long StartCastTime { get; private set; }

        //Sphere-style edit: Expose post-target cast delay for SpellTarget
        public TimeSpan SpherePostTargetDelay => _spherePostTargetDelay;

        //Sphere-style edit: Expose replaced spell for cancellation on target selection
        public Spell ReplacedSpell
        {
            get => _replacedSpell;
            set => _replacedSpell = value;
        }

        //Sphere-style edit: Expose if this spell has selected a target
        // True = target selected (will fizzle if interrupted)
        // False = only cursor shown (silent cancel if interrupted)
        public bool HasSelectedTarget
        {
            get => _hasSelectedTarget;
            set => _hasSelectedTarget = value;
        }

        public virtual SkillName CastSkill => SkillName.Magery;
        public virtual SkillName DamageSkill => SkillName.EvalInt;

        public virtual bool RevealOnCast => true;
        public virtual bool ClearHandsOnCast => true;
        public virtual bool ShowHandMovement => true;

        public virtual bool DelayedDamage => false;

        public static readonly Type[] AOSNoDelayedDamageStackingSelf = Core.AOS ? Array.Empty<Type>() : null;

        // Null means stacking is allowed while empty indicates no stacking with self
        // More than zero means no stacking with self and other spells
        public virtual Type[] DelayedDamageSpellFamilyStacking => null;

        public virtual bool BlockedByHorrificBeast => true;
        public virtual bool BlockedByAnimalForm => true;
        //Sphere-style edit: Allow movement during casting if configured
        public virtual bool BlocksMovement =>
            Systems.Combat.SphereStyle.SphereSpellHelper.CheckBlocksMovement(Caster, this, IsCasting);

        public virtual bool CheckNextSpellTime => Scroll is not BaseWand;

        public virtual int CastRecoveryBase => 6;
        public virtual int CastRecoveryFastScalar => 1;
        public virtual int CastRecoveryPerSecond => 4;
        public virtual int CastRecoveryMinimum => 0;

        public abstract TimeSpan CastDelayBase { get; }

        public virtual double CastDelayFastScalar => 1;
        public virtual double CastDelaySecondsPerTick => 0.25;
        public virtual TimeSpan CastDelayMinimum => TimeSpan.FromSeconds(0.25);

        public virtual bool IsCasting => State == SpellState.Casting;

        public virtual void OnCasterHurt()
        {
            // Confirm: Monsters and pets cannot be disturbed.
            if (Caster.Player && IsCasting)
            {
                var hasProtection = ProtectionSpell.Registry.TryGetValue(Caster, out var d);
                if (!hasProtection || d < 1000 && d < Utility.Random(1000))
                {
                    Disturb(DisturbType.Hurt, false, true);
                }
            }
        }

        public virtual void OnCasterKilled()
        {
            Disturb(DisturbType.Kill);
        }

        public virtual void OnConnectionChanged()
        {
            FinishSequence();
        }

        public virtual bool OnCasterMoving(Direction d)
        {
            if (IsCasting && BlocksMovement)
            {
                Caster.SendLocalizedMessage(500111); // You are frozen and can not move.
                return false;
            }

            return true;
        }

        public virtual bool OnCasterEquipping(Item item)
        {
            if (IsCasting)
            {
                Disturb(DisturbType.EquipRequest);
            }

            return true;
        }

        public virtual bool OnCasterUsingObject(IEntity entity)
        {
            if (State == SpellState.Sequencing)
            {
                Disturb(DisturbType.UseRequest);
            }

            return true;
        }

        public virtual bool OnCastInTown(Region r) => Info.AllowTown;

        public virtual void FinishSequence()
        {
            State = SpellState.None;

            if (Caster.Spell == this)
            {
                Caster.Spell = null;
            }

            //Sphere-style edit: Clear casting flags when spell finishes
            if (Systems.Combat.SphereStyle.SphereConfig.IsEnabled())
            {
                Caster.SphereEndSpellCast(true);
            }

            Caster.Delta(MobileDelta.Flags); // Remove paralyze
        }

        public void StartDelayedDamageContext(Mobile m, Timer t)
        {
            var damageStacking = DelayedDamageSpellFamilyStacking;
            if (damageStacking == null)
            {
                return; // Sanity
            }

            var type = GetType();

            if (!_contextTable.TryGetValue(type, out var context))
            {
                _contextTable[type] = context = new DelayedDamageContextWrapper();

                for (int i = 0; i < damageStacking.Length; i++)
                {
                    _contextTable.Add(damageStacking[i], context);
                }
            }

            context.Add(m, t);
        }

        public bool HasDelayedDamageContext(Mobile m) =>
            DelayedDamageSpellFamilyStacking != null &&
            _contextTable.TryGetValue(GetType(), out var context) && context.Contains(m);

        public void RemoveDelayedDamageContext(Mobile m)
        {
            if (m == null || DelayedDamageSpellFamilyStacking == null)
            {
                return; // Sanity
            }

            if (_contextTable.TryGetValue(GetType(), out var contexts))
            {
                contexts.Remove(m);
            }
        }

        public void HarmfulSpell(Mobile m)
        {
            (m as BaseCreature)?.OnHarmfulSpell(Caster);
        }

        public int GetNewAosDamage(int bonus, int dice, int sides, Mobile singleTarget) =>
            GetNewAosDamage(bonus, dice, sides, true, singleTarget);

        public virtual int GetNewAosDamage(int bonus, int dice, int sides, bool sdi = true, Mobile singleTarget = null)
        {
            if (singleTarget != null)
            {
                return GetNewAosDamage(
                    bonus,
                    dice,
                    sides,
                    Caster.Player && singleTarget.Player,
                    sdi,
                    GetDamageScalar(singleTarget)
                );
            }

            return GetNewAosDamage(bonus, dice, sides, sdi, false);
        }

        public virtual int GetNewAosDamage(int bonus, int dice, int sides, bool playerVsPlayer, bool sdi, double scalar = 1.0)
        {
            var damage = Utility.Dice(dice, sides, bonus) * 100;

            var inscribeSkill = GetInscribeFixed(Caster);
            var inscribeBonus = (inscribeSkill + 1000 * (inscribeSkill / 1000)) / 200;
            var damageBonus = inscribeBonus;

            var intBonus = Caster.Int / 10;
            damageBonus += intBonus;

            if (sdi)
            {
                var sdiBonus = AosAttributes.GetValue(Caster, AosAttribute.SpellDamage);
                // PvP spell damage increase cap of 15% from an item's magic property
                if (playerVsPlayer && sdiBonus > 15)
                {
                    sdiBonus = 15;
                }

                damageBonus += sdiBonus;
            }

            var context = TransformationSpellHelper.GetContext(Caster);

            if (context?.Spell is ReaperFormSpell spell)
            {
                damageBonus += spell.SpellDamageBonus;
            }

            damage = AOS.Scale(damage, 100 + damageBonus);

            var evalSkill = GetDamageFixed(Caster);
            var evalScale = 30 + 9 * evalSkill / 100;

            damage = AOS.Scale(damage, evalScale);

            damage = AOS.Scale(damage, (int)(scalar * 100));

            return damage / 100;
        }

        public virtual bool ConsumeReagents() =>
            Scroll != null || !Caster.Player ||
            AosAttributes.GetValue(Caster, AosAttribute.LowerRegCost) > Utility.Random(100) ||
            DuelContext.IsFreeConsume(Caster) || Caster.Backpack?.ConsumeTotal(Info.Reagents, Info.Amounts) == -1;

        public virtual double GetInscribeSkill(Mobile m) => m.Skills.Inscribe.Value;

        public virtual int GetInscribeFixed(Mobile m) => m.Skills.Inscribe.Fixed;

        public virtual int GetDamageFixed(Mobile m) => m.Skills[DamageSkill].Fixed;

        public virtual double GetDamageSkill(Mobile m) => m.Skills[DamageSkill].Value;

        public virtual double GetResistSkill(Mobile m) => m.Skills.MagicResist.Value;

        public virtual double GetDamageScalar(Mobile target)
        {
            var scalar = 1.0;

            if (!Core.AOS) // EvalInt stuff for AoS is handled elsewhere
            {
                var casterEI = Caster.Skills[DamageSkill].Value;
                var targetRS = target.Skills.MagicResist.Value;

                /*
                if (Core.AOS)
                  targetRS = 0;
                */

                // m_Caster.CheckSkill( DamageSkill, 0.0, 120.0 );

                if (casterEI > targetRS)
                {
                    scalar = 1.0 + (casterEI - targetRS) / 500.0;
                }
                else
                {
                    scalar = 1.0 + (casterEI - targetRS) / 200.0;
                }

                // magery damage bonus, -25% at 0 skill, +0% at 100 skill, +5% at 120 skill
                scalar += (Caster.Skills[CastSkill].Value - 100.0) / 400.0;

                if (!target.Player && !target.Body.IsHuman /*&& !Core.AOS*/)
                {
                    scalar *= 2.0; // Double magery damage to monsters/animals if not AOS
                }
            }

            (target as BaseCreature)?.AlterDamageScalarFrom(Caster, ref scalar);

            (Caster as BaseCreature)?.AlterDamageScalarTo(target, ref scalar);

            if (Core.SE)
            {
                scalar *= GetSlayerDamageScalar(target);
            }

            target.Region.SpellDamageScalar(Caster, target, ref scalar);

            if (Evasion.CheckSpellEvasion(target)) // Only single target spells an be evaded
            {
                scalar = 0;
            }

            return scalar;
        }

        public virtual double GetSlayerDamageScalar(Mobile defender)
        {
            var atkBook = Spellbook.FindEquippedSpellbook(Caster);

            var scalar = 1.0;
            if (atkBook != null)
            {
                var atkSlayer = SlayerGroup.GetEntryByName(atkBook.Slayer);
                var atkSlayer2 = SlayerGroup.GetEntryByName(atkBook.Slayer2);

                if (atkSlayer?.Slays(defender) == true || atkSlayer2?.Slays(defender) == true)
                {
                    defender.FixedEffect(0x37B9, 10, 5); // TODO: Confirm this displays on OSIs
                    scalar = 2.0;
                }

                var context = TransformationSpellHelper.GetContext(defender);

                if ((atkBook.Slayer == SlayerName.Silver || atkBook.Slayer2 == SlayerName.Silver) && context != null &&
                    context.Type != typeof(HorrificBeastSpell))
                {
                    scalar += .25; // Every necromancer transformation other than horrific beast take an additional 25% damage
                }

                if (scalar != 1.0)
                {
                    return scalar;
                }
            }

            var defISlayer = Spellbook.FindEquippedSpellbook(defender) ?? defender.Weapon as ISlayer;

            if (defISlayer != null)
            {
                var defSlayer = SlayerGroup.GetEntryByName(defISlayer.Slayer);
                var defSlayer2 = SlayerGroup.GetEntryByName(defISlayer.Slayer2);

                if (defSlayer?.Group.OppositionSuperSlays(Caster) == true ||
                    defSlayer2?.Group.OppositionSuperSlays(Caster) == true)
                {
                    scalar = 2.0;
                }
            }

            return scalar;
        }

        public virtual void DoFizzle()
        {
            Caster.LocalOverheadMessage(MessageType.Regular, 0x3B2, 502632); // The spell fizzles.

            if (Caster.Player)
            {
                if (Core.AOS)
                {
                    Caster.FixedParticles(0x3735, 1, 30, 9503, EffectLayer.Waist);
                }
                else
                {
                    Caster.FixedEffect(0x3735, 6, 30);
                }

                Caster.PlaySound(0x5C);
            }
        }

        public virtual bool CheckDisturb(DisturbType type, bool firstCircle, bool resistable) =>
            !(resistable && Scroll is BaseWand);

        public void Disturb(DisturbType type, bool firstCircle = true, bool resistable = false)
        {
            if (!CheckDisturb(type, firstCircle, resistable))
            {
                return;
            }

            if (State == SpellState.None || !firstCircle && !Core.AOS && (this as MagerySpell)?.Circle == SpellCircle.First)
            {
                return;
            }

            var wasCasting = IsCasting; // Casting state (targeting)
            var wasSequencing = State == SpellState.Sequencing; // Sequencing state (post-target countdown)
            var wasInCastDelay = Systems.Combat.SphereStyle.SphereConfig.IsEnabled() &&
                                Caster.SphereIsInCastDelay(); // Check if in post-target cast delay
            State = SpellState.None;
            Caster.Spell = null;

            //Sphere-style edit: Show fizzle effects and consume resources when interrupted
            // This handles Casting (targeting), Sequencing (countdown), and CastDelay (post-target anim) states
            if ((wasCasting || wasSequencing || wasInCastDelay) && type == DisturbType.NewCast)
            {
                DoFizzle();

                // Consume mana (deduct what would have been used)
                var requiredMana = ScaleMana(GetMana());
                if (Caster.Mana >= requiredMana)
                {
                    Caster.Mana -= requiredMana;
                }

                // Consume reagents from spellbook
                if (Scroll == null)
                {
                    if (Caster.Backpack != null)
                    {
                        ConsumeReagents();
                    }
                }
                // Consume scroll
                else if (Scroll is SpellScroll)
                {
                    Scroll.Consume();
                }
            }

            //Sphere-style edit: Clear IsInCastDelay flag
            if (Systems.Combat.SphereStyle.SphereConfig.IsEnabled())
            {
                Caster.SphereEndSpellCast(false);
            }

            OnDisturb(type, wasCasting);

            if (wasCasting)
            {
                _castTimer?.Stop();
                _animTimer?.Stop();
                Caster.NextSpellTime = Core.TickCount + (int)GetDisturbRecovery().TotalMilliseconds;
            }
            else
            {
                Target.Cancel(Caster);
            }

            if (Core.AOS && Caster.Player && type == DisturbType.Hurt)
            {
                DoHurtFizzle();
            }

            Caster.Delta(MobileDelta.Flags); // Remove paralyze
        }

        public virtual void DoHurtFizzle()
        {
            Caster.FixedEffect(0x3735, 6, 30);
            Caster.PlaySound(0x5C);
        }

        public virtual void OnDisturb(DisturbType type, bool message)
        {
            if (message)
            {
                Caster.SendLocalizedMessage(500641); // Your concentration is disturbed, thus ruining thy spell.
            }
        }

        public virtual bool CheckCast() => true;

        public virtual void SayMantra()
        {
            //Sphere-style edit: Only wands skip mantra, scrolls should show it
            if (Scroll is BaseWand)
            {
                return;
            }

            if (!string.IsNullOrEmpty(Info.Mantra) && Caster.Player)
            {
                Caster.PublicOverheadMessage(MessageType.Spell, Caster.SpeechHue, true, Info.Mantra, false);
            }
        }

        public bool Cast()
        {
            StartCastTime = Core.TickCount;

            if (!Caster.CheckAlive())
            {
                return false;
            }

            var isCasting = Caster.Spell?.IsCasting == true;
            var isWand = Scroll is BaseWand;

            if (isCasting)
            {
                if (isWand)
                {
                    Caster.SendLocalizedMessage(502643); // You can not cast a spell while frozen.
                }
                else
                {
                    Caster.SendLocalizedMessage(502642); // You are already casting a spell.
                }
            }
            else if (BlockedByHorrificBeast &&
                     TransformationSpellHelper.UnderTransformation(Caster, typeof(HorrificBeastSpell)) ||
                     BlockedByAnimalForm && AnimalForm.UnderTransformation(Caster))
            {
                Caster.SendLocalizedMessage(1061091); // You cannot cast that spell in this form.
            }
            else if (!isWand && (Caster.Paralyzed || Caster.Frozen))
            {
                Caster.SendLocalizedMessage(502643); // You can not cast a spell while frozen.
            }
            else if (CheckNextSpellTime && Core.TickCount - Caster.NextSpellTime < 0)
            {
                Caster.SendLocalizedMessage(502644); // You have not yet recovered from casting a spell.
            }
            else if (Caster is PlayerMobile mobile && mobile.PeacedUntil > Core.Now)
            {
                mobile.SendLocalizedMessage(1072060); // You cannot cast a spell while calmed.
            }
            else if ((Caster as PlayerMobile)?.DuelContext?.AllowSpellCast(Caster, this) == false)
            {
            }
            else
            {
                var requiredMana = ScaleMana(GetMana());

                if (Caster.Mana >= requiredMana)
                {
                    //Sphere-style edit: In immediate target mode, allow multiple spells in Casting state
                    // The active spell will be canceled when target is selected for the new one
                    var sphereImmediateTargetMode = Systems.Combat.SphereStyle.SphereConfig.IsEnabled() &&
                                                   Systems.Combat.SphereStyle.SphereConfig.ImmediateSpellTarget;

                    // In immediate target mode, we don't cancel here - we just update Caster.Spell
                    // The old spell's targeting cursor stays active until new spell target is selected
                    if (Caster.Spell != null && !sphereImmediateTargetMode)
                    {
                        // ModernUO default: can't cast if already casting
                        return false;
                    }

                    if (Caster.CheckSpellCast(this) && CheckCast() &&
                        Caster.Region.OnBeginSpellCast(Caster, this))
                    {
                        State = SpellState.Casting;

                        //Sphere-style edit: Store the previous spell and cancel its target cursor
                        if (sphereImmediateTargetMode && Caster.Spell is Spell previousSpell && previousSpell != this)
                        {
                            _replacedSpell = previousSpell;

                            // Cancel the previous spell's target cursor (closes the UI targeting)
                            // But don't disturb the spell yet - that happens on target selection
                            if (Caster.Target != null)
                            {
                                Caster.Target.Cancel(Caster, TargetCancelType.Overridden);
                            }
                        }

                        Caster.Spell = this;

                        if (!isWand && RevealOnCast)
                        {
                            Caster.RevealingAction();
                        }

                        //Sphere-style edit: Check if immediate targeting is enabled
                        var sphereImmediateTarget = Systems.Combat.SphereStyle.SphereConfig.IsEnabled() &&
                                                    Systems.Combat.SphereStyle.SphereConfig.ImmediateSpellTarget;

                        // Calculate the cast delay first
                        var originalCastDelay = GetCastDelay();

                        //Sphere-style edit: Store original delay for post-target casting
                        if (sphereImmediateTarget)
                        {
                            _spherePostTargetDelay = originalCastDelay;
                        }

                        if (!sphereImmediateTarget)
                        {
                            // ModernUO default: Say mantra and start cast animation/delay
                            SayMantra();
                        }

                        var castDelay = sphereImmediateTarget ? TimeSpan.Zero : originalCastDelay;

                        if (!sphereImmediateTarget && ShowHandMovement && (Caster.Body.IsHuman || Caster.Player && Caster.Body.IsMonster))
                        {
                            var count = (int)Math.Ceiling(castDelay.TotalSeconds / AnimateDelay.TotalSeconds);

                            if (count != 0)
                            {
                                _animTimer = new AnimTimer(this, count);
                                _animTimer.Start();
                            }

                            if (Info.LeftHandEffect > 0)
                            {
                                Caster.FixedParticles(0, 10, 5, Info.LeftHandEffect, EffectLayer.LeftHand);
                            }

                            if (Info.RightHandEffect > 0)
                            {
                                Caster.FixedParticles(0, 10, 5, Info.RightHandEffect, EffectLayer.RightHand);
                            }
                        }

                        //Sphere-style edit: Don't clear hands during cast initiation in Sphere mode
                        //Hands are cleared after target selection in CheckSequence() instead
                        if (ClearHandsOnCast && !sphereImmediateTarget)
                        {
                            Caster.ClearHands();
                        }

                        if (Core.ML)
                        {
                            WeaponAbility.ClearCurrentAbility(Caster);
                        }

                        //Sphere-style edit: Don't paralyze if movement allowed during cast
                        if (!sphereImmediateTarget ||
                            !Systems.Combat.SphereStyle.SphereConfig.AllowMovementDuringCast)
                        {
                            Caster.Delta(MobileDelta.Flags); // Start paralyze
                        }

                        _castTimer = new CastTimer(this, castDelay);
                        // m_CastTimer.Start();

                        OnBeginCast();

                        if (castDelay > TimeSpan.Zero)
                        {
                            _castTimer.Start();
                        }
                        else
                        {
                            _castTimer.Tick();
                        }

                        return true;
                    }
                }
                else if (Caster.NetState?.IsKRClient != true && Caster.NetState?.Version >= ClientVersion.Version70654)
                {
                    // Insufficient mana. You must have at least ~1_MANA_REQUIREMENT~ Mana to use this spell.
                    Caster.LocalOverheadMessage(MessageType.Regular, 0x22, 502625, requiredMana.ToString());
                }
                else
                {
                    Caster.LocalOverheadMessage(MessageType.Regular, 0x22, 502625); // Insufficient mana
                }
            }

            return false;
        }

        public abstract void OnCast();

        public virtual void OnBeginCast()
        {
        }

        public virtual void GetCastSkills(out double min, out double max)
        {
            min = max = 0; // Intended but not required for overriding.
        }

        public virtual bool CheckFizzle()
        {
            if (Scroll is BaseWand)
            {
                return true;
            }

            GetCastSkills(out var minSkill, out var maxSkill);

            if (DamageSkill != CastSkill)
            {
                Caster.CheckSkill(DamageSkill, 0.0, Caster.Skills[DamageSkill].Cap);
            }

            return Caster.CheckSkill(CastSkill, minSkill, maxSkill);
        }

        public abstract int GetMana();

        public virtual int ScaleMana(int mana)
        {
            var scalar = 1.0;

            if (!MindRotSpell.GetMindRotScalar(Caster, ref scalar))
            {
                scalar = 1.0;
            }

            // Lower Mana Cost = 40%
            var lmc = AosAttributes.GetValue(Caster, AosAttribute.LowerManaCost);
            if (lmc > 40)
            {
                lmc = 40;
            }

            scalar -= (double)lmc / 100;

            return (int)(mana * scalar);
        }

        public virtual TimeSpan GetDisturbRecovery()
        {
            if (Core.AOS)
            {
                return TimeSpan.Zero;
            }

            var delay = Math.Max(
                1.0 - Math.Sqrt((Core.TickCount - StartCastTime) / 1000.0 / GetCastDelay().TotalSeconds),
                0.2
            );

            return TimeSpan.FromSeconds(delay);
        }

        public virtual TimeSpan GetCastRecovery()
        {
            if (!Core.AOS)
            {
                return NextSpellDelay;
            }

            var fcr = AosAttributes.GetValue(Caster, AosAttribute.CastRecovery) -
                      ThunderstormSpell.GetCastRecoveryMalus(Caster);

            var fcrDelay = -(CastRecoveryFastScalar * fcr);

            var delay = CastRecoveryBase + fcrDelay;

            if (delay < CastRecoveryMinimum)
            {
                delay = CastRecoveryMinimum;
            }

            return TimeSpan.FromSeconds((double)delay / CastRecoveryPerSecond);
        }

        public virtual TimeSpan GetCastDelay()
        {
            if (Scroll is BaseWand)
            {
                return Core.ML ? CastDelayBase : TimeSpan.Zero; // TODO: Should FC apply to wands?
            }

            // Faster casting cap of 2 (if not using the protection spell)
            // Faster casting cap of 0 (if using the protection spell)
            // Paladin spells are subject to a faster casting cap of 4
            // Paladins with magery of 70.0 or above are subject to a faster casting cap of 2
            var fcMax = 4;

            if (CastSkill is SkillName.Magery or SkillName.Necromancy ||
                CastSkill == SkillName.Chivalry && Caster.Skills.Magery.Value >= 70.0)
            {
                fcMax = 2;
            }

            var fc = Math.Min(AosAttributes.GetValue(Caster, AosAttribute.CastSpeed), fcMax);

            if (ProtectionSpell.Registry.ContainsKey(Caster))
            {
                fc -= 2;
            }

            if (EssenceOfWindSpell.IsDebuffed(Caster))
            {
                fc -= EssenceOfWindSpell.GetFCMalus(Caster);
            }

            if (Core.SA)
            {
                // At some point OSI added 0.25s to every spell. This makes the minimum 0.5s
                // Note: This is done after multiplying for summon creature & blade spirits.
                fc--;
            }

            var fcDelay = TimeSpan.FromSeconds(-(CastDelayFastScalar * fc * CastDelaySecondsPerTick));

            return Utility.Max(CastDelayBase + fcDelay, CastDelayMinimum);
        }

        public virtual int ComputeKarmaAward() => 0;

        public virtual bool CheckSequence()
        {
            var mana = ScaleMana(GetMana());

            if (Caster.Deleted || !Caster.Alive || Caster.Spell != this || State != SpellState.Sequencing)
            {
                DoFizzle();
            }
            else if (Scroll != null && Scroll is not Runebook &&
                     (Scroll.Amount <= 0 || Scroll.Deleted || Scroll.RootParent != Caster || Scroll is BaseWand baseWand &&
                         (baseWand.Charges <= 0 || baseWand.Parent != Caster)))
            {
                DoFizzle();
            }
            else if (!ConsumeReagents())
            {
                Caster.LocalOverheadMessage(MessageType.Regular, 0x22, 502630); // More reagents are needed for this spell.
            }
            else if (Caster.Mana < mana)
            {
                Caster.LocalOverheadMessage(MessageType.Regular, 0x22, 502625); // Insufficient mana for this spell.
            }
            else if (Core.AOS && (Caster.Frozen || Caster.Paralyzed))
            {
                Caster.SendLocalizedMessage(502646); // You cannot cast a spell while frozen.
                DoFizzle();
            }
            else if (Caster is PlayerMobile mobile && mobile.PeacedUntil > Core.Now)
            {
                mobile.SendLocalizedMessage(1072060); // You cannot cast a spell while calmed.
                DoFizzle();
            }
            else if (CheckFizzle())
            {
                Caster.Mana -= mana;

                if (Scroll is SpellScroll)
                {
                    Scroll.Consume();
                }
                else if (Scroll is BaseWand wand)
                {
                    wand.ConsumeCharge(Caster);
                    Caster.RevealingAction();
                }

                //Sphere-style edit: Clear hands after target selection if configured
                var sphereClearHands = !Systems.Combat.SphereStyle.SphereConfig.IsEnabled() ||
                                      Systems.Combat.SphereStyle.SphereConfig.ClearHandsOnCast;

                if (Scroll is BaseWand)
                {
                    var m = Scroll.Movable;

                    Scroll.Movable = false;

                    if (ClearHandsOnCast && sphereClearHands)
                    {
                        Caster.ClearHands();
                    }

                    Scroll.Movable = m;
                }
                else if (ClearHandsOnCast && sphereClearHands)
                {
                    Caster.ClearHands();
                }

                var karma = ComputeKarmaAward();

                if (karma != 0)
                {
                    Titles.AwardKarma(Caster, karma, true);
                }

                if (TransformationSpellHelper.UnderTransformation(Caster, typeof(VampiricEmbraceSpell)))
                {
                    var garlic = false;

                    for (var i = 0; !garlic && i < Info.Reagents.Length; ++i)
                    {
                        garlic = Info.Reagents[i] == Reagent.Garlic;
                    }

                    if (garlic)
                    {
                        Caster.SendLocalizedMessage(1061651); // The garlic burns you!
                        AOS.Damage(Caster, Utility.RandomMinMax(17, 23), 100, 0, 0, 0, 0);
                    }
                }

                return true;
            }
            else
            {
                DoFizzle();
            }

            return false;
        }

        public bool CheckBSequence(Mobile target, bool allowDead = false)
        {
            if (!target.Alive && !allowDead)
            {
                Caster.SendLocalizedMessage(501857); // This spell won't work on that!
                return false;
            }

            if ((Caster as PlayerMobile)?.Young == true && (target as PlayerMobile)?.Young == false)
            {
                Caster.SendLocalizedMessage(500278); // As a young player, you may not cast beneficial spells onto older players.
                return false;
            }

            if (Caster.CanBeBeneficial(target, true, allowDead) && CheckSequence())
            {
                Caster.DoBeneficial(target);
                return true;
            }

            return false;
        }

        public bool CheckHSequence(Mobile target)
        {
            if (!target.Alive)
            {
                Caster.SendLocalizedMessage(501857); // This spell won't work on that!
                return false;
            }

            if (Caster.CanBeHarmful(target) && CheckSequence())
            {
                Caster.DoHarmful(target);
                return true;
            }

            return false;
        }

        private class DelayedDamageContextWrapper
        {
            private readonly Dictionary<Mobile, Timer> m_Contexts = new();

            public void Add(Mobile m, Timer t)
            {
                if (m_Contexts.Remove(m, out var oldTimer))
                {
                    oldTimer.Stop();
                }

                m_Contexts.Add(m, t);
            }

            public bool Contains(Mobile m) => m_Contexts.ContainsKey(m);

            public void Remove(Mobile m)
            {
                if (m_Contexts.Remove(m, out var t))
                {
                    t.Stop();
                }
            }
        }

        private class AnimTimer : Timer
        {
            private readonly Spell m_Spell;

            public AnimTimer(Spell spell, int count) : base(TimeSpan.Zero, AnimateDelay, count)
            {
                m_Spell = spell;
            }

            protected override void OnTick()
            {
                var caster = m_Spell.Caster;

                if (m_Spell.State != SpellState.Casting || caster.Spell != m_Spell)
                {
                    Stop();
                    return;
                }

                if (!caster.Mounted && m_Spell.Info.Action >= 0)
                {
                    if (caster.Body.IsHuman)
                    {
                        caster.Animate(m_Spell.Info.Action, 7, 1, true, false, 0);
                    }
                    else if (caster.Player && caster.Body.IsMonster)
                    {
                        caster.Animate(12, 7, 1, true, false, 0);
                    }
                }

                if (!Running)
                {
                    m_Spell._animTimer = null;
                }
            }
        }

        private class CastTimer : Timer
        {
            private readonly Spell m_Spell;

            public CastTimer(Spell spell, TimeSpan castDelay) : base(castDelay)
            {
                m_Spell = spell;
            }

            protected override void OnTick()
            {
                var caster = m_Spell?.Caster;

                if (caster == null)
                {
                    return;
                }

                //Sphere-style edit: In immediate target mode, allow spell to show cursor even if replaced
                // The spell will be fizzled later if the replacing spell's target is selected
                var sphereImmediateTargetMode = Systems.Combat.SphereStyle.SphereConfig.IsEnabled() &&
                                               Systems.Combat.SphereStyle.SphereConfig.ImmediateSpellTarget;

                if (m_Spell.State == SpellState.Casting && (caster.Spell == m_Spell || sphereImmediateTargetMode))
                {
                    m_Spell.State = SpellState.Sequencing;
                    m_Spell._castTimer = null;
                    caster.OnSpellCast(m_Spell);
                    caster.Region?.OnSpellCast(caster, m_Spell);

                    //Sphere-style edit: Use Sphere helper to get cast recovery (may be zero)
                    var recovery = m_Spell.GetCastRecovery();
                    if (Systems.Combat.SphereStyle.SphereConfig.IsEnabled())
                    {
                        recovery = Systems.Combat.SphereStyle.SphereSpellHelper.GetCastRecovery(caster, m_Spell, recovery);
                    }

                    caster.NextSpellTime = Core.TickCount + (int)recovery.TotalMilliseconds;

                    caster.Delta(MobileDelta.Flags); // Update paralyze

                    var originalTarget = caster.Target;

                    m_Spell.OnCast();

                    if (caster.Player && caster.Target != originalTarget)
                    {
                        caster.Target?.BeginTimeout(caster, 30000); // 30 seconds
                    }

                    m_Spell._castTimer = null;
                }
            }

            public void Tick()
            {
                OnTick();
            }
        }
    }
}
