using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace ECS
{
    /// <summary>
    /// Generic array pooling with ArrayPool integration for reduced GC pressure
    /// </summary>
    public static class GenericArrayPool
    {
        private static readonly Dictionary<int, Queue<object[]>> _sizeSpecificPools = new();
        
        /// <summary>
        /// Get a pooled array for a specific component type
        /// </summary>
        public static T[] RentComponentArray<T>(int size)
        {
            return ArrayPool<T>.Shared.Rent(size);
        }
        
        /// <summary>
        /// Return a component array to the pool
        /// </summary>
        public static void ReturnComponentArray<T>(T[] array)
        {
            ArrayPool<T>.Shared.Return(array);
        }
        
        /// <summary>
        /// Get a pooled array of specific size
        /// </summary>
        public static object[] RentArray(int size)
        {
            if (!_sizeSpecificPools.TryGetValue(size, out var pool))
            {
                pool = new Queue<object[]>();
                _sizeSpecificPools[size] = pool;
            }
            
            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }
            
            return new object[size];
        }
        
        /// <summary>
        /// Return an array to the size-specific pool
        /// </summary>
        public static void ReturnArray(object[] array)
        {
            var size = array.Length;
            if (!_sizeSpecificPools.TryGetValue(size, out var pool))
            {
                pool = new Queue<object[]>();
                _sizeSpecificPools[size] = pool;
            }
            
            // Clear the array before returning to pool
            Array.Clear(array, 0, array.Length);
            pool.Enqueue(array);
        }
        
        /// <summary>
        /// Get memory statistics for monitoring
        /// </summary>
        public static (int typeSpecificPools, int sizeSpecificPools, int totalPooledArrays) GetPoolStatistics()
        {
            var sizeSpecificCount = _sizeSpecificPools.Count;
            var totalPooledArrays = _sizeSpecificPools.Values.Sum(pool => pool.Count);
            
            return (0, sizeSpecificCount, totalPooledArrays); // Using ArrayPool<T> for type-specific pools
        }
        
        /// <summary>
        /// Clear all pools (useful for testing or memory management)
        /// </summary>
        public static void ClearAllPools()
        {
            _sizeSpecificPools.Clear();
        }
    }
} 