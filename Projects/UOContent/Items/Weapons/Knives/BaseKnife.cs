using ModernUO.Serialization;
using Server.Targets;
using Server.Modules.Sphere51a.Configuration;
using Server.Modules.Sphere51a.Extensions;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public abstract partial class BaseKnife : BaseMeleeWeapon
    {
        public BaseKnife(int itemID) : base(itemID)
        {
        }

        public override int DefHitSound => 0x23B;
        public override int DefMissSound => 0x238;

        public override SkillName DefSkill => SkillName.Swords;
        public override WeaponType DefType => WeaponType.Slashing;
        public override WeaponAnimation DefAnimation => WeaponAnimation.Slash1H;

        public override void OnDoubleClick(Mobile from)
        {
            if (SphereConfiguration.Enabled && !(Parent == from && from.FindItemOnLayer(Layer) == this))
            {
                EquipmentHelper.TryEquipItem(from, this);
            }

            from.SendLocalizedMessage(1010018); // What do you want to use this item on?

            from.Target = new BladedItemTarget(this);
        }

        public override void OnHit(Mobile attacker, Mobile defender, double damageBonus = 1)
        {
            base.OnHit(attacker, defender, damageBonus);

            if (!Core.AOS && Poison != null && PoisonCharges > 0)
            {
                --PoisonCharges;

                if (Utility.RandomBool()) // 50% chance to poison
                {
                    defender.ApplyPoison(attacker, Poison);
                }
            }
        }
    }
}
