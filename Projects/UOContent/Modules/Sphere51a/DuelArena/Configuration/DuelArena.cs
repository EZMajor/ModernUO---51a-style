using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Engines.DuelArena;

[SerializationGenerator(0, false)]
public partial class DuelArena
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _name;

    [SerializableField(1)]
    private Point3D[] _spawnPoints;

    [SerializableField(2)]
    private Rectangle2D _bounds;

    [SerializableField(3)]
    private Map _map;

    [SerializableField(4)]
    private Point3D _exitLocation;

    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _maxPlayers;

    // Region is not serialized - it's recreated when needed
    public DuelArenaRegion Region { get; private set; }

    public bool IsInBounds(Point3D location) => Bounds.Contains(location);

    public DuelArena(string name, Point3D[] spawnPoints, Rectangle2D bounds, Map map, Point3D exitLocation, int maxPlayers = 2)
    {
        Name = name;
        SpawnPoints = spawnPoints;
        Bounds = bounds;
        Map = map;
        ExitLocation = exitLocation;
        MaxPlayers = maxPlayers;
    }

    [SerializableFieldDefault(0)]
    private string NameDefaultValue() => "Unnamed Arena";

    [SerializableFieldDefault(1)]
    private Point3D[] SpawnPointsDefaultValue() => [new Point3D(5450, 1162, 0), new Point3D(5460, 1162, 0)];

    [SerializableFieldDefault(2)]
    private Rectangle2D BoundsDefaultValue() => new Rectangle2D(5445, 1157, 20, 20);

    [SerializableFieldDefault(3)]
    private Map MapDefaultValue() => Map.Felucca;

    [SerializableFieldDefault(4)]
    private Point3D ExitLocationDefaultValue() => new Point3D(5440, 1150, 0);

    [SerializableFieldDefault(5)]
    private int MaxPlayersDefaultValue() => 2;

    public static DuelArena CreateDefault() => new(
        "Classic Arena",
        [new Point3D(5450, 1162, 0), new Point3D(5460, 1162, 0)],
        new Rectangle2D(5445, 1157, 20, 20),
        Map.Felucca,
        new Point3D(5440, 1150, 0),
        2
    );

    /// <summary>
    /// Creates a protected region for this arena that prevents karma loss,
    /// murder counts, and criminal flagging during duels.
    /// </summary>
    public void CreateRegion()
    {
        // Delete existing region if any
        DeleteRegion();

        // Create new region
        Region = new DuelArenaRegion(this);
    }

    /// <summary>
    /// Removes the protected region for this arena.
    /// </summary>
    public void DeleteRegion()
    {
        if (Region != null)
        {
            Region.Delete();
            Region = null;
        }
    }
}
