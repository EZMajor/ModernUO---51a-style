/*************************************************************************
 * ModernUO - Sphere 0.51a Combat System
 * File: ObjectPool.cs
 *
 * Description: Generic object pool implementation for reducing memory
 *              allocations and garbage collection pressure in hot paths.
 *
 * Reference: PHASE4_IMPLEMENTATION_REPORT.md
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Collections.Generic;

namespace Server.Systems.Combat.SphereStyle
{
    /// <summary>
    /// Interface for poolable objects. Implementations must support
    /// reset and reinitialization for pool reuse.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Resets the object to a clean state for pool reuse.
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Generic object pool for reducing allocations in performance-critical code.
    /// Implements the object pool pattern with thread-safe operations.
    /// </summary>
    /// <typeparam name="T">The type of objects to pool. Must implement IPoolable.</typeparam>
    public class ObjectPool<T> where T : class, IPoolable, new()
    {
        private readonly Stack<T> _available;
        private readonly int _maxSize;
        private readonly Action<T> _resetAction;
        private int _rentedCount;
        private int _totalCreated;
        private readonly object _lockObject = new();

        /// <summary>
        /// Gets the number of objects currently rented from the pool.
        /// </summary>
        public int RentedCount
        {
            get
            {
                lock (_lockObject)
                {
                    return _rentedCount;
                }
            }
        }

        /// <summary>
        /// Gets the number of available objects in the pool.
        /// </summary>
        public int AvailableCount
        {
            get
            {
                lock (_lockObject)
                {
                    return _available.Count;
                }
            }
        }

        /// <summary>
        /// Gets the total number of objects created by this pool.
        /// </summary>
        public int TotalCreated
        {
            get
            {
                lock (_lockObject)
                {
                    return _totalCreated;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the ObjectPool class.
        /// </summary>
        /// <param name="initialSize">The initial number of objects to pre-allocate.</param>
        /// <param name="maxSize">The maximum number of objects to keep in the pool.</param>
        /// <param name="resetAction">Optional action to reset objects before returning to pool.</param>
        public ObjectPool(int initialSize = 100, int maxSize = 500, Action<T> resetAction = null)
        {
            _available = new Stack<T>(initialSize);
            _maxSize = maxSize;
            _resetAction = resetAction;
            _rentedCount = 0;
            _totalCreated = 0;

            // Pre-allocate initial objects
            for (int i = 0; i < initialSize; i++)
            {
                var obj = new T();
                _available.Push(obj);
                _totalCreated++;
            }
        }

        /// <summary>
        /// Rents an object from the pool. If no objects are available,
        /// creates a new one.
        /// </summary>
        /// <returns>An object from the pool or a newly created object.</returns>
        public T Rent()
        {
            lock (_lockObject)
            {
                T obj;

                if (_available.Count > 0)
                {
                    obj = _available.Pop();
                }
                else
                {
                    obj = new T();
                    _totalCreated++;
                }

                _rentedCount++;
                return obj;
            }
        }

        /// <summary>
        /// Returns an object to the pool. The object will be reset before
        /// being made available for reuse.
        /// </summary>
        /// <param name="obj">The object to return to the pool.</param>
        public void Return(T obj)
        {
            if (obj == null)
                return;

            lock (_lockObject)
            {
                // Only return to pool if we haven't exceeded max size
                if (_available.Count < _maxSize)
                {
                    // Reset the object for reuse
                    obj.Reset();
                    _resetAction?.Invoke(obj);

                    _available.Push(obj);
                }

                _rentedCount--;
            }
        }

        /// <summary>
        /// Clears the pool and disposes all objects if they implement IDisposable.
        /// </summary>
        public void Clear()
        {
            lock (_lockObject)
            {
                while (_available.Count > 0)
                {
                    var obj = _available.Pop();

                    if (obj is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }

                _rentedCount = 0;
            }
        }

        /// <summary>
        /// Gets pool statistics for monitoring and debugging.
        /// </summary>
        public PoolStatistics GetStatistics()
        {
            lock (_lockObject)
            {
                return new PoolStatistics
                {
                    PoolType = typeof(T).Name,
                    RentedCount = _rentedCount,
                    AvailableCount = _available.Count,
                    TotalCreated = _totalCreated,
                    MaxSize = _maxSize
                };
            }
        }

        /// <summary>
        /// Provides statistics about the pool state.
        /// </summary>
        public class PoolStatistics
        {
            public string PoolType { get; set; }
            public int RentedCount { get; set; }
            public int AvailableCount { get; set; }
            public int TotalCreated { get; set; }
            public int MaxSize { get; set; }

            public override string ToString()
            {
                return $"{PoolType} Pool: Rented={RentedCount}, Available={AvailableCount}, Total={TotalCreated}, Max={MaxSize}";
            }
        }
    }
}
