using System;
using System.Collections.Generic;
using System.Reflection;

namespace ECS
{
    /// <summary>
    /// SIMULATION ONLY: Simulates what a real source generator would produce for component accessors
    /// This is a stub implementation for demonstration purposes only.
    /// In a real implementation, this would be generated at compile time by a source generator.
    /// </summary>
    public static class SimulatedSourceGeneratorAccessor
    {
        private static readonly Dictionary<Type, Delegate> _generatedAccessors = new();
        
        static SimulatedSourceGeneratorAccessor()
        {
            // Pre-generate accessors for common component types
            PreGenerateCommonAccessors();
        }
        
        private static void PreGenerateCommonAccessors()
        {
            var commonTypes = new[] { typeof(Position), typeof(Velocity), typeof(Rotation), typeof(Scale), typeof(Name) };
            
            foreach (var type in commonTypes)
            {
                GenerateComponentAccessor(type);
            }
        }
        
        /// <summary>
        /// SIMULATION: Generate a type-safe component accessor delegate
        /// In practice, this would be generated at compile time by a source generator
        /// </summary>
        public static Func<EntityManager, EntityId, T> GenerateComponentAccessor<T>()
        {
            var type = typeof(T);
            if (_generatedAccessors.TryGetValue(type, out var cached))
                return (Func<EntityManager, EntityId, T>)cached;
            
            // Simulate generated code - in practice this would be generated at compile time
            var accessor = new Func<EntityManager, EntityId, T>((manager, entity) =>
            {
                return manager.GetComponent<T>(entity);
            });
            
            _generatedAccessors[type] = accessor;
            return accessor;
        }
        
        private static void GenerateComponentAccessor(Type componentType)
        {
            // SIMULATION: This would be generated at compile time by a source generator
            var method = typeof(SimulatedSourceGeneratorAccessor).GetMethod(nameof(GenerateComponentAccessor), 
                BindingFlags.Public | BindingFlags.Static);
            if (method != null)
            {
                var genericMethod = method.MakeGenericMethod(componentType);
                genericMethod.Invoke(null, null);
            }
        }
        
        /// <summary>
        /// SIMULATION: Get a cached type-safe accessor for a component type
        /// </summary>
        public static Func<EntityManager, EntityId, T> GetCachedAccessor<T>()
        {
            return GenerateComponentAccessor<T>();
        }
        
        /// <summary>
        /// SIMULATION: Optimized component access using generated delegates
        /// </summary>
        public static T GetComponentOptimized<T>(this EntityManager manager, EntityId entity)
        {
            var accessor = GetCachedAccessor<T>();
            return accessor(manager, entity);
        }
    }
} 