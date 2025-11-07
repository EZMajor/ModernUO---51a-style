using System.Collections.Generic;

namespace Server.Modules.Sphere51a.Combat;

public class WeaponTimingConfig
{
    public string Version { get; set; }
    public string Description { get; set; }
    public Dictionary<string, WeaponConfigEntry> Weapons { get; set; } = new();
    public WeaponConfigEntry Defaults { get; set; }
}

public class WeaponConfigEntry
{
    public int BaseDelay { get; set; }
    public double SkillBonus { get; set; }
    public string Description { get; set; }
}
