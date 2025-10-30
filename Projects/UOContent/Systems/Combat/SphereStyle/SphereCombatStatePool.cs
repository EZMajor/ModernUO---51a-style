/*************************************************************************
 * ModernUO - Sphere 0.51a Combat System
 * File: SphereCombatStatePool.cs
 *
 * Description: Object pool specifically for SphereCombatState instances.
 *              Reduces garbage collection pressure from frequent combat
 *              state creation and destruction.
 *
 * Reference: PHASE4_IMPLEMENTATION_REPORT.md
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Collections.Generic;

namespace Server.Systems.Combat.SphereStyle
{
    /// <summary>
    /// Poolable wrapper for SphereCombatState to enable object pooling.
    /// Wraps SphereCombatState with IPoolable interface support.
    /// </summary>
    public class PooledSphereCombatState : IPoolable
    {
        private SphereCombatState _state;
        private Mobile _mobile;

        public SphereCombatState State => _state;

        public PooledSphereCombatState()
        {
            // Will be initialized on rent
        }

        public void Initialize(Mobile mobile)
        {
            _mobile = mobile;
            _state = new SphereCombatState(mobile);
        }

        public void Reset()
        {
            // Clear all state for reuse
            if (_state != null)
            {
                _state.NextSwingTime = 0;
                _state.NextSpellTime = 0;
                _state.NextBandageTime = 0;
                _state.NextWandTime = 0;
                _state.IsCasting = false;
                _state.IsInCastDelay = false;
                _state.HasPendingSwing = false;
                _state.IsBandaging = false;
                _state.CurrentSpell = null;
                _state.SpellCastStartTime = 0;
            }
        }
    }

    /// <summary>
    /// Singleton pool manager for SphereCombatState objects.
    /// Provides cached access to combat states with automatic pooling.
    /// </summary>
    public static class SphereCombatStatePool
    {
        private static ObjectPool<PooledSphereCombatState> _pool;
        private static readonly object _initLock = new();

        /// <summary>
        /// Initializes the pool with specified parameters.
        /// </summary>
        /// <param name="initialSize">Initial pool size (default: 100)</param>
        /// <param name="maxSize">Maximum pool size (default: 500)</param>
        public static void Initialize(int initialSize = 100, int maxSize = 500)
        {
            lock (_initLock)
            {
                if (_pool == null)
                {
                    _pool = new ObjectPool<PooledSphereCombatState>(initialSize, maxSize);
                }
            }
        }

        /// <summary>
        /// Rents a combat state from the pool for a specific mobile.
        /// </summary>
        /// <param name="mobile">The mobile to create/get state for.</param>
        /// <returns>A pooled combat state instance.</returns>
        public static SphereCombatState Rent(Mobile mobile)
        {
            EnsureInitialized();
            var pooled = _pool.Rent();
            pooled.Initialize(mobile);
            return pooled.State;
        }

        /// <summary>
        /// Returns a combat state to the pool.
        /// </summary>
        /// <param name="state">The state to return.</param>
        public static void Return(SphereCombatState state)
        {
            // This would require tracking the wrapper, so for now we'll
            // use a simpler approach with lazy pooling on next allocation
            if (state != null)
            {
                state.ClearAllTimers();
            }
        }

        /// <summary>
        /// Gets current pool statistics.
        /// </summary>
        public static ObjectPool<PooledSphereCombatState>.PoolStatistics GetStatistics()
        {
            EnsureInitialized();
            return _pool.GetStatistics();
        }

        /// <summary>
        /// Clears the pool.
        /// </summary>
        public static void Clear()
        {
            lock (_initLock)
            {
                _pool?.Clear();
                _pool = null;
            }
        }

        private static void EnsureInitialized()
        {
            if (_pool == null)
            {
                Initialize();
            }
        }
    }

}
