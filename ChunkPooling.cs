using System;
using System.Collections.Generic;
using System.Linq;

namespace ECS
{
    /// <summary>
    /// ECS-specific chunk pooling with memory tracking
    /// </summary>
    public static class ChunkPooling
    {
        private static readonly Dictionary<Archetype, Queue<PooledArchetypeChunk>> _archetypeChunkPools = new();
        private static readonly Dictionary<int, int> _chunkPoolStats = new();
        
        /// <summary>
        /// Rent a chunk for a specific archetype
        /// </summary>
        public static PooledArchetypeChunk RentChunk(Archetype archetype, int capacity = 1024)
        {
            if (!_archetypeChunkPools.TryGetValue(archetype, out var pool))
            {
                pool = new Queue<PooledArchetypeChunk>();
                _archetypeChunkPools[archetype] = pool;
            }
            
            if (pool.Count > 0)
            {
                var chunk = pool.Dequeue();
                chunk.Reset();
                return chunk;
            }
            
            return new PooledArchetypeChunk(archetype, capacity);
        }
        
        /// <summary>
        /// Return a chunk to the pool
        /// </summary>
        public static void ReturnChunk(PooledArchetypeChunk chunk)
        {
            var archetype = chunk.Archetype;
            if (!_archetypeChunkPools.TryGetValue(archetype, out var pool))
            {
                pool = new Queue<PooledArchetypeChunk>();
                _archetypeChunkPools[archetype] = pool;
            }
            
            pool.Enqueue(chunk);
        }
        
        /// <summary>
        /// Get chunk pool statistics
        /// </summary>
        public static (int archetypePools, int totalPooledChunks, long estimatedMemorySaved) GetChunkPoolStatistics()
        {
            var archetypePools = _archetypeChunkPools.Count;
            var totalPooledChunks = _archetypeChunkPools.Values.Sum(pool => pool.Count);
            var estimatedMemorySaved = totalPooledChunks * 1024 * 8; // Rough estimate: 1024 entities * 8 bytes per entity ID
            
            return (archetypePools, totalPooledChunks, estimatedMemorySaved);
        }
        
        /// <summary>
        /// Clear all chunk pools
        /// </summary>
        public static void ClearAllChunkPools()
        {
            _archetypeChunkPools.Clear();
        }
    }
    
    /// <summary>
    /// Memory-efficient chunk with ArrayPool integration
    /// </summary>
    public class PooledArchetypeChunk : IDisposable
    {
        private readonly Archetype _archetype;
        private readonly int _capacity;
        private int _count;
        private readonly object[][] _componentArrays;
        private readonly EntityId[] _entities;
        private readonly int[] _entityIndices;
        private readonly bool[] _rentedArrays;
        
        public Archetype Archetype => _archetype;
        
        public PooledArchetypeChunk(Archetype archetype, int capacity = 1024)
        {
            _archetype = archetype;
            _capacity = capacity;
            _count = 0;
            _componentArrays = new object[64][];
            _entities = new EntityId[capacity];
            _entityIndices = new int[capacity];
            _rentedArrays = new bool[64];
            
            // Rent arrays from pool
            foreach (var componentType in archetype.ComponentTypes)
            {
                if (componentType.Id >= 64)
                    throw new InvalidOperationException($"Component type ID {componentType.Id} exceeds maximum of 63");
                
                var array = GenericArrayPool.RentComponentArray<object>(capacity);
                _componentArrays[componentType.Id] = array;
                _rentedArrays[componentType.Id] = true;
            }
        }
        
        public int Count => _count;
        public int Capacity => _capacity;
        public bool IsFull => _count >= _capacity;
        
        /// <summary>
        /// Reset the chunk for reuse
        /// </summary>
        public void Reset()
        {
            _count = 0;
            // Clear entity arrays
            Array.Clear(_entities, 0, _entities.Length);
            Array.Clear(_entityIndices, 0, _entityIndices.Length);
            
            // Clear component arrays
            foreach (var componentType in _archetype.ComponentTypes)
            {
                if (_componentArrays[componentType.Id] != null)
                {
                    Array.Clear(_componentArrays[componentType.Id], 0, _componentArrays[componentType.Id].Length);
                }
            }
        }
        
        public int AddEntity(EntityId entityId)
        {
            if (IsFull)
                throw new InvalidOperationException("Chunk is full");
            
            _entities[_count] = entityId;
            _entityIndices[_count] = _count;
            _count++;
            return _count - 1;
        }
        
        public void RemoveEntity(int entityIndex)
        {
            if (entityIndex < 0 || entityIndex >= _count)
                throw new ArgumentOutOfRangeException(nameof(entityIndex));
            
            // Move last entity to this position
            if (entityIndex < _count - 1)
            {
                _entities[entityIndex] = _entities[_count - 1];
                _entityIndices[entityIndex] = _entityIndices[_count - 1];
                
                // Move component data
                foreach (var componentType in _archetype.ComponentTypes)
                {
                    var array = _componentArrays[componentType.Id];
                    array[entityIndex] = array[_count - 1];
                    array[_count - 1] = default!; // Clear the moved element
                }
            }
            
            _count--;
        }
        
        public T GetComponent<T>(int entityIndex, ComponentType componentType)
        {
            if (entityIndex < 0 || entityIndex >= _count)
                throw new ArgumentOutOfRangeException(nameof(entityIndex));
            
            if (componentType.Id < 0 || componentType.Id >= _componentArrays.Length)
                throw new ArgumentOutOfRangeException(nameof(componentType.Id));
            
            var array = _componentArrays[componentType.Id];
            if (array == null)
                throw new ArgumentException($"Component type {componentType.Id} not found in archetype");
            
            return (T)array[entityIndex];
        }
        
        public void SetComponent<T>(int entityIndex, ComponentType componentType, T component)
        {
            if (entityIndex < 0 || entityIndex >= _count)
                throw new ArgumentOutOfRangeException(nameof(entityIndex));
            
            if (componentType.Id < 0 || componentType.Id >= _componentArrays.Length)
                throw new ArgumentOutOfRangeException(nameof(componentType.Id));
            
            var array = _componentArrays[componentType.Id];
            if (array == null)
                throw new ArgumentException($"Component type {componentType.Id} not found in archetype");
            
            array[entityIndex] = component;
        }
        
        public EntityId GetEntity(int entityIndex)
        {
            if (entityIndex < 0 || entityIndex >= _count)
                throw new ArgumentOutOfRangeException(nameof(entityIndex));
            
            return _entities[entityIndex];
        }
        
        public void Dispose()
        {
            // Return all rented arrays to pool
            for (int i = 0; i < _componentArrays.Length; i++)
            {
                if (_rentedArrays[i] && _componentArrays[i] != null)
                {
                    GenericArrayPool.ReturnComponentArray<object>(_componentArrays[i]);
                    _componentArrays[i] = null;
                    _rentedArrays[i] = false;
                }
            }
        }
    }
} 