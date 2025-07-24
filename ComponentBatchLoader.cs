using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ECS
{
    /// <summary>
    /// Handles loading and processing of component batches for parallel operations
    /// </summary>
    public static class ComponentBatchLoader
    {
        /// <summary>
        /// Loads component pairs for a range of entities
        /// </summary>
        /// <typeparam name="T1">First component type</typeparam>
        /// <typeparam name="T2">Second component type</typeparam>
        /// <param name="manager">Entity manager instance</param>
        /// <param name="entities">List of entities to process</param>
        /// <param name="range">Range of entities to process</param>
        /// <param name="processor">Action to process the loaded components</param>
        public static void LoadAndProcessComponentPairs<T1, T2>(EntityManager manager, 
            List<EntityId> entities, (int start, int end) range, Action<T1, T2> processor)
        {
            ValidateInputs(manager, entities, range, processor);
            
            for (int i = range.start; i < range.end; i++)
            {
                var entity = entities[i];
                var comp1 = manager.GetComponent<T1>(entity);
                var comp2 = manager.GetComponent<T2>(entity);
                processor(comp1, comp2);
            }
        }
        
        /// <summary>
        /// Loads component pairs with entity context for a range of entities
        /// </summary>
        /// <typeparam name="T1">First component type</typeparam>
        /// <typeparam name="T2">Second component type</typeparam>
        /// <param name="manager">Entity manager instance</param>
        /// <param name="entities">List of entities to process</param>
        /// <param name="range">Range of entities to process</param>
        /// <param name="processor">Action to process the loaded components with entity context</param>
        public static void LoadAndProcessComponentPairs<T1, T2>(EntityManager manager, 
            List<EntityId> entities, (int start, int end) range, Action<EntityId, T1, T2> processor)
        {
            ValidateInputs(manager, entities, range, processor);
            
            for (int i = range.start; i < range.end; i++)
            {
                var entity = entities[i];
                var comp1 = manager.GetComponent<T1>(entity);
                var comp2 = manager.GetComponent<T2>(entity);
                processor(entity, comp1, comp2);
            }
        }
        
        /// <summary>
        /// Loads component pairs with write-back support for a range of entities
        /// </summary>
        /// <typeparam name="T1">First component type</typeparam>
        /// <typeparam name="T2">Second component type</typeparam>
        /// <param name="manager">Entity manager instance</param>
        /// <param name="entities">List of entities to process</param>
        /// <param name="range">Range of entities to process</param>
        /// <param name="processor">Function to process and return updated components</param>
        public static void LoadAndProcessComponentPairsWrite<T1, T2>(EntityManager manager, 
            List<EntityId> entities, (int start, int end) range, Func<T1, T2, (T1, T2)> processor)
        {
            ValidateInputs(manager, entities, range, processor);
            
            for (int i = range.start; i < range.end; i++)
            {
                var entity = entities[i];
                var comp1 = manager.GetComponent<T1>(entity);
                var comp2 = manager.GetComponent<T2>(entity);
                var (newComp1, newComp2) = processor(comp1, comp2);
                manager.SetComponent(entity, newComp1);
                manager.SetComponent(entity, newComp2);
            }
        }
        
        /// <summary>
        /// Loads hot component arrays for SIMD-optimized processing
        /// </summary>
        /// <typeparam name="T1">First component type</typeparam>
        /// <typeparam name="T2">Second component type</typeparam>
        /// <param name="manager">Entity manager instance</param>
        /// <param name="processor">Action to process hot component arrays</param>
        /// <param name="batchSize">Size of SIMD batches</param>
        public static void LoadAndProcessHotComponents<T1, T2>(EntityManager manager, 
            Action<T1[], T2[], int> processor, int batchSize = 1000) 
            where T1 : unmanaged where T2 : unmanaged
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));
            
            if (processor == null)
                throw new ArgumentNullException(nameof(processor));
            
            if (batchSize <= 0)
                throw new ArgumentException("Batch size must be positive", nameof(batchSize));
            
            var matchingArchetypes = GetArchetypesWithComponents<T1, T2>(manager);
            
            foreach (var archetype in matchingArchetypes)
            {
                var chunks = GetChunksForArchetype(manager, archetype);
                
                foreach (var chunk in chunks)
                {
                    var array1 = chunk.GetComponentArray<T1>(ComponentTypeRegistry.Get<T1>());
                    var array2 = chunk.GetComponentArray<T2>(ComponentTypeRegistry.Get<T2>());
                    var count = chunk.Count;
                    
                    // Process in SIMD-friendly batches
                    for (int i = 0; i < count; i += Vector<float>.Count)
                    {
                        var simdBatchSize = Math.Min(Vector<float>.Count, count - i);
                        processor(array1, array2, simdBatchSize);
                    }
                }
            }
        }
        
        /// <summary>
        /// Gets all archetypes that have the specified components
        /// </summary>
        /// <typeparam name="T1">First component type</typeparam>
        /// <typeparam name="T2">Second component type</typeparam>
        /// <param name="manager">Entity manager instance</param>
        /// <returns>Enumerable of matching archetypes</returns>
        public static IEnumerable<Archetype> GetArchetypesWithComponents<T1, T2>(EntityManager manager)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));
            
            var componentType1 = ComponentTypeRegistry.Get<T1>();
            var componentType2 = ComponentTypeRegistry.Get<T2>();
            var mask = 1UL << componentType1.Id | 1UL << componentType2.Id;
            
            return manager.GetArchetypeChunks().Keys
                .Where(archetype => archetype.HasAllComponents(mask));
        }
        
        /// <summary>
        /// Gets chunks for a specific archetype
        /// </summary>
        /// <param name="manager">Entity manager instance</param>
        /// <param name="archetype">Archetype to get chunks for</param>
        /// <returns>Enumerable of archetype chunks</returns>
        public static IEnumerable<ArchetypeChunk> GetChunksForArchetype(EntityManager manager, Archetype archetype)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));
            
            return manager.GetArchetypeChunks()[archetype];
        }
        
        /// <summary>
        /// Validates input parameters for component batch loading
        /// </summary>
        private static void ValidateInputs(EntityManager manager, List<EntityId> entities, 
            (int start, int end) range, object processor)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));
            
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));
            
            if (range.start < 0 || range.end > entities.Count || range.start >= range.end)
                throw new ArgumentException("Invalid range", nameof(range));
            
            if (processor == null)
                throw new ArgumentNullException(nameof(processor));
        }
        
        /// <summary>
        /// Loads component data for a single entity
        /// </summary>
        /// <typeparam name="T1">First component type</typeparam>
        /// <typeparam name="T2">Second component type</typeparam>
        /// <param name="manager">Entity manager instance</param>
        /// <param name="entity">Entity to load components for</param>
        /// <returns>Tuple of loaded components</returns>
        public static (T1, T2) LoadComponentPair<T1, T2>(EntityManager manager, EntityId entity)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));
            
            var comp1 = manager.GetComponent<T1>(entity);
            var comp2 = manager.GetComponent<T2>(entity);
            return (comp1, comp2);
        }
        
        /// <summary>
        /// Loads component data for multiple entities
        /// </summary>
        /// <typeparam name="T1">First component type</typeparam>
        /// <typeparam name="T2">Second component type</typeparam>
        /// <param name="manager">Entity manager instance</param>
        /// <param name="entities">Entities to load components for</param>
        /// <returns>List of entity-component tuples</returns>
        public static List<(EntityId, T1, T2)> LoadComponentPairs<T1, T2>(EntityManager manager, IEnumerable<EntityId> entities)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));
            
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));
            
            var results = new List<(EntityId, T1, T2)>();
            foreach (var entity in entities)
            {
                var comp1 = manager.GetComponent<T1>(entity);
                var comp2 = manager.GetComponent<T2>(entity);
                results.Add((entity, comp1, comp2));
            }
            return results;
        }
    }
} 