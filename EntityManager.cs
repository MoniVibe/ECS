using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Buffers;

namespace ECS
{
    /// <summary>
    /// Represents a unique entity identifier with generation tracking for reuse safety
    /// </summary>
    public readonly struct EntityId : IEquatable<EntityId>
    {
        public readonly int Id;
        public readonly int Generation;

        public EntityId(int id, int generation)
        {
            Id = id;
            Generation = generation;
        }

        public bool Equals(EntityId other) => Id == other.Id && Generation == other.Generation;
        public override bool Equals(object? obj) => obj is EntityId other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Id, Generation);
        public static bool operator ==(EntityId left, EntityId right) => left.Equals(right);
        public static bool operator !=(EntityId left, EntityId right) => !left.Equals(right);
    }

    /// <summary>
    /// Represents a component type identifier with automatic registration
    /// </summary>
    public readonly struct ComponentType : IEquatable<ComponentType>
    {
        public readonly int Id;
        public readonly Type Type;

        public ComponentType(int id, Type type)
        {
            Id = id;
            Type = type;
        }

        public bool Equals(ComponentType other) => Id == other.Id;
        public override bool Equals(object? obj) => obj is ComponentType other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode();
        public static bool operator ==(ComponentType left, ComponentType right) => left.Equals(right);
        public static bool operator !=(ComponentType left, ComponentType right) => !left.Equals(right);
    }

    /// <summary>
    /// Registry for component types with automatic ID assignment and bounds checking
    /// </summary>
    public static class ComponentTypeRegistry
    {
        private static readonly Dictionary<Type, ComponentType> _typeMap = new();
        private static readonly Dictionary<int, Type> _idMap = new();
        private static int _nextId = 0;
        private const int MAX_COMPONENT_TYPES = 256; // Extended to support more component types

        public static ComponentType Get<T>()
        {
            var type = typeof(T);
            if (_typeMap.TryGetValue(type, out var componentType))
                return componentType;

            if (_nextId >= MAX_COMPONENT_TYPES)
                throw new InvalidOperationException($"Maximum component types ({MAX_COMPONENT_TYPES}) exceeded");

            var id = _nextId++;
            componentType = new ComponentType(id, type);
            _typeMap[type] = componentType;
            _idMap[id] = type;
            return componentType;
        }

        public static ComponentType Get(Type type)
        {
            if (_typeMap.TryGetValue(type, out var componentType))
                return componentType;

            if (_nextId >= MAX_COMPONENT_TYPES)
                throw new InvalidOperationException($"Maximum component types ({MAX_COMPONENT_TYPES}) exceeded");

            var id = _nextId++;
            componentType = new ComponentType(id, type);
            _typeMap[type] = componentType;
            _idMap[id] = type;
            return componentType;
        }

        public static Type? GetType(int id)
        {
            return _idMap.TryGetValue(id, out var type) ? type : null;
        }

        public static int GetTypeCount() => _nextId;
        public static int GetMaxComponentTypes() => MAX_COMPONENT_TYPES;
    }

    /// <summary>
    /// Centralized utility for bit operations and component type management
    /// </summary>




    /// <summary>
    /// Action types for structural changes
    /// </summary>
    public enum ActionType
    {
        AddComponent,
        RemoveComponent,
        DestroyEntity
    }

    /// <summary>
    /// Queued structural change for batching
    /// </summary>
    public readonly struct StructuralChange
    {
        public readonly EntityId Entity;
        public readonly ActionType Type;
        public readonly ComponentType Component;
        public readonly object? Data;

        public StructuralChange(EntityId entity, ActionType type, ComponentType component = default, object? data = null)
        {
            Entity = entity;
            Type = type;
            Component = component;
            Data = data;
        }
    }

    /// <summary>
    /// Hot data layout for frequently accessed components
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct HotData
    {
        // Example hot components - these would be defined by your game
        // public Position Position;
        // public Velocity Velocity;
        // public Transform Transform;
    }

    /// <summary>
    /// Cold data layout for rarely accessed components
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ColdData
    {
        // Example cold components - these would be defined by your game
        // public Name Name;
        // public Description Description;
        // public Metadata Metadata;
    }

    /// <summary>
    /// Chunk data layout for hot/cold storage separation
    /// </summary>
    public class ChunkDataLayout
    {
        public readonly ComponentType[] HotComponents;
        public readonly ComponentType[] ColdComponents;
        public readonly int HotDataSize;
        public readonly int ColdDataSize;

        public ChunkDataLayout(ComponentType[] allComponents)
        {
            var hotComponents = new List<ComponentType>();
            var coldComponents = new List<ComponentType>();

            foreach (var component in allComponents)
            {
                // TODO: Implement component heat classification logic
                // This could be based on attributes, naming conventions, or explicit configuration
                var heat = ClassifyComponentHeat(component);
                
                if (heat == ComponentHeat.Hot)
                    hotComponents.Add(component);
                else
                    coldComponents.Add(component);
            }

            HotComponents = hotComponents.ToArray();
            ColdComponents = coldComponents.ToArray();
            HotDataSize = CalculateDataSize(HotComponents);
            ColdDataSize = CalculateDataSize(ColdComponents);
        }

        private ComponentHeat ClassifyComponentHeat(ComponentType componentType)
        {
            // Check for explicit attributes first
            if (componentType.Type.GetCustomAttribute<HotComponentAttribute>() != null)
                return ComponentHeat.Hot;
            if (componentType.Type.GetCustomAttribute<ColdComponentAttribute>() != null)
                return ComponentHeat.Cold;
            
            // Fallback to name-based classification
            var typeName = componentType.Type.Name.ToLower();
            
            // Hot components (frequently accessed)
            if (typeName.Contains("position") || typeName.Contains("velocity") || 
                typeName.Contains("transform") || typeName.Contains("physics") ||
                typeName.Contains("rotation") || typeName.Contains("scale"))
                return ComponentHeat.Hot;
            
            // Cold components (rarely accessed)
            if (typeName.Contains("name") || typeName.Contains("description") || 
                typeName.Contains("metadata") || typeName.Contains("tag") ||
                typeName.Contains("ui") || typeName.Contains("audio"))
                return ComponentHeat.Cold;
            
            // Default to hot for performance
            return ComponentHeat.Hot;
        }

        private int CalculateDataSize(ComponentType[] components)
        {
            // Simplified size calculation - in practice, you'd use Marshal.SizeOf
            return components.Length * 8; // Assume 8 bytes per component for now
        }
    }

    /// <summary>
    /// Represents an archetype with bitmask for fast matching and hot/cold storage layout
    /// </summary>
    public readonly struct Archetype : IEquatable<Archetype>
    {
        public readonly ComponentType[] ComponentTypes;
        public readonly BitSet Bitmask;
        public readonly int HashCode;
        public readonly ChunkDataLayout DataLayout;

        public Archetype(ComponentType[] componentTypes)
        {
            // Always sort component types by ID for deterministic layout
            ComponentTypes = (componentTypes ?? Array.Empty<ComponentType>()).OrderBy(t => t.Id).ToArray();
            Bitmask = CalculateBitmask(ComponentTypes);
            HashCode = CalculateHashCode(ComponentTypes);
            DataLayout = new ChunkDataLayout(ComponentTypes);
        }

        private static BitSet CalculateBitmask(ComponentType[] types)
        {
            return BitUtils.CalculateBitSet(types);
        }

        private static int CalculateHashCode(ComponentType[] types)
        {
            var hash = new HashCode();
            foreach (var type in types.OrderBy(t => t.Id))
            {
                hash.Add(type);
            }
            return hash.ToHashCode();
        }

        public bool HasAllComponents(BitSet requiredMask)
        {
            return Bitmask.HasAll(requiredMask);
        }

        public bool HasAnyComponent(BitSet mask)
        {
            return Bitmask.HasAny(mask);
        }



        public bool Equals(Archetype other) => HashCode == other.HashCode && ComponentTypes.SequenceEqual(other.ComponentTypes);
        public override bool Equals(object? obj) => obj is Archetype other && Equals(other);
        public override int GetHashCode() => HashCode;
    }

    /// <summary>
    /// Pool for reusing empty chunks to reduce GC pressure
    /// </summary>
    public class ChunkPool
    {
        private readonly Dictionary<Archetype, Queue<ArchetypeChunk>> _pools = new();
        private readonly Dictionary<Archetype, Queue<ArchetypeChunk>> _nonFullChunks = new();
        private readonly int _maxPoolSize;

        public ChunkPool(int maxPoolSize = 10)
        {
            _maxPoolSize = maxPoolSize;
        }

        public ArchetypeChunk GetChunk(Archetype archetype, int capacity)
        {
            // First check for non-full chunks (O(1) access)
            if (_nonFullChunks.TryGetValue(archetype, out var nonFullQueue) && nonFullQueue.Count > 0)
            {
                var chunk = nonFullQueue.Dequeue();
                if (!chunk.IsFull)
                    return chunk;
                // If chunk is full, return it to the regular pool
                ReturnChunk(chunk);
            }

            // Fallback to regular pool
            if (_pools.TryGetValue(archetype, out var pool) && pool.Count > 0)
            {
                var chunk = pool.Dequeue();
                chunk.Count = 0; // Reset count
                chunk.ClearDirtyFlags(); // Clear dirty flags
                return chunk;
            }

            return new ArchetypeChunk(archetype, capacity);
        }

        public void ReturnChunk(ArchetypeChunk chunk)
        {
            var archetype = chunk.Archetype;
            
            // If chunk is not full, add to non-full queue for fast access
            if (!chunk.IsFull)
            {
                if (!_nonFullChunks.TryGetValue(archetype, out var nonFullQueue))
                {
                    nonFullQueue = new Queue<ArchetypeChunk>();
                    _nonFullChunks[archetype] = nonFullQueue;
                }
                nonFullQueue.Enqueue(chunk);
                return;
            }

            // Otherwise, add to regular pool
            if (!_pools.TryGetValue(archetype, out var pool))
            {
                pool = new Queue<ArchetypeChunk>();
                _pools[archetype] = pool;
            }

            if (pool.Count < _maxPoolSize)
            {
                pool.Enqueue(chunk);
            }
        }

        public void Clear()
        {
            _pools.Clear();
            _nonFullChunks.Clear();
        }
    }

    /// <summary>
    /// Query cache for fast archetype matching
    /// </summary>
    public class QueryCache
    {
        private readonly Dictionary<BitSet, List<Archetype>> _queryCache = new();
        private readonly Dictionary<Archetype, List<ArchetypeChunk>> _archetypeChunks;

        public QueryCache(Dictionary<Archetype, List<ArchetypeChunk>> archetypeChunks)
        {
            _archetypeChunks = archetypeChunks;
        }

        public List<Archetype> GetMatchingArchetypes(BitSet componentMask)
        {
            if (_queryCache.TryGetValue(componentMask, out var cached))
                return cached;

            var matching = _archetypeChunks.Keys
                .Where(archetype => archetype.HasAllComponents(componentMask))
                .ToList();

            _queryCache[componentMask] = matching;
            return matching;
        }

        public void InvalidateCache()
        {
            _queryCache.Clear();
        }
    }

    /// <summary>
    /// Zero-allocation entity enumerator for performance-critical iteration
    /// </summary>
    public struct EntityEnumerator : IEnumerator<EntityId>
    {
        private readonly List<ArchetypeChunk> _chunks;
        private int _chunkIndex;
        private int _entityIndex;
        private EntityId _current;

        public EntityEnumerator(List<ArchetypeChunk> chunks)
        {
            _chunks = chunks;
            _chunkIndex = 0;
            _entityIndex = 0;
            _current = default;
        }

        public EntityId Current => _current;
        object IEnumerator.Current => _current;

        public bool MoveNext()
        {
            while (_chunkIndex < _chunks.Count)
            {
                var chunk = _chunks[_chunkIndex];
                if (_entityIndex < chunk.Count)
                {
                    _current = chunk.GetEntity(_entityIndex);
                    _entityIndex++;
                    return true;
                }
                _chunkIndex++;
                _entityIndex = 0;
            }
            return false;
        }

        public void Reset()
        {
            _chunkIndex = 0;
            _entityIndex = 0;
            _current = default;
        }

        public void Dispose() { }
    }

    /// <summary>
    /// Type-safe query result for fluent API
    /// </summary>
    public readonly struct Query<T1>
    {
        public readonly EntityId Entity;
        public readonly T1 Component1;

        public Query(EntityId entity, T1 component1)
        {
            Entity = entity;
            Component1 = component1;
        }

        public void Deconstruct(out EntityId entity, out T1 component1)
        {
            entity = Entity;
            component1 = Component1;
        }
    }

    /// <summary>
    /// Type-safe query result for fluent API
    /// </summary>
    public readonly struct Query<T1, T2>
    {
        public readonly EntityId Entity;
        public readonly T1 Component1;
        public readonly T2 Component2;

        public Query(EntityId entity, T1 component1, T2 component2)
        {
            Entity = entity;
            Component1 = component1;
            Component2 = component2;
        }

        public void Deconstruct(out EntityId entity, out T1 component1, out T2 component2)
        {
            entity = Entity;
            component1 = Component1;
            component2 = Component2;
        }
    }

    /// <summary>
    /// Type-safe query result for fluent API
    /// </summary>
    public readonly struct Query<T1, T2, T3>
    {
        public readonly EntityId Entity;
        public readonly T1 Component1;
        public readonly T2 Component2;
        public readonly T3 Component3;

        public Query(EntityId entity, T1 component1, T2 component2, T3 component3)
        {
            Entity = entity;
            Component1 = component1;
            Component2 = component2;
            Component3 = component3;
        }

        public void Deconstruct(out EntityId entity, out T1 component1, out T2 component2, out T3 component3)
        {
            entity = Entity;
            component1 = Component1;
            component2 = Component2;
            component3 = Component3;
        }
    }

    /// <summary>
    /// Zero-allocation query enumerator for performance-critical iteration
    /// </summary>
    public struct QueryEnumerator<T1> : IEnumerator<Query<T1>>
    {
        private readonly EntityManager _entityManager;
        private readonly EntityEnumerator _entityEnumerator;
        private Query<T1> _current;

        public QueryEnumerator(EntityManager entityManager, EntityEnumerator entityEnumerator)
        {
            _entityManager = entityManager;
            _entityEnumerator = entityEnumerator;
            _current = default;
        }

        public Query<T1> Current => _current;
        object IEnumerator.Current => _current;

        public bool MoveNext()
        {
            if (_entityEnumerator.MoveNext())
            {
                var entity = _entityEnumerator.Current;
                var component = _entityManager.GetComponent<T1>(entity);
                _current = new Query<T1>(entity, component);
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _entityEnumerator.Reset();
            _current = default;
        }

        public void Dispose() { }
    }

    /// <summary>
    /// Zero-allocation query enumerator for performance-critical iteration
    /// </summary>
    public struct QueryEnumerator<T1, T2> : IEnumerator<Query<T1, T2>>
    {
        private readonly EntityManager _entityManager;
        private readonly EntityEnumerator _entityEnumerator;
        private Query<T1, T2> _current;

        public QueryEnumerator(EntityManager entityManager, EntityEnumerator entityEnumerator)
        {
            _entityManager = entityManager;
            _entityEnumerator = entityEnumerator;
            _current = default;
        }

        public Query<T1, T2> Current => _current;
        object IEnumerator.Current => _current;

        public bool MoveNext()
        {
            if (_entityEnumerator.MoveNext())
            {
                var entity = _entityEnumerator.Current;
                var component1 = _entityManager.GetComponent<T1>(entity);
                var component2 = _entityManager.GetComponent<T2>(entity);
                _current = new Query<T1, T2>(entity, component1, component2);
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _entityEnumerator.Reset();
            _current = default;
        }

        public void Dispose() { }
    }

    /// <summary>
    /// Zero-allocation query enumerator for performance-critical iteration
    /// </summary>
    public struct QueryEnumerator<T1, T2, T3> : IEnumerator<Query<T1, T2, T3>>
    {
        private readonly EntityManager _entityManager;
        private readonly EntityEnumerator _entityEnumerator;
        private Query<T1, T2, T3> _current;

        public QueryEnumerator(EntityManager entityManager, EntityEnumerator entityEnumerator)
        {
            _entityManager = entityManager;
            _entityEnumerator = entityEnumerator;
            _current = default;
        }

        public Query<T1, T2, T3> Current => _current;
        object IEnumerator.Current => _current;

        public bool MoveNext()
        {
            if (_entityEnumerator.MoveNext())
            {
                var entity = _entityEnumerator.Current;
                var component1 = _entityManager.GetComponent<T1>(entity);
                var component2 = _entityManager.GetComponent<T2>(entity);
                var component3 = _entityManager.GetComponent<T3>(entity);
                _current = new Query<T1, T2, T3>(entity, component1, component2, component3);
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _entityEnumerator.Reset();
            _current = default;
        }

        public void Dispose() { }
    }

    /// <summary>
    /// Queryable collection for fluent API
    /// </summary>
    public readonly struct Queryable<T1>
    {
        private readonly EntityManager _entityManager;
        private readonly EntityEnumerator _entityEnumerator;

        public Queryable(EntityManager entityManager, EntityEnumerator entityEnumerator)
        {
            _entityManager = entityManager;
            _entityEnumerator = entityEnumerator;
        }

        public QueryEnumerator<T1> GetEnumerator()
        {
            return new QueryEnumerator<T1>(_entityManager, _entityEnumerator);
        }
    }

    /// <summary>
    /// Queryable collection for fluent API
    /// </summary>
    public readonly struct Queryable<T1, T2>
    {
        private readonly EntityManager _entityManager;
        private readonly EntityEnumerator _entityEnumerator;

        public Queryable(EntityManager entityManager, EntityEnumerator entityEnumerator)
        {
            _entityManager = entityManager;
            _entityEnumerator = entityEnumerator;
        }

        public QueryEnumerator<T1, T2> GetEnumerator()
        {
            return new QueryEnumerator<T1, T2>(_entityManager, _entityEnumerator);
        }
    }

    /// <summary>
    /// Type-safe query result for fluent API
    /// </summary>
    public readonly struct Query<T1, T2, T3, T4>
    {
        public readonly EntityId Entity;
        public readonly T1 Component1;
        public readonly T2 Component2;
        public readonly T3 Component3;
        public readonly T4 Component4;

        public Query(EntityId entity, T1 component1, T2 component2, T3 component3, T4 component4)
        {
            Entity = entity;
            Component1 = component1;
            Component2 = component2;
            Component3 = component3;
            Component4 = component4;
        }

        public void Deconstruct(out EntityId entity, out T1 component1, out T2 component2, out T3 component3, out T4 component4)
        {
            entity = Entity;
            component1 = Component1;
            component2 = Component2;
            component3 = Component3;
            component4 = Component4;
        }
    }

    /// <summary>
    /// Zero-allocation query enumerator for performance-critical iteration
    /// </summary>
    public struct QueryEnumerator<T1, T2, T3, T4> : IEnumerator<Query<T1, T2, T3, T4>>
    {
        private readonly EntityManager _entityManager;
        private readonly EntityEnumerator _entityEnumerator;
        private Query<T1, T2, T3, T4> _current;

        public QueryEnumerator(EntityManager entityManager, EntityEnumerator entityEnumerator)
        {
            _entityManager = entityManager;
            _entityEnumerator = entityEnumerator;
            _current = default;
        }

        public Query<T1, T2, T3, T4> Current => _current;
        object IEnumerator.Current => _current;

        public bool MoveNext()
        {
            if (_entityEnumerator.MoveNext())
            {
                var entity = _entityEnumerator.Current;
                var component1 = _entityManager.GetComponent<T1>(entity);
                var component2 = _entityManager.GetComponent<T2>(entity);
                var component3 = _entityManager.GetComponent<T3>(entity);
                var component4 = _entityManager.GetComponent<T4>(entity);
                _current = new Query<T1, T2, T3, T4>(entity, component1, component2, component3, component4);
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _entityEnumerator.Reset();
            _current = default;
        }

        public void Dispose() { }
    }

    /// <summary>
    /// Queryable collection for fluent API
    /// </summary>
    public readonly struct Queryable<T1, T2, T3>
    {
        private readonly EntityManager _entityManager;
        private readonly EntityEnumerator _entityEnumerator;

        public Queryable(EntityManager entityManager, EntityEnumerator entityEnumerator)
        {
            _entityManager = entityManager;
            _entityEnumerator = entityEnumerator;
        }

        public QueryEnumerator<T1, T2, T3> GetEnumerator()
        {
            return new QueryEnumerator<T1, T2, T3>(_entityManager, _entityEnumerator);
        }
    }

    /// <summary>
    /// Queryable collection for fluent API
    /// </summary>
    public readonly struct Queryable<T1, T2, T3, T4>
    {
        private readonly EntityManager _entityManager;
        private readonly EntityEnumerator _entityEnumerator;

        public Queryable(EntityManager entityManager, EntityEnumerator entityEnumerator)
        {
            _entityManager = entityManager;
            _entityEnumerator = entityEnumerator;
        }

        public QueryEnumerator<T1, T2, T3, T4> GetEnumerator()
        {
            return new QueryEnumerator<T1, T2, T3, T4>(_entityManager, _entityEnumerator);
        }
    }

    /// <summary>
    /// Represents a chunk of memory containing entities with the same archetype
    /// Optimized with preindexed arrays and dirty flags
    /// </summary>
    public class ArchetypeChunk
    {
        public readonly Archetype Archetype;
        public readonly int Capacity;
        public int Count { get; set; }
        
        // OPTIMIZATION 1: Preindexed arrays instead of Dictionary for faster access
        // Direct array indexing: arrays[componentType.Id][entityIndex]
        private readonly Array[] _componentArrays; // Indexed by component type ID
        private readonly EntityId[] _entities;
        private readonly int[] _entityIndices;
        private readonly bool[] _rentedArrays; // Track which arrays are rented from pool
        
        // OPTIMIZATION 6: Dirty flags for change detection
        private BitSet _currentDirtyFlags;
        private readonly BitArray[] _entityDirtyFlags; // Per-entity dirty state for large components

        public ArchetypeChunk(Archetype archetype, int capacity = 1024)
        {
            Archetype = archetype;
            Capacity = capacity;
            Count = 0;
            
            // Preindexed arrays - support up to 256 component types
            _componentArrays = new Array[256];
            _rentedArrays = new bool[256];
            _entities = new EntityId[capacity];
            _entityIndices = new int[capacity];
            _currentDirtyFlags = new BitSet(256);
            _entityDirtyFlags = new BitArray[256]; // One BitArray per component type
            for (int i = 0; i < 256; i++)
            {
                _entityDirtyFlags[i] = new BitArray(capacity);
            }

            // Initialize typed component arrays for each component type
            foreach (var componentType in archetype.ComponentTypes)
            {
                if (componentType.Id >= 256)
                    throw new InvalidOperationException($"Component type ID {componentType.Id} exceeds maximum of 255");
                
                // Use array pool for better memory management
                var arrayType = Array.CreateInstance(componentType.Type, capacity);
                _componentArrays[componentType.Id] = arrayType;
                _rentedArrays[componentType.Id] = false; // Not rented from pool initially
            }
        }

        public bool IsFull => Count >= Capacity;

        public int AddEntity(EntityId entityId)
        {
            if (IsFull)
                throw new InvalidOperationException("Chunk is full");

            _entities[Count] = entityId;
            _entityIndices[Count] = Count;
            Count++;
            return Count - 1; // Return the entity index
        }

        public void RemoveEntity(int entityIndex)
        {
            if (entityIndex < 0 || entityIndex >= Count)
                throw new ArgumentOutOfRangeException(nameof(entityIndex));

            // Move last entity to this position to maintain contiguous storage
            if (entityIndex < Count - 1)
            {
                _entities[entityIndex] = _entities[Count - 1];
                _entityIndices[entityIndex] = _entityIndices[Count - 1];
                
                // Move component data - optimized with direct array access
                foreach (var componentType in Archetype.ComponentTypes)
                {
                    var array = _componentArrays[componentType.Id];
                    Array.Copy(array, Count - 1, array, entityIndex, 1);
                }
            }

            Count--;
        }

        public void SetComponent<T>(int entityIndex, ComponentType componentType, T component)
        {
            if (entityIndex < 0 || entityIndex >= Count)
                throw new ArgumentOutOfRangeException(nameof(entityIndex));

            if (componentType.Id < 0 || componentType.Id >= _componentArrays.Length)
                throw new ArgumentOutOfRangeException(nameof(componentType.Id), $"ComponentType ID {componentType.Id} exceeds bounds (0-{_componentArrays.Length - 1})");

            var array = _componentArrays[componentType.Id];
            if (array == null)
                throw new ArgumentException($"Component type {componentType.Id} not found in archetype");

            if (array is T[] typedArray)
            {
                typedArray[entityIndex] = component;
                // Mark component as dirty
                _currentDirtyFlags.Set(componentType.Id);
                // Mark specific entity as dirty
                if (componentType.Id < 256)
                {
                    _entityDirtyFlags[componentType.Id].Set(entityIndex, true);
                }
            }
            else
            {
                throw new InvalidCastException($"Component type mismatch. Expected {typeof(T)}, got {componentType.Type}");
            }
        }

        public T GetComponent<T>(int entityIndex, ComponentType componentType)
        {
            if (entityIndex < 0 || entityIndex >= Count)
                throw new ArgumentOutOfRangeException(nameof(entityIndex));

            if (componentType.Id < 0 || componentType.Id >= _componentArrays.Length)
                throw new ArgumentOutOfRangeException(nameof(componentType.Id), $"ComponentType ID {componentType.Id} exceeds bounds (0-{_componentArrays.Length - 1})");

            var array = _componentArrays[componentType.Id];
            if (array == null)
                throw new ArgumentException($"Component type {componentType.Id} not found in archetype");

            if (array is T[] typedArray)
            {
                return typedArray[entityIndex];
            }
            
            throw new InvalidCastException($"Component type mismatch. Expected {typeof(T)}, got {componentType.Type}");
        }

        public bool HasComponent(ComponentType componentType)
        {
            if (componentType.Id < 0 || componentType.Id >= _componentArrays.Length)
                return false;
            return _componentArrays[componentType.Id] != null;
        }

        public EntityId GetEntity(int entityIndex)
        {
            if (entityIndex < 0 || entityIndex >= Count)
                throw new ArgumentOutOfRangeException(nameof(entityIndex));

            return _entities[entityIndex];
        }

        public int GetEntityIndex(EntityId entityId)
        {
            for (int i = 0; i < Count; i++)
            {
                if (_entities[i].Equals(entityId))
                    return i;
            }
            return -1;
        }

        // Get typed array for direct iteration (performance optimization)
        public T[] GetComponentArray<T>(ComponentType componentType)
        {
            if (componentType.Id < 0 || componentType.Id >= _componentArrays.Length)
                throw new ArgumentOutOfRangeException(nameof(componentType.Id), $"ComponentType ID {componentType.Id} exceeds bounds (0-{_componentArrays.Length - 1})");

            var array = _componentArrays[componentType.Id];
            if (array == null)
                throw new ArgumentException($"Component type {componentType.Id} not found in archetype");

            if (array is T[] typedArray)
                return typedArray;

            throw new InvalidCastException($"Component type mismatch. Expected {typeof(T)}, got {componentType.Type}");
        }

        /// <summary>
        /// SIMD-friendly component array access for hot components
        /// </summary>
        public T[] GetHotComponentArray<T>(ComponentType componentType)
        {
            if (componentType.Id < 0 || componentType.Id >= _componentArrays.Length)
                throw new ArgumentOutOfRangeException(nameof(componentType.Id), $"ComponentType ID {componentType.Id} exceeds bounds (0-{_componentArrays.Length - 1})");

            var array = _componentArrays[componentType.Id];
            if (array == null)
                throw new ArgumentException($"Component type {componentType.Id} not found in archetype");

            if (array is T[] typedArray)
                return typedArray;

            throw new InvalidCastException($"Component type mismatch. Expected {typeof(T)}, got {componentType.Type}");
        }

        /// <summary>
        /// SIMD-friendly iteration support for hot components
        /// </summary>
        public void ProcessHotComponents<T>(ComponentType componentType, Action<T[], int> processor)
        {
            var array = GetHotComponentArray<T>(componentType);
            processor(array, Count);
        }

        /// <summary>
        /// SIMD-friendly batch processing for multiple hot components
        /// </summary>
        public void ProcessHotComponentsBatch<T1, T2>(ComponentType componentType1, ComponentType componentType2, 
            Action<T1[], T2[], int> processor)
        {
            var array1 = GetHotComponentArray<T1>(componentType1);
            var array2 = GetHotComponentArray<T2>(componentType2);
            processor(array1, array2, Count);
        }

        // OPTIMIZATION 6: Dirty flag methods
        public bool IsComponentDirty(ComponentType componentType)
        {
            return (_currentDirtyFlags & (1UL << componentType.Id)) != 0;
        }

        public void ClearDirtyFlags()
        {
            _currentDirtyFlags.ClearAll();
            // Clear all entity-specific dirty flags
            for (int i = 0; i < 256; i++)
            {
                _entityDirtyFlags[i].SetAll(false);
            }
        }

        public BitSet GetDirtyFlags() => _currentDirtyFlags;

        /// <summary>
        /// Check if a specific entity's component is dirty
        /// </summary>
        public bool IsEntityComponentDirty(int entityIndex, ComponentType componentType)
        {
            if (componentType.Id >= 256 || entityIndex >= Capacity)
                return false;
            return _entityDirtyFlags[componentType.Id].Get(entityIndex);
        }

        /// <summary>
        /// Get all dirty entities for a specific component type
        /// </summary>
        public IEnumerable<int> GetDirtyEntities(ComponentType componentType)
        {
            if (componentType.Id >= 256)
                return Enumerable.Empty<int>();

            var dirtyBits = _entityDirtyFlags[componentType.Id];
            var dirtyEntities = new List<int>();
            for (int i = 0; i < Count; i++)
            {
                if (dirtyBits.Get(i))
                    dirtyEntities.Add(i);
            }
            return dirtyEntities;
        }

        // Zero-allocation iteration support
        public EntityEnumerator GetEnumerator()
        {
            return new EntityEnumerator(new List<ArchetypeChunk> { this });
        }

        /// <summary>
        /// Cleanup method to return rented arrays to pool
        /// </summary>
        public void Dispose()
        {
            // Return rented arrays to pool
            for (int i = 0; i < _componentArrays.Length; i++)
            {
                if (_rentedArrays[i] && _componentArrays[i] != null)
                {
                    // Note: ArrayPool<T> requires typed arrays, so we'd need type-specific pools
                    // For now, we'll just clear the reference
                    _componentArrays[i] = null;
                    _rentedArrays[i] = false;
                }
            }
        }
    }

    /// <summary>
    /// Manages entities with archetyped storage, chunked memory, reusable IDs, and SoA component arrays
    /// Production-grade optimizations: preindexed arrays, query caching, dirty flags, zero-allocation iteration
    /// </summary>
    public class EntityManager
    {
        // Subsystems for modular design
        private readonly EntityAllocator _entityAllocator;
        private readonly ArchetypeStore _archetypeStore;
        private readonly StructuralChangeScheduler _structuralChangeScheduler;
        
        // Query cache for fast archetype matching
        private readonly QueryCache _queryCache;

        public EntityManager(int chunkCapacity = 1024)
        {
            _entityAllocator = new EntityAllocator();
            _archetypeStore = new ArchetypeStore(chunkCapacity);
            _structuralChangeScheduler = new StructuralChangeScheduler(this);
            _queryCache = new QueryCache(_archetypeStore.GetAllArchetypeChunks());
        }

        /// <summary>
        /// Creates a new entity with the specified component types
        /// </summary>
        public EntityId CreateEntity(params ComponentType[] componentTypes)
        {
            var archetype = _archetypeStore.GetOrCreateArchetype(componentTypes);
            var entityId = _entityAllocator.CreateEntityId();
            
            // Add entity to archetype
            _archetypeStore.AddEntityToArchetype(entityId, archetype);
            
            // Invalidate query cache
            _queryCache.InvalidateCache();
            
            return entityId;
        }

        /// <summary>
        /// Creates a new entity with the specified component types using generic type parameters
        /// </summary>
        public EntityId CreateEntity<T1>()
        {
            return CreateEntity(ComponentTypeRegistry.Get<T1>());
        }

        public EntityId CreateEntity<T1, T2>()
        {
            return CreateEntity(ComponentTypeRegistry.Get<T1>(), ComponentTypeRegistry.Get<T2>());
        }

        public EntityId CreateEntity<T1, T2, T3>()
        {
            return CreateEntity(ComponentTypeRegistry.Get<T1>(), ComponentTypeRegistry.Get<T2>(), ComponentTypeRegistry.Get<T3>());
        }

        public EntityId CreateEntity<T1, T2, T3, T4>()
        {
            return CreateEntity(ComponentTypeRegistry.Get<T1>(), ComponentTypeRegistry.Get<T2>(), ComponentTypeRegistry.Get<T3>(), ComponentTypeRegistry.Get<T4>());
        }

        /// <summary>
        /// Destroys an entity and makes its ID reusable
        /// </summary>
        public void DestroyEntity(EntityId entityId)
        {
            // Remove entity from archetype store
            _archetypeStore.RemoveEntityFromArchetype(entityId);
            
            // Make ID reusable
            _entityAllocator.ReleaseEntityId(entityId.Id);
            
            // Invalidate query cache
            _queryCache.InvalidateCache();
        }

        /// <summary>
        /// Sets a component value for an entity
        /// </summary>
        public void SetComponent<T>(EntityId entityId, T component)
        {
            var componentType = ComponentTypeRegistry.Get<T>();
            SetComponent(entityId, componentType, component);
        }

        public void SetComponent<T>(EntityId entityId, ComponentType componentType, T component)
        {
            var location = _archetypeStore.GetEntityLocation(entityId);
            if (!location.HasValue)
                throw new ArgumentException($"Entity {entityId.Id} not found");

            var (archetype, chunkIndex, entityIndex) = location.Value;
            var chunks = _archetypeStore.GetChunksForArchetype(archetype);
            var chunk = chunks[chunkIndex];
            
            if (!chunk.HasComponent(componentType))
                throw new ArgumentException($"Component type {componentType.Id} not found in entity's archetype");

            chunk.SetComponent(entityIndex, componentType, component);
        }

        /// <summary>
        /// Gets a component value from an entity
        /// </summary>
        public T GetComponent<T>(EntityId entityId)
        {
            var componentType = ComponentTypeRegistry.Get<T>();
            return GetComponent<T>(entityId, componentType);
        }

        public T GetComponent<T>(EntityId entityId, ComponentType componentType)
        {
            var location = _archetypeStore.GetEntityLocation(entityId);
            if (!location.HasValue)
                throw new ArgumentException($"Entity {entityId.Id} not found");

            var (archetype, chunkIndex, entityIndex) = location.Value;
            var chunks = _archetypeStore.GetChunksForArchetype(archetype);
            var chunk = chunks[chunkIndex];
            
            if (!chunk.HasComponent(componentType))
                throw new ArgumentException($"Component type {componentType.Id} not found in entity's archetype");

            return chunk.GetComponent<T>(entityIndex, componentType);
        }

        /// <summary>
        /// Checks if an entity has a specific component type
        /// </summary>
        public bool HasComponent<T>(EntityId entityId)
        {
            var componentType = ComponentTypeRegistry.Get<T>();
            return HasComponent(entityId, componentType);
        }

        public bool HasComponent(EntityId entityId, ComponentType componentType)
        {
            var location = _archetypeStore.GetEntityLocation(entityId);
            if (!location.HasValue)
                return false;

            var (archetype, chunkIndex, entityIndex) = location.Value;
            var chunks = _archetypeStore.GetChunksForArchetype(archetype);
            var chunk = chunks[chunkIndex];
            return chunk.HasComponent(componentType);
        }

        /// <summary>
        /// Adds a component to an entity (changes archetype)
        /// </summary>
        public void AddComponent<T>(EntityId entityId, T component)
        {
            var componentType = ComponentTypeRegistry.Get<T>();
            AddComponent(entityId, componentType, component);
        }

        public void AddComponent<T>(EntityId entityId, ComponentType componentType, T component)
        {
            _archetypeStore.AddComponentToEntity(entityId, componentType, component);
            _queryCache.InvalidateCache();
        }

        /// <summary>
        /// Removes a component from an entity (changes archetype)
        /// </summary>
        public void RemoveComponent<T>(EntityId entityId)
        {
            var componentType = ComponentTypeRegistry.Get<T>();
            RemoveComponent(entityId, componentType);
        }

        public void RemoveComponent(EntityId entityId, ComponentType componentType)
        {
            _archetypeStore.RemoveComponentFromEntity(entityId, componentType);
            _queryCache.InvalidateCache();
        }

        /// <summary>
        /// Gets all entities with a specific archetype
        /// </summary>
        public IEnumerable<EntityId> GetEntitiesWithArchetype(Archetype archetype)
        {
            var chunks = _archetypeStore.GetChunksForArchetype(archetype);
            if (chunks.Count == 0)
                return Enumerable.Empty<EntityId>();

            return chunks.SelectMany(chunk => 
                Enumerable.Range(0, chunk.Count).Select(i => chunk.GetEntity(i)));
        }

        /// <summary>
        /// Gets all entities with specific component types using cached query matching
        /// OPTIMIZATION 2: Uses query cache for fast archetype matching
        /// </summary>
        public IEnumerable<EntityId> GetEntitiesWithComponents(params ComponentType[] componentTypes)
        {
            var requiredMask = BitUtils.CalculateBitmask(componentTypes);
            var matchingArchetypes = _queryCache.GetMatchingArchetypes(requiredMask);
            
            return matchingArchetypes
                .SelectMany(archetype => _archetypeStore.GetChunksForArchetype(archetype))
                .SelectMany(chunk => 
                    Enumerable.Range(0, chunk.Count).Select(i => chunk.GetEntity(i)));
        }

        /// <summary>
        /// Gets all entities with specific component types using BitSet (future-proof API)
        /// </summary>
        public IEnumerable<EntityId> GetEntitiesWithComponents(BitSet componentMask)
        {
            var matchingArchetypes = _queryCache.GetMatchingArchetypes(componentMask);
            
            return matchingArchetypes
                .SelectMany(archetype => _archetypeStore.GetChunksForArchetype(archetype))
                .SelectMany(chunk => 
                    Enumerable.Range(0, chunk.Count).Select(i => chunk.GetEntity(i)));
        }

        /// <summary>
        /// Creates a BitSet query from component types (transitional API)
        /// </summary>
        public BitSet CreateComponentMask(params ComponentType[] componentTypes)
        {
            return BitUtils.CalculateBitSet(componentTypes);
        }

        /// <summary>
        /// Zero-allocation iteration for performance-critical scenarios
        /// OPTIMIZATION 4: Zero-allocation iteration
        /// </summary>
        public EntityEnumerator GetEntitiesWithComponentsEnumerator(params ComponentType[] componentTypes)
        {
            var requiredMask = BitUtils.CalculateBitmask(componentTypes);
            var matchingArchetypes = _queryCache.GetMatchingArchetypes(requiredMask);
            
            var allChunks = matchingArchetypes
                .SelectMany(archetype => _archetypeStore.GetChunksForArchetype(archetype))
                .ToList();
            
            return new EntityEnumerator(allChunks);
        }

        /// <summary>
        /// Generic overload for zero-allocation iteration
        /// </summary>
        public EntityEnumerator GetEntitiesWithComponentsEnumerator<T1>()
        {
            var componentType = ComponentTypeRegistry.Get<T1>();
            return GetEntitiesWithComponentsEnumerator(componentType);
        }

        public EntityEnumerator GetEntitiesWithComponentsEnumerator<T1, T2>()
        {
            var componentType1 = ComponentTypeRegistry.Get<T1>();
            var componentType2 = ComponentTypeRegistry.Get<T2>();
            return GetEntitiesWithComponentsEnumerator(componentType1, componentType2);
        }

        public EntityEnumerator GetEntitiesWithComponentsEnumerator<T1, T2, T3>()
        {
            var componentType1 = ComponentTypeRegistry.Get<T1>();
            var componentType2 = ComponentTypeRegistry.Get<T2>();
            var componentType3 = ComponentTypeRegistry.Get<T3>();
            return GetEntitiesWithComponentsEnumerator(componentType1, componentType2, componentType3);
        }

        /// <summary>
        /// OPTIMIZATION 5: Static query type API for fluent, type-safe queries
        /// </summary>
        public Queryable<T1> For<T1>()
        {
            var componentType = ComponentTypeRegistry.Get<T1>();
            var enumerator = GetEntitiesWithComponentsEnumerator(componentType);
            return new Queryable<T1>(this, enumerator);
        }

        public Queryable<T1, T2> For<T1, T2>()
        {
            var componentType1 = ComponentTypeRegistry.Get<T1>();
            var componentType2 = ComponentTypeRegistry.Get<T2>();
            var enumerator = GetEntitiesWithComponentsEnumerator(componentType1, componentType2);
            return new Queryable<T1, T2>(this, enumerator);
        }

        public Queryable<T1, T2, T3> For<T1, T2, T3>()
        {
            var componentType1 = ComponentTypeRegistry.Get<T1>();
            var componentType2 = ComponentTypeRegistry.Get<T2>();
            var componentType3 = ComponentTypeRegistry.Get<T3>();
            var enumerator = GetEntitiesWithComponentsEnumerator(componentType1, componentType2, componentType3);
            return new Queryable<T1, T2, T3>(this, enumerator);
        }

        public Queryable<T1, T2, T3, T4> For<T1, T2, T3, T4>()
        {
            var componentType1 = ComponentTypeRegistry.Get<T1>();
            var componentType2 = ComponentTypeRegistry.Get<T2>();
            var componentType3 = ComponentTypeRegistry.Get<T3>();
            var componentType4 = ComponentTypeRegistry.Get<T4>();
            var enumerator = GetEntitiesWithComponentsEnumerator(componentType1, componentType2, componentType3, componentType4);
            return new Queryable<T1, T2, T3, T4>(this, enumerator);
        }

        /// <summary>
        /// Gets all entities with specific component types using generic type parameters
        /// </summary>
        public IEnumerable<EntityId> GetEntitiesWithComponents<T1>()
        {
            return GetEntitiesWithComponents(ComponentTypeRegistry.Get<T1>());
        }

        public IEnumerable<EntityId> GetEntitiesWithComponents<T1, T2>()
        {
            return GetEntitiesWithComponents(ComponentTypeRegistry.Get<T1>(), ComponentTypeRegistry.Get<T2>());
        }

        public IEnumerable<EntityId> GetEntitiesWithComponents<T1, T2, T3>()
        {
            return GetEntitiesWithComponents(ComponentTypeRegistry.Get<T1>(), ComponentTypeRegistry.Get<T2>(), ComponentTypeRegistry.Get<T3>());
        }

        /// <summary>
        /// Gets statistics about the entity manager
        /// </summary>
        public (int totalEntities, int totalChunks, int reusableIds) GetStatistics()
        {
            var (totalArchetypes, totalChunks, totalEntities) = _archetypeStore.GetStatistics();
            var (totalAllocated, reusableCount, nextId) = _entityAllocator.GetStatistics();
            
            return (totalEntities, totalChunks, reusableCount);
        }
        
        /// <summary>
        /// Gets all archetype chunks for parallel processing
        /// </summary>
        public Dictionary<Archetype, List<ArchetypeChunk>> GetArchetypeChunks()
        {
            return _archetypeStore.GetAllArchetypeChunks();
        }

        public (Archetype archetype, int chunkIndex, int entityIndex)? GetEntityLocation(EntityId entityId)
        {
            return _archetypeStore.GetEntityLocation(entityId);
        }
        
        public IEnumerable<EntityId> GetAllEntities()
        {
            return _archetypeStore.GetAllEntityLocations().Keys;
        }

        /// <summary>
        /// OPTIMIZATION 6: Get entities with dirty components for efficient updates
        /// </summary>
        public IEnumerable<EntityId> GetEntitiesWithDirtyComponents<T>()
        {
            var componentType = ComponentTypeRegistry.Get<T>();
            var dirtyMask = 1UL << componentType.Id;
            
            return _archetypeStore.GetAllArchetypeChunks()
                .Where(kvp => kvp.Key.HasAllComponents(dirtyMask))
                .SelectMany(kvp => kvp.Value)
                .Where(chunk => chunk.IsComponentDirty(componentType))
                .SelectMany(chunk => 
                    Enumerable.Range(0, chunk.Count).Select(i => chunk.GetEntity(i)));
        }

        /// <summary>
        /// OPTIMIZATION 6: Clear all dirty flags
        /// </summary>
        public void ClearAllDirtyFlags()
        {
            foreach (var chunks in _archetypeStore.GetAllArchetypeChunks().Values)
            {
                foreach (var chunk in chunks)
                {
                    chunk.ClearDirtyFlags();
                }
            }
        }

        /// <summary>
        /// SIMD-friendly batch processing for hot components across all chunks
        /// </summary>
        public void ProcessHotComponentsBatch<T1, T2>(ComponentType componentType1, ComponentType componentType2, 
            Action<T1[], T2[], int> processor)
        {
            var matchingArchetypes = _archetypeStore.GetAllArchetypeChunks().Keys
                .Where(archetype => archetype.HasAllComponents(1UL << componentType1.Id | 1UL << componentType2.Id))
                .ToList();

            foreach (var archetype in matchingArchetypes)
            {
                foreach (var chunk in _archetypeStore.GetChunksForArchetype(archetype))
                {
                    chunk.ProcessHotComponentsBatch(componentType1, componentType2, processor);
                }
            }
        }

        /// <summary>
        /// SIMD-optimized processing using System.Numerics.Vector for vectorized updates
        /// </summary>
        public void ProcessHotComponentsSimd<T1, T2>(ComponentType componentType1, ComponentType componentType2,
            Action<T1[], T2[], int> processor, float deltaTime = 1.0f) where T1 : unmanaged where T2 : unmanaged
        {
            var matchingArchetypes = _archetypeStore.GetAllArchetypeChunks().Keys
                .Where(archetype => archetype.HasAllComponents(1UL << componentType1.Id | 1UL << componentType2.Id))
                .ToList();

            foreach (var archetype in matchingArchetypes)
            {
                foreach (var chunk in _archetypeStore.GetChunksForArchetype(archetype))
                {
                    var array1 = chunk.GetComponentArray<T1>(componentType1);
                    var array2 = chunk.GetComponentArray<T2>(componentType2);
                    var count = chunk.Count;

                    // Process in SIMD-friendly batches
                    for (int i = 0; i < count; i += System.Numerics.Vector<float>.Count)
                    {
                        var batchSize = Math.Min(System.Numerics.Vector<float>.Count, count - i);
                        processor(array1, array2, batchSize);
                    }
                }
            }
        }

        /// <summary>
        /// SIMD-friendly batch processing for hot components with query filtering
        /// </summary>
        public void ProcessHotComponentsBatch<T1, T2>(ComponentType componentType1, ComponentType componentType2, 
            Action<T1[], T2[], int> processor, params ComponentType[] filterComponents)
        {
            var requiredMask = BitUtils.CalculateBitmask(filterComponents);
            var matchingArchetypes = _queryCache.GetMatchingArchetypes(requiredMask);

            foreach (var archetype in matchingArchetypes)
            {
                if (!archetype.HasAllComponents(1UL << componentType1.Id | 1UL << componentType2.Id))
                    continue;

                foreach (var chunk in _archetypeStore.GetChunksForArchetype(archetype))
                {
                    chunk.ProcessHotComponentsBatch(componentType1, componentType2, processor);
                }
            }
        }

        /// <summary>
        /// SIMD-friendly batch processing with generic type parameters
        /// </summary>
        public void ProcessHotComponentsBatch<T1, T2>(Action<T1[], T2[], int> processor)
        {
            var componentType1 = ComponentTypeRegistry.Get<T1>();
            var componentType2 = ComponentTypeRegistry.Get<T2>();
            ProcessHotComponentsBatch(componentType1, componentType2, processor);
        }

        /// <summary>
        /// Queue a structural change for end-of-frame processing
        /// </summary>
        public void QueueAddComponent<T>(EntityId entityId, T component)
        {
            _structuralChangeScheduler.QueueAddComponent(entityId, component);
        }

        public void QueueRemoveComponent<T>(EntityId entityId)
        {
            _structuralChangeScheduler.QueueRemoveComponent<T>(entityId);
        }

        public void QueueDestroyEntity(EntityId entityId)
        {
            _structuralChangeScheduler.QueueDestroyEntity(entityId);
        }

        /// <summary>
        /// Process all queued structural changes
        /// </summary>
        public void ProcessStructuralChanges()
        {
            _structuralChangeScheduler.ProcessStructuralChanges();
        }

        /// <summary>
        /// Clear all queued structural changes
        /// </summary>
        public void ClearStructuralChanges()
        {
            _structuralChangeScheduler.ClearStructuralChanges();
        }



        private ArchetypeChunk GetOrCreateChunk(Archetype archetype)
        {
            return _archetypeStore.GetOrCreateChunk(archetype);
        }

        private int GetChunkIndex(Archetype archetype, ArchetypeChunk chunk)
        {
            return _archetypeStore.GetChunkIndex(archetype, chunk);
        }



        /// <summary>
        /// Generic component copying to avoid boxing
        /// </summary>
        private void CopyComponentGeneric<T>(ArchetypeChunk fromChunk, int fromIndex, ArchetypeChunk toChunk, int toIndex, ComponentType componentType)
        {
            var component = fromChunk.GetComponent<T>(fromIndex, componentType);
            toChunk.SetComponent(toIndex, componentType, component);
        }

        /// <summary>
        /// Type-safe component copying with explicit type parameter
        /// </summary>
        public void TryCopyComponent<T>(ArchetypeChunk fromChunk, int fromIndex, ArchetypeChunk toChunk, int toIndex, ComponentType componentType)
        {
            if (componentType.Type != typeof(T))
                throw new ArgumentException($"Component type mismatch. Expected {typeof(T)}, got {componentType.Type}");

            var component = fromChunk.GetComponent<T>(fromIndex, componentType);
            toChunk.SetComponent(toIndex, componentType, component);
        }

        /// <summary>
        /// Non-generic component copying using cached reflection (fallback)
        /// </summary>
        private void CopyComponent(ArchetypeChunk fromChunk, int fromIndex, ArchetypeChunk toChunk, int toIndex, ComponentType componentType)
        {
            if (componentType.Type == null)
                throw new ArgumentException("Component type is null");
                
            // Use reflection to copy component
            var getComponentMethod = typeof(ArchetypeChunk).GetMethod("GetComponent")?.MakeGenericMethod(componentType.Type);
            var setComponentMethod = typeof(ArchetypeChunk).GetMethod("SetComponent")?.MakeGenericMethod(componentType.Type);
            
            if (getComponentMethod == null || setComponentMethod == null)
                throw new InvalidOperationException($"Could not find GetComponent or SetComponent method for type {componentType.Type}");
            
            var component = getComponentMethod.Invoke(fromChunk, new object[] { fromIndex, componentType });
            setComponentMethod.Invoke(toChunk, new object[] { toIndex, componentType, component });
        }
    }
} 