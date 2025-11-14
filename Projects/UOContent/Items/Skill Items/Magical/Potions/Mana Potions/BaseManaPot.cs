using System;
using Server.Targeting;

namespace Server.Items
{
    public abstract class BaseManaPot : BasePotion
    {
        public abstract int MinMana { get; }
        public abstract int MaxMana { get; }
        public abstract double Delay { get; }

        public BaseManaPot(PotionEffect effect) : base(0xF0C, effect)
        {
        }

        public BaseManaPot(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }

        public void DoMana(Mobile from)
        {
            var min = MinMana;
            var max = MaxMana;

            from.Mana += Utility.RandomMinMax(min, max);
        }

        public override void Drink(Mobile from)
        {
            if (from.Mana < from.ManaMax)
            {
                if (from.BeginAction<BaseManaPot>())
                {
                    DoMana(from);

                    PlayDrinkEffect(from);

                    if (!Core.AOS)
                    {
                        from.FixedEffect(0x375A, 10, 15);
                    }

                    Consume();

                    Timer.StartTimer(TimeSpan.FromSeconds(Delay), () => ReleaseManaLock(from));
                }
                else
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x22, 500235); // You must wait 10 seconds before using another mana potion.
                }
            }
            else
            {
                from.SendMessage("You decide against drinking this potion, as you are already at full mana.");
            }
        }

        private static void ReleaseManaLock(Mobile from)
        {
            from.EndAction<BaseManaPot>();
        }
    }
}
