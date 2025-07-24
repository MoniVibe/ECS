using System;
using System.Collections.Generic;
using System.Reflection;

namespace ECS
{
    /// <summary>
    /// SIMULATION ONLY: Simulates what a real source generator would produce for component setters
    /// This is a stub implementation for demonstration purposes only.
    /// In a real implementation, this would be generated at compile time by a source generator.
    /// </summary>
    public static class SimulatedSourceGeneratorSetter
    {
        private static readonly Dictionary<Type, Delegate> _generatedSetters = new();
        
        static SimulatedSourceGeneratorSetter()
        {
            // Pre-generate setters for common component types
            PreGenerateCommonSetters();
        }
        
        private static void PreGenerateCommonSetters()
        {
            var commonTypes = new[] { typeof(Position), typeof(Velocity), typeof(Rotation), typeof(Scale), typeof(Name) };
            
            foreach (var type in commonTypes)
            {
                GenerateComponentSetter(type);
            }
        }
        
        /// <summary>
        /// SIMULATION: Generate a type-safe component setter delegate
        /// In practice, this would be generated at compile time by a source generator
        /// </summary>
        public static Action<EntityManager, EntityId, T> GenerateComponentSetter<T>()
        {
            var type = typeof(T);
            if (_generatedSetters.TryGetValue(type, out var cached))
                return (Action<EntityManager, EntityId, T>)cached;
            
            // Simulate generated code - in practice this would be generated at compile time
            var setter = new Action<EntityManager, EntityId, T>((manager, entity, component) =>
            {
                manager.SetComponent(entity, component);
            });
            
            _generatedSetters[type] = setter;
            return setter;
        }
        
        private static void GenerateComponentSetter(Type componentType)
        {
            // SIMULATION: This would be generated at compile time by a source generator
            var method = typeof(SimulatedSourceGeneratorSetter).GetMethod(nameof(GenerateComponentSetter), 
                BindingFlags.Public | BindingFlags.Static);
            if (method != null)
            {
                var genericMethod = method.MakeGenericMethod(componentType);
                genericMethod.Invoke(null, null);
            }
        }
        
        /// <summary>
        /// SIMULATION: Get a cached type-safe setter for a component type
        /// </summary>
        public static Action<EntityManager, EntityId, T> GetCachedSetter<T>()
        {
            return GenerateComponentSetter<T>();
        }
        
        /// <summary>
        /// SIMULATION: Optimized component setting using generated delegates
        /// </summary>
        public static void SetComponentOptimized<T>(this EntityManager manager, EntityId entity, T component)
        {
            var setter = GetCachedSetter<T>();
            setter(manager, entity, component);
        }
    }
} 