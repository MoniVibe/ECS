using System;
using System.Collections.Generic;
using System.Linq;

namespace ECS
{
    /// <summary>
    /// Manages archetype storage, lookup, and chunk associations
    /// Single responsibility: Archetype and chunk management
    /// </summary>
    public class ArchetypeStore
    {
        private readonly Dictionary<Archetype, List<ArchetypeChunk>> _archetypeChunks;
        private readonly Dictionary<EntityId, (Archetype archetype, int chunkIndex, int entityIndex)> _entityLocations;
        private readonly ChunkPool _chunkPool;
        private readonly int _chunkCapacity;

        public ArchetypeStore(int chunkCapacity = 1024)
        {
            _archetypeChunks = new Dictionary<Archetype, List<ArchetypeChunk>>();
            _entityLocations = new Dictionary<EntityId, (Archetype archetype, int chunkIndex, int entityIndex)>();
            _chunkPool = new ChunkPool();
            _chunkCapacity = chunkCapacity;
        }

        /// <summary>
        /// Gets or creates an archetype with the specified component types
        /// </summary>
        public Archetype GetOrCreateArchetype(params ComponentType[] componentTypes)
        {
            return new Archetype(componentTypes);
        }

        /// <summary>
        /// Gets or creates a chunk for the specified archetype
        /// </summary>
        public ArchetypeChunk GetOrCreateChunk(Archetype archetype)
        {
            if (!_archetypeChunks.TryGetValue(archetype, out var chunks))
            {
                chunks = new List<ArchetypeChunk>();
                _archetypeChunks[archetype] = chunks;
            }

            // Find a non-full chunk
            foreach (var chunk in chunks)
            {
                if (!chunk.IsFull)
                    return chunk;
            }

            // Create new chunk using pool
            var newChunk = _chunkPool.GetChunk(archetype, _chunkCapacity);
            chunks.Add(newChunk);
            return newChunk;
        }

        /// <summary>
        /// Adds an entity to an archetype chunk
        /// </summary>
        public int AddEntityToArchetype(EntityId entityId, Archetype archetype)
        {
            var chunk = GetOrCreateChunk(archetype);
            var entityIndex = chunk.AddEntity(entityId);
            
            // Store entity location
            var chunkIndex = GetChunkIndex(archetype, chunk);
            _entityLocations[entityId] = (archetype, chunkIndex, entityIndex);
            
            return entityIndex;
        }

        /// <summary>
        /// Removes an entity from its current archetype
        /// </summary>
        public void RemoveEntityFromArchetype(EntityId entityId)
        {
            if (!_entityLocations.TryGetValue(entityId, out var location))
                throw new ArgumentException($"Entity {entityId.Id} not found");

            var (archetype, chunkIndex, entityIndex) = location;
            var chunk = _archetypeChunks[archetype][chunkIndex];
            
            // Remove from chunk
            chunk.RemoveEntity(entityIndex);
            
            // Update entity locations for entities that moved
            if (entityIndex < chunk.Count)
            {
                var movedEntity = chunk.GetEntity(entityIndex);
                _entityLocations[movedEntity] = (archetype, chunkIndex, entityIndex);
            }
            
            // Remove from tracking
            _entityLocations.Remove(entityId);
        }

        /// <summary>
        /// Gets the location of an entity
        /// </summary>
        public (Archetype archetype, int chunkIndex, int entityIndex)? GetEntityLocation(EntityId entityId)
        {
            return _entityLocations.TryGetValue(entityId, out var location) ? location : null;
        }

        /// <summary>
        /// Updates entity location after movement
        /// </summary>
        public void UpdateEntityLocation(EntityId entityId, Archetype archetype, int chunkIndex, int entityIndex)
        {
            _entityLocations[entityId] = (archetype, chunkIndex, entityIndex);
        }

        /// <summary>
        /// Gets all chunks for an archetype
        /// </summary>
        public List<ArchetypeChunk> GetChunksForArchetype(Archetype archetype)
        {
            return _archetypeChunks.TryGetValue(archetype, out var chunks) ? chunks : new List<ArchetypeChunk>();
        }

        /// <summary>
        /// Gets all archetypes
        /// </summary>
        public IEnumerable<Archetype> GetAllArchetypes()
        {
            return _archetypeChunks.Keys;
        }

        /// <summary>
        /// Gets all entity locations
        /// </summary>
        public Dictionary<EntityId, (Archetype archetype, int chunkIndex, int entityIndex)> GetAllEntityLocations()
        {
            return new Dictionary<EntityId, (Archetype archetype, int chunkIndex, int entityIndex)>(_entityLocations);
        }

        /// <summary>
        /// Gets all archetype chunks for query cache
        /// </summary>
        public Dictionary<Archetype, List<ArchetypeChunk>> GetAllArchetypeChunks()
        {
            return new Dictionary<Archetype, List<ArchetypeChunk>>(_archetypeChunks);
        }

        /// <summary>
        /// Gets the chunk index for a specific chunk
        /// </summary>
        public int GetChunkIndex(Archetype archetype, ArchetypeChunk chunk)
        {
            return _archetypeChunks[archetype].IndexOf(chunk);
        }

        /// <summary>
        /// Gets statistics about archetype storage
        /// </summary>
        public (int totalArchetypes, int totalChunks, int totalEntities) GetStatistics()
        {
            var totalArchetypes = _archetypeChunks.Count;
            var totalChunks = _archetypeChunks.Values.Sum(chunks => chunks.Count);
            var totalEntities = _entityLocations.Count;
            
            return (totalArchetypes, totalChunks, totalEntities);
        }

        /// <summary>
        /// Invalidates all caches (for query cache invalidation)
        /// </summary>
        public void InvalidateCaches()
        {
            // This method is called when structural changes occur
            // It can be extended to invalidate other caches as needed
        }

        /// <summary>
        /// Adds a component to an entity by moving it to a new archetype
        /// </summary>
        public void AddComponentToEntity(EntityId entityId, ComponentType componentType, object component)
        {
            var location = GetEntityLocation(entityId);
            if (!location.HasValue)
                throw new ArgumentException($"Entity {entityId.Id} not found");

            var (currentArchetype, chunkIndex, entityIndex) = location.Value;
            
            // Check if component already exists
            if (currentArchetype.HasAllComponents(1UL << componentType.Id))
                throw new InvalidOperationException($"Entity already has component type {componentType.Id}");

            // Create new archetype with added component
            var newComponentTypes = currentArchetype.ComponentTypes.Concat(new[] { componentType }).ToArray();
            var newArchetype = new Archetype(newComponentTypes);

            // Move entity to new archetype
            MoveEntityToArchetype(entityId, currentArchetype, newArchetype, componentType, component);
        }

        /// <summary>
        /// Removes a component from an entity by moving it to a new archetype
        /// </summary>
        public void RemoveComponentFromEntity(EntityId entityId, ComponentType componentType)
        {
            var location = GetEntityLocation(entityId);
            if (!location.HasValue)
                throw new ArgumentException($"Entity {entityId.Id} not found");

            var (currentArchetype, chunkIndex, entityIndex) = location.Value;
            
            // Check if component exists
            if (!currentArchetype.HasAllComponents(1UL << componentType.Id))
                throw new InvalidOperationException($"Entity does not have component type {componentType.Id}");

            // Create new archetype without the component
            var newComponentTypes = currentArchetype.ComponentTypes.Where(ct => ct.Id != componentType.Id).ToArray();
            var newArchetype = new Archetype(newComponentTypes);

            // Move entity to new archetype
            MoveEntityToArchetype(entityId, currentArchetype, newArchetype);
        }

        /// <summary>
        /// Moves an entity from one archetype to another
        /// </summary>
        private void MoveEntityToArchetype(EntityId entityId, Archetype fromArchetype, Archetype toArchetype, ComponentType newComponentType = default, object? newComponent = null!)
        {
            var location = GetEntityLocation(entityId);
            if (!location.HasValue)
                throw new ArgumentException($"Entity {entityId.Id} not found");

            var (_, fromChunkIndex, fromEntityIndex) = location.Value;
            var fromChunks = GetChunksForArchetype(fromArchetype);
            var fromChunk = fromChunks[fromChunkIndex];

            // Get new chunk
            var toChunk = GetOrCreateChunk(toArchetype);
            var toEntityIndex = toChunk.AddEntity(entityId);

            // Copy shared components
            foreach (var componentType in fromArchetype.ComponentTypes)
            {
                if (toArchetype.HasAllComponents(1UL << componentType.Id))
                {
                    // Use reflection to copy component
                    var getComponentMethod = typeof(ArchetypeChunk).GetMethod("GetComponent")?.MakeGenericMethod(componentType.Type);
                    var setComponentMethod = typeof(ArchetypeChunk).GetMethod("SetComponent")?.MakeGenericMethod(componentType.Type);
                    
                    if (getComponentMethod != null && setComponentMethod != null)
                    {
                        var component = getComponentMethod.Invoke(fromChunk, new object[] { fromEntityIndex, componentType });
                        setComponentMethod.Invoke(toChunk, new object[] { toEntityIndex, componentType, component });
                    }
                }
            }

            // Set new component if provided
            if (newComponent != null && newComponentType.Type != null)
            {
                var setComponentMethod = typeof(ArchetypeChunk).GetMethod("SetComponent")?.MakeGenericMethod(newComponentType.Type);
                if (setComponentMethod != null)
                {
                    setComponentMethod.Invoke(toChunk, new object[] { toEntityIndex, newComponentType, newComponent });
                }
            }

            // Remove from old chunk
            fromChunk.RemoveEntity(fromEntityIndex);

            // Update entity locations for entities that moved in old chunk
            if (fromEntityIndex < fromChunk.Count)
            {
                var movedEntity = fromChunk.GetEntity(fromEntityIndex);
                UpdateEntityLocation(movedEntity, fromArchetype, fromChunkIndex, fromEntityIndex);
            }

            // Update entity location
            var toChunkIndex = GetChunkIndex(toArchetype, toChunk);
            UpdateEntityLocation(entityId, toArchetype, toChunkIndex, toEntityIndex);
        }

        /// <summary>
        /// Clears all archetype data (for testing/reset purposes)
        /// </summary>
        public void Clear()
        {
            _archetypeChunks.Clear();
            _entityLocations.Clear();
            _chunkPool.Clear();
        }
    }
} 