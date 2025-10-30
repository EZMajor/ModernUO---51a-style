/*************************************************************************
 * ModernUO - Sphere 0.51a Combat System
 * File: SphereConfigCache.cs
 *
 * Description: Per-tick configuration value caching system that reduces
 *              repeated property access in hot paths. Values are refreshed
 *              every 100ms to balance performance vs. responsiveness.
 *
 * Reference: PHASE4_IMPLEMENTATION_REPORT.md
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;

namespace Server.Systems.Combat.SphereStyle
{
    /// <summary>
    /// Caches frequently accessed SphereConfig values to reduce repeated
    /// property lookups in hot paths. Values are refreshed periodically.
    /// </summary>
    public static class SphereConfigCache
    {
        private const int CacheRefreshIntervalMs = 100; // Refresh every 100ms
        private static long _lastRefreshTick;

        // Cached configuration values
        private static bool _cachedIsEnabled;
        private static bool _cachedImmediateSpellTarget;
        private static bool _cachedAllowMovementDuringCast;
        private static bool _cachedClearHandsOnCast;
        private static bool _cachedTargetManaDeduction;
        private static int _cachedPartialManaPercent;
        private static bool _cachedRestrictedFizzleTriggers;
        private static bool _cachedDamageBasedFizzle;
        private static bool _cachedDisableSwingDuringCast;
        private static bool _cachedDisableSwingDuringCastDelay;
        private static bool _cachedSpellCancelSwing;
        private static bool _cachedSwingCancelSpell;

        // Static constructor initializes cache
        static SphereConfigCache()
        {
            RefreshCache();
        }

        /// <summary>
        /// Refreshes all cached configuration values.
        /// Called automatically when cache expires or manually if needed.
        /// </summary>
        private static void RefreshCache()
        {
            _lastRefreshTick = Core.TickCount;

            _cachedIsEnabled = SphereConfig.IsEnabled();
            _cachedImmediateSpellTarget = SphereConfig.ImmediateSpellTarget;
            _cachedAllowMovementDuringCast = SphereConfig.AllowMovementDuringCast;
            _cachedClearHandsOnCast = SphereConfig.ClearHandsOnCast;
            _cachedTargetManaDeduction = SphereConfig.TargetManaDeduction;
            _cachedPartialManaPercent = SphereConfig.PartialManaPercent;
            _cachedRestrictedFizzleTriggers = SphereConfig.RestrictedFizzleTriggers;
            _cachedDamageBasedFizzle = SphereConfig.DamageBasedFizzle;
            _cachedDisableSwingDuringCast = SphereConfig.DisableSwingDuringCast;
            _cachedDisableSwingDuringCastDelay = SphereConfig.DisableSwingDuringCastDelay;
            _cachedSpellCancelSwing = SphereConfig.SpellCancelSwing;
            _cachedSwingCancelSpell = SphereConfig.SwingCancelSpell;
        }

        /// <summary>
        /// Checks if cache needs refresh and refreshes if necessary.
        /// </summary>
        private static void CheckAndRefreshCache()
        {
            if (Core.TickCount - _lastRefreshTick >= CacheRefreshIntervalMs)
            {
                RefreshCache();
            }
        }

        #region Cached Property Accessors

        /// <summary>
        /// Gets cached value of IsEnabled configuration.
        /// </summary>
        public static bool IsEnabled
        {
            get
            {
                CheckAndRefreshCache();
                return _cachedIsEnabled;
            }
        }

        /// <summary>
        /// Gets cached value of ImmediateSpellTarget configuration.
        /// </summary>
        public static bool ImmediateSpellTarget
        {
            get
            {
                CheckAndRefreshCache();
                return _cachedImmediateSpellTarget;
            }
        }

        /// <summary>
        /// Gets cached value of AllowMovementDuringCast configuration.
        /// </summary>
        public static bool AllowMovementDuringCast
        {
            get
            {
                CheckAndRefreshCache();
                return _cachedAllowMovementDuringCast;
            }
        }

        /// <summary>
        /// Gets cached value of ClearHandsOnCast configuration.
        /// </summary>
        public static bool ClearHandsOnCast
        {
            get
            {
                CheckAndRefreshCache();
                return _cachedClearHandsOnCast;
            }
        }

        /// <summary>
        /// Gets cached value of TargetManaDeduction configuration.
        /// </summary>
        public static bool TargetManaDeduction
        {
            get
            {
                CheckAndRefreshCache();
                return _cachedTargetManaDeduction;
            }
        }

        /// <summary>
        /// Gets cached value of PartialManaPercent configuration.
        /// </summary>
        public static int PartialManaPercent
        {
            get
            {
                CheckAndRefreshCache();
                return _cachedPartialManaPercent;
            }
        }

        /// <summary>
        /// Gets cached value of RestrictedFizzleTriggers configuration.
        /// </summary>
        public static bool RestrictedFizzleTriggers
        {
            get
            {
                CheckAndRefreshCache();
                return _cachedRestrictedFizzleTriggers;
            }
        }

        /// <summary>
        /// Gets cached value of DamageBasedFizzle configuration.
        /// </summary>
        public static bool DamageBasedFizzle
        {
            get
            {
                CheckAndRefreshCache();
                return _cachedDamageBasedFizzle;
            }
        }

        /// <summary>
        /// Gets cached value of DisableSwingDuringCast configuration.
        /// </summary>
        public static bool DisableSwingDuringCast
        {
            get
            {
                CheckAndRefreshCache();
                return _cachedDisableSwingDuringCast;
            }
        }

        /// <summary>
        /// Gets cached value of DisableSwingDuringCastDelay configuration.
        /// </summary>
        public static bool DisableSwingDuringCastDelay
        {
            get
            {
                CheckAndRefreshCache();
                return _cachedDisableSwingDuringCastDelay;
            }
        }

        /// <summary>
        /// Gets cached value of SpellCancelSwing configuration.
        /// </summary>
        public static bool SpellCancelSwing
        {
            get
            {
                CheckAndRefreshCache();
                return _cachedSpellCancelSwing;
            }
        }

        /// <summary>
        /// Gets cached value of SwingCancelSpell configuration.
        /// </summary>
        public static bool SwingCancelSpell
        {
            get
            {
                CheckAndRefreshCache();
                return _cachedSwingCancelSpell;
            }
        }

        #endregion

        #region Manual Cache Control

        /// <summary>
        /// Invalidates the cache, forcing a refresh on next access.
        /// </summary>
        public static void Invalidate()
        {
            _lastRefreshTick = 0;
        }

        /// <summary>
        /// Gets the current cache age in milliseconds.
        /// </summary>
        public static long GetCacheAge()
        {
            return Core.TickCount - _lastRefreshTick;
        }

        /// <summary>
        /// Gets cache statistics for monitoring.
        /// </summary>
        public static CacheStatistics GetStatistics()
        {
            return new CacheStatistics
            {
                CacheAgeMs = GetCacheAge(),
                RefreshIntervalMs = CacheRefreshIntervalMs,
                IsDirty = GetCacheAge() >= CacheRefreshIntervalMs
            };
        }

        #endregion

        #region Cache Statistics

        /// <summary>
        /// Provides cache statistics for monitoring and debugging.
        /// </summary>
        public class CacheStatistics
        {
            /// <summary>
            /// Age of the cache in milliseconds.
            /// </summary>
            public long CacheAgeMs { get; set; }

            /// <summary>
            /// Configured refresh interval in milliseconds.
            /// </summary>
            public int RefreshIntervalMs { get; set; }

            /// <summary>
            /// Whether the cache needs refreshing.
            /// </summary>
            public bool IsDirty { get; set; }

            public override string ToString()
            {
                return $"Cache Age: {CacheAgeMs}ms, Interval: {RefreshIntervalMs}ms, Dirty: {IsDirty}";
            }
        }

        #endregion
    }
}
