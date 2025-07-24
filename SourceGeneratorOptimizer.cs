using System;
using System.Collections.Generic;
using System.Linq;

namespace ECS
{
    /// <summary>
    /// SIMULATION ONLY: Consolidated interface for simulated source generator functionality
    /// This is a stub implementation that delegates to the actual simulated classes.
    /// In a real implementation, this would be generated at compile time by a source generator.
    /// </summary>
    public static class SourceGeneratorOptimizer
    {
        /// <summary>
        /// SIMULATION: Get a cached type-safe accessor for a component type
        /// Delegates to SimulatedSourceGeneratorAccessor
        /// </summary>
        public static Func<EntityManager, EntityId, T> GetCachedAccessor<T>()
        {
            return SimulatedSourceGeneratorAccessor.GetCachedAccessor<T>();
        }
        
        /// <summary>
        /// SIMULATION: Get a cached type-safe setter for a component type
        /// Delegates to SimulatedSourceGeneratorSetter
        /// </summary>
        public static Action<EntityManager, EntityId, T> GetCachedSetter<T>()
        {
            return SimulatedSourceGeneratorSetter.GetCachedSetter<T>();
        }
        
        /// <summary>
        /// SIMULATION: Optimized component access using generated delegates
        /// Delegates to SimulatedSourceGeneratorAccessor
        /// </summary>
        public static T GetComponentOptimized<T>(this EntityManager manager, EntityId entity)
        {
            return SimulatedSourceGeneratorAccessor.GetComponentOptimized<T>(manager, entity);
        }
        
        /// <summary>
        /// SIMULATION: Optimized component setting using generated delegates
        /// Delegates to SimulatedSourceGeneratorSetter
        /// </summary>
        public static void SetComponentOptimized<T>(this EntityManager manager, EntityId entity, T component)
        {
            SimulatedSourceGeneratorSetter.SetComponentOptimized<T>(manager, entity, component);
        }
    }
    
    /// <summary>
    /// SIMULATION: Extension methods for optimized component access
    /// </summary>
    public static class EntityManagerExtensions
    {
        /// <summary>
        /// SIMULATION: Optimized batch processing using generated delegates
        /// </summary>
        public static void ProcessComponentsOptimized<T1, T2>(this EntityManager manager, 
            Action<T1, T2> processor)
        {
            var accessor1 = SimulatedSourceGeneratorAccessor.GetCachedAccessor<T1>();
            var accessor2 = SimulatedSourceGeneratorAccessor.GetCachedAccessor<T2>();
            
            foreach (var entity in manager.GetEntitiesWithComponents<T1, T2>())
            {
                var comp1 = accessor1(manager, entity);
                var comp2 = accessor2(manager, entity);
                processor(comp1, comp2);
            }
        }
        
        /// <summary>
        /// SIMULATION: Optimized batch processing with entity context
        /// </summary>
        public static void ProcessComponentsOptimized<T1, T2>(this EntityManager manager, 
            Action<EntityId, T1, T2> processor)
        {
            var accessor1 = SimulatedSourceGeneratorAccessor.GetCachedAccessor<T1>();
            var accessor2 = SimulatedSourceGeneratorAccessor.GetCachedAccessor<T2>();
            
            foreach (var entity in manager.GetEntitiesWithComponents<T1, T2>())
            {
                var comp1 = accessor1(manager, entity);
                var comp2 = accessor2(manager, entity);
                processor(entity, comp1, comp2);
            }
        }
    }
} 