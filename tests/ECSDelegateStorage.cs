using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;

namespace ECS
{
    /// <summary>
    /// Type-safe delegate storage per component type to avoid boxing in dynamic contexts
    /// Thread-safe implementation for multithreaded job execution
    /// </summary>
    public static class ECSDelegateStorage
    {
        // Thread-safe concurrent storage per component type to avoid boxing in dynamic contexts
        private static readonly ConcurrentDictionary<Type, object> _componentAccessors = new();
        private static readonly ConcurrentDictionary<Type, object> _componentSetters = new();
        private static readonly ConcurrentDictionary<Type, object> _componentCopiers = new();
        private static readonly ConcurrentDictionary<Type, object> _componentSerializers = new();
        private static readonly ConcurrentDictionary<Type, object> _componentDeserializers = new();
        
        // Memory barrier for ensuring thread safety in delegate creation
        private static readonly object _delegateCreationLock = new();

        // Factory instances for creating delegates
        private static readonly ComponentAccessorFactory _accessorFactory = new();
        private static readonly ComponentSetterFactory _setterFactory = new();
        private static readonly ComponentCopierFactory _copierFactory = new();
        private static readonly ComponentSerializerFactory _serializerFactory = new();
        private static readonly ComponentDeserializerFactory _deserializerFactory = new();

        #region Thread-Safe Accessor Management

        /// <summary>
        /// Get or create a type-safe component accessor delegate with thread safety
        /// </summary>
        public static DelegateContracts.ComponentAccessor<T> GetAccessor<T>() where T : struct
        {
            var type = typeof(T);
            
            // Try to get existing delegate first (lock-free read)
            if (_componentAccessors.TryGetValue(type, out var cached))
            {
                Thread.MemoryBarrier(); // Ensure we see the latest value
                return (DelegateContracts.ComponentAccessor<T>)cached;
            }

            // Create delegate with proper synchronization
            return GetOrCreateAccessor<T>(type);
        }

        /// <summary>
        /// Get or create a type-safe component setter delegate with thread safety
        /// </summary>
        public static DelegateContracts.ComponentSetter<T> GetSetter<T>() where T : struct
        {
            var type = typeof(T);
            
            if (_componentSetters.TryGetValue(type, out var cached))
            {
                Thread.MemoryBarrier();
                return (DelegateContracts.ComponentSetter<T>)cached;
            }

            return GetOrCreateSetter<T>(type);
        }

        /// <summary>
        /// Get or create a type-safe component copier delegate with thread safety
        /// </summary>
        public static DelegateContracts.ComponentCopier<T> GetCopier<T>() where T : struct
        {
            var type = typeof(T);
            
            if (_componentCopiers.TryGetValue(type, out var cached))
            {
                Thread.MemoryBarrier();
                return (DelegateContracts.ComponentCopier<T>)cached;
            }

            return GetOrCreateCopier<T>(type);
        }

        /// <summary>
        /// Get or create a type-safe component serializer delegate with thread safety
        /// </summary>
        public static DelegateContracts.ComponentSerializer<T> GetSerializer<T>() where T : struct
        {
            var type = typeof(T);
            
            if (_componentSerializers.TryGetValue(type, out var cached))
            {
                Thread.MemoryBarrier();
                return (DelegateContracts.ComponentSerializer<T>)cached;
            }

            return GetOrCreateSerializer<T>(type);
        }

        /// <summary>
        /// Get or create a type-safe component deserializer delegate with thread safety
        /// </summary>
        public static DelegateContracts.ComponentDeserializer<T> GetDeserializer<T>() where T : struct
        {
            var type = typeof(T);
            
            if (_componentDeserializers.TryGetValue(type, out var cached))
            {
                Thread.MemoryBarrier();
                return (DelegateContracts.ComponentDeserializer<T>)cached;
            }

            return GetOrCreateDeserializer<T>(type);
        }

        #endregion

        #region Thread-Safe Delegate Creation

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DelegateContracts.ComponentAccessor<T> GetOrCreateAccessor<T>(Type type) where T : struct
        {
            lock (_delegateCreationLock)
            {
                // Double-check pattern for thread safety
                if (_componentAccessors.TryGetValue(type, out var cached))
                {
                    return (DelegateContracts.ComponentAccessor<T>)cached;
                }

                var accessor = _accessorFactory.CreateAccessor<T>();
                _componentAccessors[type] = accessor;
                Thread.MemoryBarrier(); // Ensure delegate is visible to all threads
                return accessor;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DelegateContracts.ComponentSetter<T> GetOrCreateSetter<T>(Type type) where T : struct
        {
            lock (_delegateCreationLock)
            {
                if (_componentSetters.TryGetValue(type, out var cached))
                {
                    return (DelegateContracts.ComponentSetter<T>)cached;
                }

                var setter = _setterFactory.CreateSetter<T>();
                _componentSetters[type] = setter;
                Thread.MemoryBarrier();
                return setter;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DelegateContracts.ComponentCopier<T> GetOrCreateCopier<T>(Type type) where T : struct
        {
            lock (_delegateCreationLock)
            {
                if (_componentCopiers.TryGetValue(type, out var cached))
                {
                    return (DelegateContracts.ComponentCopier<T>)cached;
                }

                var copier = _copierFactory.CreateCopier<T>();
                _componentCopiers[type] = copier;
                Thread.MemoryBarrier();
                return copier;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DelegateContracts.ComponentSerializer<T> GetOrCreateSerializer<T>(Type type) where T : struct
        {
            lock (_delegateCreationLock)
            {
                if (_componentSerializers.TryGetValue(type, out var cached))
                {
                    return (DelegateContracts.ComponentSerializer<T>)cached;
                }

                var serializer = _serializerFactory.CreateSerializer<T>();
                _componentSerializers[type] = serializer;
                Thread.MemoryBarrier();
                return serializer;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DelegateContracts.ComponentDeserializer<T> GetOrCreateDeserializer<T>(Type type) where T : struct
        {
            lock (_delegateCreationLock)
            {
                if (_componentDeserializers.TryGetValue(type, out var cached))
                {
                    return (DelegateContracts.ComponentDeserializer<T>)cached;
                }

                var deserializer = _deserializerFactory.CreateDeserializer<T>();
                _componentDeserializers[type] = deserializer;
                Thread.MemoryBarrier();
                return deserializer;
            }
        }

        #endregion



        #region Optimized Batch Processing

        /// <summary>
        /// Optimized batch processing using type-safe delegates
        /// </summary>
        public static void ProcessBatchOptimized<T>(this EntityManager manager, Action<T> processor) where T : struct
        {
            var accessor = GetAccessor<T>();
            
            foreach (var entity in manager.GetEntitiesWithComponents<T>())
            {
                var component = accessor(manager, entity);
                processor(component);
            }
        }

        /// <summary>
        /// Optimized batch processing with entity context using type-safe delegates
        /// </summary>
        public static void ProcessBatchOptimized<T>(this EntityManager manager, Action<EntityId, T> processor) where T : struct
        {
            var accessor = GetAccessor<T>();
            
            foreach (var entity in manager.GetEntitiesWithComponents<T>())
            {
                var component = accessor(manager, entity);
                processor(entity, component);
            }
        }

        /// <summary>
        /// Optimized batch processing for two components using type-safe delegates
        /// </summary>
        public static void ProcessBatchOptimized<T1, T2>(this EntityManager manager, Action<T1, T2> processor) 
            where T1 : struct where T2 : struct
        {
            var accessor1 = GetAccessor<T1>();
            var accessor2 = GetAccessor<T2>();
            
            foreach (var entity in manager.GetEntitiesWithComponents<T1, T2>())
            {
                var comp1 = accessor1(manager, entity);
                var comp2 = accessor2(manager, entity);
                processor(comp1, comp2);
            }
        }

        /// <summary>
        /// Optimized batch processing for two components with entity context using type-safe delegates
        /// </summary>
        public static void ProcessBatchOptimized<T1, T2>(this EntityManager manager, Action<EntityId, T1, T2> processor) 
            where T1 : struct where T2 : struct
        {
            var accessor1 = GetAccessor<T1>();
            var accessor2 = GetAccessor<T2>();
            
            foreach (var entity in manager.GetEntitiesWithComponents<T1, T2>())
            {
                var comp1 = accessor1(manager, entity);
                var comp2 = accessor2(manager, entity);
                processor(entity, comp1, comp2);
            }
        }

        #endregion

        #region SIMD-Optimized Processing

        /// <summary>
        /// SIMD-optimized batch processing using type-safe delegates
        /// </summary>
        public static void ProcessSimdOptimized<T>(this EntityManager manager, Action<T[], int> processor) where T : struct
        {
            // Note: ProcessHotComponentsBatch requires two type parameters, so we'll use a different approach
            // This is a placeholder implementation - the actual implementation would need to be adjusted
            // based on the specific requirements of the batch processing
            throw new NotImplementedException("ProcessSimdOptimized<T> needs to be implemented with proper batch processing");
        }



        #endregion

        #region Component Copying

        /// <summary>
        /// Type-safe component copying using generated delegates
        /// </summary>
        public static void CopyComponentOptimized<T>(ArchetypeChunk fromChunk, int fromIndex, 
            ArchetypeChunk toChunk, int toIndex, ComponentType componentType) where T : struct
        {
            var copier = GetCopier<T>();
            copier(fromChunk, fromIndex, toChunk, toIndex, componentType);
        }

        #endregion

        #region Component Serialization

        /// <summary>
        /// Type-safe component serialization using generated delegates
        /// </summary>
        public static void SerializeComponentOptimized<T>(BinaryWriter writer, T component) where T : struct
        {
            var serializer = GetSerializer<T>();
            serializer(writer, component);
        }

        /// <summary>
        /// Type-safe component deserialization using generated delegates
        /// </summary>
        public static T DeserializeComponentOptimized<T>(BinaryReader reader) where T : struct
        {
            var deserializer = GetDeserializer<T>();
            return deserializer(reader);
        }

        #endregion

        #region Statistics and Management

        /// <summary>
        /// Get statistics about cached delegates
        /// </summary>
        public static (int accessors, int setters, int copiers, int serializers, int deserializers) GetStatistics()
        {
            return (
                _componentAccessors.Count,
                _componentSetters.Count,
                _componentCopiers.Count,
                _componentSerializers.Count,
                _componentDeserializers.Count
            );
        }

        /// <summary>
        /// Clear all cached delegates (useful for testing or memory management)
        /// </summary>
        public static void ClearAllDelegates()
        {
            _componentAccessors.Clear();
            _componentSetters.Clear();
            _componentCopiers.Clear();
            _componentSerializers.Clear();
            _componentDeserializers.Clear();
        }

        /// <summary>
        /// Pre-generate delegates for common component types
        /// </summary>
        public static void PreGenerateCommonDelegates()
        {
            // Pre-generate for common component types
            GetAccessor<Position>();
            GetSetter<Position>();
            GetCopier<Position>();
            GetSerializer<Position>();
            GetDeserializer<Position>();

            GetAccessor<Velocity>();
            GetSetter<Velocity>();
            GetCopier<Velocity>();
            GetSerializer<Velocity>();
            GetDeserializer<Velocity>();

            GetAccessor<Rotation>();
            GetSetter<Rotation>();
            GetCopier<Rotation>();
            GetSerializer<Rotation>();
            GetDeserializer<Rotation>();

            GetAccessor<Scale>();
            GetSetter<Scale>();
            GetCopier<Scale>();
            GetSerializer<Scale>();
            GetDeserializer<Scale>();

            GetAccessor<Name>();
            GetSetter<Name>();
            GetCopier<Name>();
            GetSerializer<Name>();
            GetDeserializer<Name>();
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for optimized component access using delegate storage
    /// </summary>
    public static class EntityManagerDelegateExtensions
    {
        /// <summary>
        /// Optimized component access using cached delegates
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetComponentOptimized<T>(this EntityManager manager, EntityId entity) where T : struct
        {
            var accessor = ECSDelegateStorage.GetAccessor<T>();
            return accessor(manager, entity);
        }

        /// <summary>
        /// Optimized component setting using cached delegates
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetComponentOptimized<T>(this EntityManager manager, EntityId entity, T component) where T : struct
        {
            var setter = ECSDelegateStorage.GetSetter<T>();
            setter(manager, entity, component);
        }

        /// <summary>
        /// Optimized component checking using cached delegates
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasComponentOptimized<T>(this EntityManager manager, EntityId entity) where T : struct
        {
            return manager.HasComponent<T>(entity);
        }

        /// <summary>
        /// Optimized component addition using cached delegates
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddComponentOptimized<T>(this EntityManager manager, EntityId entity, T component) where T : struct
        {
            manager.AddComponent(entity, component);
        }

        /// <summary>
        /// Optimized component removal using cached delegates
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveComponentOptimized<T>(this EntityManager manager, EntityId entity) where T : struct
        {
            manager.RemoveComponent<T>(entity);
        }
    }
} 