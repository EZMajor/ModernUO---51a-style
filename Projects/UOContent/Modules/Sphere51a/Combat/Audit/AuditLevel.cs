namespace Server.Modules.Sphere51a.Combat.Audit;

/// <summary>
/// Defines the granularity level for combat audit logging.
/// Higher levels include all information from lower levels.
/// </summary>
public enum AuditLevel
{
    /// <summary>
    /// No audit logging performed. Audit system disabled.
    /// </summary>
    None = 0,

    /// <summary>
    /// Standard audit logging: records all combat actions with timing information.
    /// Includes: swing start/complete, hit resolution, basic metrics.
    /// </summary>
    Standard = 1,

    /// <summary>
    /// Detailed audit logging: includes Standard plus additional context.
    /// Includes: weapon details, stat calculations, cancellation reasons.
    /// </summary>
    Detailed = 2,

    /// <summary>
    /// Debug audit logging: includes Detailed plus verbose diagnostic information.
    /// Includes: timer state, pulse metrics, shadow mode comparisons.
    /// </summary>
    Debug = 3
}
