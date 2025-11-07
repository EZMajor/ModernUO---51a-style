using ModernUO.Serialization;
using Server.Gumps;
using Server.Multis;
using Server.Targeting;

namespace Server.Engines.DuelArena;

[TypeAlias("DuelArenaDeed", "duelarenaDeed")]
[SerializationGenerator(0)]
public partial class DuelArenaDeed : Item
{
    // Use 10x10 2-story customizable house multi for transparent preview
    public const int ArenaMultiID = 0x1413; // 10x10 2-Story Customizable House
    public static readonly Point3D ArenaOffset = new(0, 6, 0); // Offset from house placement entry

    [Constructible]
    public DuelArenaDeed() : base(0x14F0)
    {
        Weight = 1.0;
        Name = "a duel arena deed";
        Hue = 1109; // Blue-ish hue to match duel stones
        LootType = LootType.Blessed;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        if (from.AccessLevel < AccessLevel.GameMaster)
        {
            from.SendMessage("Only Game Masters can place duel arenas.");
            return;
        }

        from.SendMessage("Target the location where you want to place the duel arena.");
        from.SendMessage("A transparent preview will appear. Move your cursor to position it, then click to place.");

        from.Target = new ArenaTarget(this);
    }
}
