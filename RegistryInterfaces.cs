using System;
using System.Collections.Generic;

namespace ECS
{
    /// <summary>
    /// Interface for entity management operations
    /// </summary>
    public interface IEntityRegistry
    {
        /// <summary>
        /// Creates a new entity with the specified component types
        /// </summary>
        EntityId CreateEntity(params ComponentType[] componentTypes);

        /// <summary>
        /// Creates a new entity with a single component type
        /// </summary>
        EntityId CreateEntity<T1>();

        /// <summary>
        /// Creates a new entity with two component types
        /// </summary>
        EntityId CreateEntity<T1, T2>();

        /// <summary>
        /// Creates a new entity with three component types
        /// </summary>
        EntityId CreateEntity<T1, T2, T3>();

        /// <summary>
        /// Creates a new entity with four component types
        /// </summary>
        EntityId CreateEntity<T1, T2, T3, T4>();

        /// <summary>
        /// Destroys an entity and makes its ID reusable
        /// </summary>
        void DestroyEntity(EntityId entityId);

        /// <summary>
        /// Checks if an entity exists
        /// </summary>
        bool EntityExists(EntityId entityId);

        /// <summary>
        /// Gets the total number of entities
        /// </summary>
        int GetEntityCount();

        /// <summary>
        /// Gets all entity IDs
        /// </summary>
        IEnumerable<EntityId> GetAllEntities();

        /// <summary>
        /// Gets entity location information
        /// </summary>
        (Archetype archetype, int chunkIndex, int entityIndex)? GetEntityLocation(EntityId entityId);
    }

    /// <summary>
    /// Interface for archetype management operations
    /// </summary>
    public interface IArchetypeRegistry
    {
        /// <summary>
        /// Creates or retrieves an archetype for the given component types
        /// </summary>
        Archetype GetOrCreateArchetype(params ComponentType[] componentTypes);

        /// <summary>
        /// Gets all archetypes that match the given BitSet component mask
        /// </summary>
        IEnumerable<Archetype> GetMatchingArchetypes(BitSet componentMask);

        /// <summary>
        /// Gets all registered archetypes
        /// </summary>
        IEnumerable<Archetype> GetAllArchetypes();

        /// <summary>
        /// Gets the total number of archetypes
        /// </summary>
        int GetArchetypeCount();

        /// <summary>
        /// Checks if an archetype exists
        /// </summary>
        bool ArchetypeExists(Archetype archetype);

        /// <summary>
        /// Invalidates the archetype cache
        /// </summary>
        void InvalidateCache();
    }

    /// <summary>
    /// Interface for chunk management operations
    /// </summary>
    public interface IChunkRegistry
    {
        /// <summary>
        /// Gets or creates a chunk for the given archetype
        /// </summary>
        ArchetypeChunk GetOrCreateChunk(Archetype archetype);

        /// <summary>
        /// Gets all chunks for a specific archetype
        /// </summary>
        IEnumerable<ArchetypeChunk> GetChunks(Archetype archetype);

        /// <summary>
        /// Gets all chunks that match the given BitSet component mask
        /// </summary>
        IEnumerable<ArchetypeChunk> GetMatchingChunks(BitSet componentMask);

        /// <summary>
        /// Returns a chunk to the pool for reuse
        /// </summary>
        void ReturnChunk(ArchetypeChunk chunk);

        /// <summary>
        /// Gets the total number of chunks
        /// </summary>
        int GetChunkCount();

        /// <summary>
        /// Gets the total number of chunks for a specific archetype
        /// </summary>
        int GetChunkCount(Archetype archetype);

        /// <summary>
        /// Clears all chunks and returns them to the pool
        /// </summary>
        void ClearAllChunks();

        /// <summary>
        /// Gets chunk statistics
        /// </summary>
        (int totalChunks, int totalEntities, int reusableChunks) GetStatistics();
    }

    /// <summary>
    /// Combined interface for all registry operations
    /// </summary>
    public interface IEntityManagerRegistry : IEntityRegistry, IArchetypeRegistry, IChunkRegistry
    {
        /// <summary>
        /// Gets the chunk capacity used for new chunks
        /// </summary>
        int ChunkCapacity { get; }

        /// <summary>
        /// Sets a component value for an entity
        /// </summary>
        void SetComponent<T>(EntityId entityId, T component);

        /// <summary>
        /// Sets a component value for an entity with explicit component type
        /// </summary>
        void SetComponent<T>(EntityId entityId, ComponentType componentType, T component);

        /// <summary>
        /// Gets a component value from an entity
        /// </summary>
        T GetComponent<T>(EntityId entityId);

        /// <summary>
        /// Gets a component value from an entity with explicit component type
        /// </summary>
        T GetComponent<T>(EntityId entityId, ComponentType componentType);

        /// <summary>
        /// Checks if an entity has a specific component type
        /// </summary>
        bool HasComponent<T>(EntityId entityId);

        /// <summary>
        /// Checks if an entity has a specific component type
        /// </summary>
        bool HasComponent(EntityId entityId, ComponentType componentType);

        /// <summary>
        /// Adds a component to an entity (changes archetype)
        /// </summary>
        void AddComponent<T>(EntityId entityId, T component);

        /// <summary>
        /// Adds a component to an entity with explicit component type (changes archetype)
        /// </summary>
        void AddComponent<T>(EntityId entityId, ComponentType componentType, T component);

        /// <summary>
        /// Removes a component from an entity (changes archetype)
        /// </summary>
        void RemoveComponent<T>(EntityId entityId);

        /// <summary>
        /// Removes a component from an entity with explicit component type (changes archetype)
        /// </summary>
        void RemoveComponent(EntityId entityId, ComponentType componentType);

        /// <summary>
        /// Gets all entities with specific component types
        /// </summary>
        IEnumerable<EntityId> GetEntitiesWithComponents(params ComponentType[] componentTypes);

        /// <summary>
        /// Gets all entities with specific component types using BitSet
        /// </summary>
        IEnumerable<EntityId> GetEntitiesWithComponents(BitSet componentMask);

        /// <summary>
        /// Gets all entities with specific component types using generic type parameters
        /// </summary>
        IEnumerable<EntityId> GetEntitiesWithComponents<T1>();

        /// <summary>
        /// Gets all entities with specific component types using generic type parameters
        /// </summary>
        IEnumerable<EntityId> GetEntitiesWithComponents<T1, T2>();

        /// <summary>
        /// Gets all entities with specific component types using generic type parameters
        /// </summary>
        IEnumerable<EntityId> GetEntitiesWithComponents<T1, T2, T3>();

        /// <summary>
        /// Creates a BitSet query from component types
        /// </summary>
        BitSet CreateComponentMask(params ComponentType[] componentTypes);

        /// <summary>
        /// Gets zero-allocation enumerator for performance-critical iteration
        /// </summary>
        EntityEnumerator GetEntitiesWithComponentsEnumerator(params ComponentType[] componentTypes);

        /// <summary>
        /// Gets zero-allocation enumerator for performance-critical iteration with generic types
        /// </summary>
        EntityEnumerator GetEntitiesWithComponentsEnumerator<T1>();

        /// <summary>
        /// Gets zero-allocation enumerator for performance-critical iteration with generic types
        /// </summary>
        EntityEnumerator GetEntitiesWithComponentsEnumerator<T1, T2>();

        /// <summary>
        /// Gets zero-allocation enumerator for performance-critical iteration with generic types
        /// </summary>
        EntityEnumerator GetEntitiesWithComponentsEnumerator<T1, T2, T3>();

        /// <summary>
        /// Gets type-safe queryable for fluent API
        /// </summary>
        Queryable<T1> For<T1>();

        /// <summary>
        /// Gets type-safe queryable for fluent API
        /// </summary>
        Queryable<T1, T2> For<T1, T2>();

        /// <summary>
        /// Gets type-safe queryable for fluent API
        /// </summary>
        Queryable<T1, T2, T3> For<T1, T2, T3>();

        /// <summary>
        /// Gets type-safe queryable for fluent API
        /// </summary>
        Queryable<T1, T2, T3, T4> For<T1, T2, T3, T4>();

        /// <summary>
        /// Gets entities with dirty components for efficient updates
        /// </summary>
        IEnumerable<EntityId> GetEntitiesWithDirtyComponents<T>();

        /// <summary>
        /// Clears all dirty flags
        /// </summary>
        void ClearAllDirtyFlags();

        /// <summary>
        /// Processes structural changes
        /// </summary>
        void ProcessStructuralChanges();

        /// <summary>
        /// Clears all queued structural changes
        /// </summary>
        void ClearStructuralChanges();

        /// <summary>
        /// Queues an add component operation
        /// </summary>
        void QueueAddComponent<T>(EntityId entityId, T component);

        /// <summary>
        /// Queues a remove component operation
        /// </summary>
        void QueueRemoveComponent<T>(EntityId entityId);

        /// <summary>
        /// Queues a destroy entity operation
        /// </summary>
        void QueueDestroyEntity(EntityId entityId);
    }
} 