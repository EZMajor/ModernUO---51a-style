/*************************************************************************
 * ModernUO - Sphere 0.51a Combat System
 * File: SphereSpellMantras.cs
 *
 * Description: Caching system for spell mantras and related string data.
 *              Reduces memory allocations from repeated string operations.
 *
 * Reference: PHASE4_IMPLEMENTATION_REPORT.md
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Collections.Generic;
using Server.Spells;

namespace Server.Systems.Combat.SphereStyle
{
    /// <summary>
    /// Caching system for spell mantras to reduce string allocations.
    /// </summary>
    public static class SphereSpellMantras
    {
        private static readonly Dictionary<Type, string> _mantrasCache = new Dictionary<Type, string>();
        private static readonly object _lockObject = new object();

        /// <summary>
        /// Gets the cached mantra for a spell type.
        /// </summary>
        public static string GetMantra(Type spellType)
        {
            if (spellType == null)
                return null;

            if (_mantrasCache.TryGetValue(spellType, out var cachedMantra))
                return cachedMantra;

            lock (_lockObject)
            {
                if (_mantrasCache.TryGetValue(spellType, out cachedMantra))
                    return cachedMantra;

                var mantra = RetrieveSpellMantra(spellType);
                _mantrasCache[spellType] = mantra;
                return mantra;
            }
        }

        /// <summary>
        /// Gets the cached mantra for a spell instance.
        /// </summary>
        public static string GetMantra(Spell spell)
        {
            if (spell == null)
                return null;

            return GetMantra(spell.GetType());
        }

        /// <summary>
        /// Clears the mantra cache.
        /// </summary>
        public static void ClearCache()
        {
            lock (_lockObject)
            {
                _mantrasCache.Clear();
            }
        }

        /// <summary>
        /// Gets cache statistics for monitoring.
        /// </summary>
        public static MantaraCacheStats GetStatistics()
        {
            lock (_lockObject)
            {
                return new MantaraCacheStats
                {
                    CachedMantras = _mantrasCache.Count,
                    CacheSize = _mantrasCache.Count * 50
                };
            }
        }

        private static string RetrieveSpellMantra(Type spellType)
        {
            try
            {
                if (Activator.CreateInstance(spellType) is Spell spell)
                {
                    return spell.Info?.Mantra;
                }
            }
            catch
            {
            }

            return null;
        }

        public class MantaraCacheStats
        {
            public int CachedMantras { get; set; }
            public int CacheSize { get; set; }

            public override string ToString()
            {
                return $"Mantras Cached: {CachedMantras}, Est. Size: {CacheSize} bytes";
            }
        }
    }

    /// <summary>
    /// StringBuilder pooling for reducing allocations in message formatting.
    /// </summary>
    public static class SphereStringBuilder
    {
        private static readonly Stack<System.Text.StringBuilder> _pool =
            new Stack<System.Text.StringBuilder>(20);
        private static readonly object _lockObject = new object();

        /// <summary>
        /// Rents a StringBuilder from the pool.
        /// </summary>
        public static System.Text.StringBuilder Rent(int capacity = 16)
        {
            lock (_lockObject)
            {
                System.Text.StringBuilder sb;

                if (_pool.Count > 0)
                {
                    sb = _pool.Pop();
                    sb.Capacity = Math.Max(sb.Capacity, capacity);
                }
                else
                {
                    sb = new System.Text.StringBuilder(capacity);
                }

                return sb;
            }
        }

        /// <summary>
        /// Returns a StringBuilder to the pool for reuse.
        /// </summary>
        public static void Return(System.Text.StringBuilder sb)
        {
            if (sb == null)
                return;

            lock (_lockObject)
            {
                if (_pool.Count < 20)
                {
                    sb.Clear();
                    _pool.Push(sb);
                }
            }
        }

        /// <summary>
        /// Gets a string from a StringBuilder and returns it to the pool.
        /// </summary>
        public static string GetStringAndReturn(System.Text.StringBuilder sb)
        {
            var result = sb.ToString();
            Return(sb);
            return result;
        }

        /// <summary>
        /// Clears the pool.
        /// </summary>
        public static void Clear()
        {
            lock (_lockObject)
            {
                _pool.Clear();
            }
        }
    }
}
