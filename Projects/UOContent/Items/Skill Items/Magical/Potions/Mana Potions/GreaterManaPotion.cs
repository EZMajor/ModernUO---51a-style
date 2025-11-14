namespace Server.Items
{
    public class GreaterManaPotion : BaseManaPot
    {
        [Constructible]
        public GreaterManaPotion() : base(PotionEffect.ManaGreater)
        {
            Hue = 0x2D; // Orange hue for mana potions
        }

        public GreaterManaPotion(Serial serial) : base(serial)
        {
        }

        public override int MinMana => 40;
        public override int MaxMana => 40;
        public override double Delay => 10.0;

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
    }
}
