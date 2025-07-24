using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Numerics;
using System.IO;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ECS
{
    #region Component Heat Classification Attributes
    
    /// <summary>
    /// Marks a component as frequently accessed (hot) for SIMD optimization
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class HotComponentAttribute : Attribute { }
    
    /// <summary>
    /// Marks a component as rarely accessed (cold) for memory optimization
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class ColdComponentAttribute : Attribute { }
    
    /// <summary>
    /// Specifies the memory layout for SIMD optimization
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class SimdLayoutAttribute : Attribute 
    {
        public int Alignment { get; }
        public SimdLayoutAttribute(int alignment = 16) => Alignment = alignment;
    }
    
    #endregion
    
    #region Enhanced Component Types
    
    /// <summary>
    /// Enhanced component type with heat classification and SIMD layout info
    /// </summary>
    public readonly struct EnhancedComponentType : IEquatable<EnhancedComponentType>
    {
        public readonly int Id;
        public readonly Type Type;
        public readonly ComponentHeat Heat;
        public readonly int Size;
        public readonly int Alignment;
        public readonly bool IsSimdOptimized;
        
        public EnhancedComponentType(int id, Type type)
        {
            Id = id;
            Type = type;
            Heat = ComponentHeatClassifier.GetComponentHeat(type);
            Size = Marshal.SizeOf(type);
            Alignment = SimdAlignmentUtility.GetComponentAlignment(type);
            IsSimdOptimized = SimdAlignmentUtility.IsComponentSimdOptimized(type);
        }
        
        /// <summary>
        /// Register a component type for precomputed tables
        /// </summary>
        public static void RegisterComponentType<T>(ComponentHeat heat = ComponentHeat.Hot, int alignment = 4, bool isSimdOptimized = false)
        {
            ComponentHeatClassifier.RegisterComponentType<T>(heat);
            SimdAlignmentUtility.RegisterComponentType<T>(alignment, isSimdOptimized);
        }
        
        /// <summary>
        /// Pre-register common component types for optimal performance
        /// </summary>
        public static void PreRegisterCommonTypes()
        {
            ComponentHeatClassifier.PreRegisterCommonTypes();
            SimdAlignmentUtility.PreRegisterCommonTypes();
        }
        
        public bool Equals(EnhancedComponentType other) => Id == other.Id;
        public override bool Equals(object? obj) => obj is EnhancedComponentType other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode();
        public static bool operator ==(EnhancedComponentType left, EnhancedComponentType right) => left.Equals(right);
        public static bool operator !=(EnhancedComponentType left, EnhancedComponentType right) => !left.Equals(right);
    }
    
    #endregion
    
    #region Flat SIMD Memory Layout
    
    /// <summary>
    /// Flat memory layout for SIMD-optimized component storage
    /// </summary>
    public unsafe class FlatChunkMemory : IDisposable
    {
        private readonly byte* _memory;
        private readonly int _capacity;
        private readonly int _entityStride;
        private readonly Dictionary<int, int> _componentOffsets;
        private readonly Dictionary<int, int> _componentSizes;
        
        public FlatChunkMemory(int capacity, EnhancedComponentType[] componentTypes)
        {
            _capacity = capacity;
            _componentOffsets = new Dictionary<int, int>();
            _componentSizes = new Dictionary<int, int>();
            
            // Calculate memory layout
            int currentOffset = 0;
            foreach (var componentType in componentTypes.OrderBy(c => c.Heat).ThenBy(c => c.Id))
            {
                var alignedSize = AlignSize(componentType.Size, componentType.Alignment);
                _componentOffsets[componentType.Id] = currentOffset;
                _componentSizes[componentType.Id] = alignedSize;
                currentOffset += alignedSize;
            }
            
            _entityStride = currentOffset;
            
            // Allocate flat memory
            var totalSize = _entityStride * capacity;
            _memory = (byte*)Marshal.AllocHGlobal(totalSize);
            GC.AddMemoryPressure(totalSize);
        }
        
        private static int AlignSize(int size, int alignment)
        {
            return SimdAlignmentUtility.AlignSize(size, alignment);
        }
        
        public void* GetComponentPtr(int entityIndex, int componentId)
        {
            var offset = _componentOffsets[componentId];
            return _memory + (entityIndex * _entityStride) + offset;
        }
        
        public T GetComponent<T>(int entityIndex, int componentId) where T : unmanaged
        {
            return *(T*)GetComponentPtr(entityIndex, componentId);
        }
        
        public void SetComponent<T>(int entityIndex, int componentId, T value) where T : unmanaged
        {
            *(T*)GetComponentPtr(entityIndex, componentId) = value;
        }
        
        public void CopyComponent(int fromEntityIndex, int toEntityIndex, int componentId)
        {
            var fromPtr = GetComponentPtr(fromEntityIndex, componentId);
            var toPtr = GetComponentPtr(toEntityIndex, componentId);
            var size = _componentSizes[componentId];
            Buffer.MemoryCopy(fromPtr, toPtr, size, size);
        }
        
        public void Dispose()
        {
            if (_memory != null)
            {
                var totalSize = _entityStride * _capacity;
                Marshal.FreeHGlobal((IntPtr)_memory);
                GC.RemoveMemoryPressure(totalSize);
            }
        }
    }
    
    #endregion
    
    #region Archetype Explosion Prevention
    
    /// <summary>
    /// Prevents archetype explosion by limiting active archetypes and caching migrations
    /// </summary>
    public class ArchetypeGraph
    {
        private readonly Dictionary<Archetype, HashSet<Archetype>> _migrationCache = new();
        private readonly Dictionary<BitSet, Archetype> _archetypeCache = new();
        private readonly Dictionary<BitSet, long> _archetypeTimestamps = new(); // LRU timestamps
        private readonly Dictionary<Archetype, long> _accessTimestamps = new(); // Access tracking
        private readonly int _maxArchetypes;
        private int _currentArchetypeCount;
        private long _currentTimestamp = 0;
        private readonly object _evictionLock = new();
        
        public ArchetypeGraph(int maxArchetypes = 1000)
        {
            _maxArchetypes = maxArchetypes;
        }
        
        public Archetype GetOrCreateArchetype(EnhancedComponentType[] componentTypes)
        {
            var bitSet = CalculateBitSet(componentTypes);
            
            if (_archetypeCache.TryGetValue(bitSet, out var existing))
            {
                // Update access timestamp for LRU tracking
                UpdateAccessTimestamp(existing);
                return existing;
            }
            
            if (_currentArchetypeCount >= _maxArchetypes)
            {
                // Evict least recently used archetype
                EvictOldestArchetype();
            }
            
            // Convert EnhancedComponentType to ComponentType for compatibility
            var componentTypesArray = componentTypes.Select(ect => new ComponentType(ect.Id, ect.Type)).ToArray();
            var archetype = new Archetype(componentTypesArray);
            
            // Add to cache with current timestamp
            lock (_evictionLock)
            {
                _archetypeCache[bitSet] = archetype;
                _archetypeTimestamps[bitSet] = _currentTimestamp++;
                _accessTimestamps[archetype] = _currentTimestamp++;
                _currentArchetypeCount++;
            }
            
            return archetype;
        }
        
        public Archetype? GetMigrationPath(Archetype from, EnhancedComponentType[] toComponents)
        {
            if (_migrationCache.TryGetValue(from, out var migrations))
            {
                foreach (var migration in migrations)
                {
                    var migrationComponentTypes = migration.ComponentTypes;
                    var toComponentTypes = toComponents.Select(ect => new ComponentType(ect.Id, ect.Type)).ToArray();
                    if (migrationComponentTypes.SequenceEqual(toComponentTypes))
                    {
                        // Update access timestamp for LRU tracking
                        UpdateAccessTimestamp(migration);
                        return migration;
                    }
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Update access timestamp for LRU tracking
        /// </summary>
        private void UpdateAccessTimestamp(Archetype archetype)
        {
            lock (_evictionLock)
            {
                _accessTimestamps[archetype] = _currentTimestamp++;
            }
        }
        
        /// <summary>
        /// Evict the least recently used archetype based on access timestamps
        /// </summary>
        private void EvictOldestArchetype()
        {
            lock (_evictionLock)
            {
                if (_archetypeCache.Count == 0) return;
                
                // Find the archetype with the oldest access timestamp
                Archetype? oldestArchetype = null;
                long oldestTimestamp = long.MaxValue;
                
                foreach (var kvp in _accessTimestamps)
                {
                    if (kvp.Value < oldestTimestamp)
                    {
                        oldestTimestamp = kvp.Value;
                        oldestArchetype = kvp.Key;
                    }
                }
                
                if (oldestArchetype != null)
                {
                    // Remove from all caches
                    var bitmaskToRemove = _archetypeCache.FirstOrDefault(kvp => kvp.Value.HashCode == oldestArchetype.Value.HashCode).Key;
                    if (bitmaskToRemove != 0)
                    {
                        _archetypeCache.Remove(bitmaskToRemove);
                        _archetypeTimestamps.Remove(bitmaskToRemove);
                    }
                    
                    _accessTimestamps.Remove(oldestArchetype.Value);
                    _migrationCache.Remove(oldestArchetype.Value);
                    _currentArchetypeCount--;
                }
            }
        }
        
        /// <summary>
        /// Get statistics about archetype cache usage
        /// </summary>
        public (int totalArchetypes, int maxArchetypes, long oldestTimestamp, long newestTimestamp) GetStatistics()
        {
            lock (_evictionLock)
            {
                var timestamps = _accessTimestamps.Values.ToList();
                var oldest = timestamps.Count > 0 ? timestamps.Min() : 0;
                var newest = timestamps.Count > 0 ? timestamps.Max() : 0;
                
                return (_currentArchetypeCount, _maxArchetypes, oldest, newest);
            }
        }
        
        /// <summary>
        /// Clear all cached archetypes (useful for testing or memory management)
        /// </summary>
        public void ClearCache()
        {
            lock (_evictionLock)
            {
                _archetypeCache.Clear();
                _archetypeTimestamps.Clear();
                _accessTimestamps.Clear();
                _migrationCache.Clear();
                _currentArchetypeCount = 0;
                _currentTimestamp = 0;
            }
        }
        
        /// <summary>
        /// Get the least recently used archetype (for testing)
        /// </summary>
        public Archetype? GetLeastRecentlyUsedArchetype()
        {
            lock (_evictionLock)
            {
                if (_accessTimestamps.Count == 0) return null;
                
                var oldest = _accessTimestamps.OrderBy(kvp => kvp.Value).First();
                return oldest.Key;
            }
        }
        
        private static BitSet CalculateBitSet(EnhancedComponentType[] types)
        {
            var bitSet = new BitSet(256);
            foreach (var type in types)
            {
                if (type.Id < 256)
                    bitSet.Set(type.Id);
            }
            return bitSet;
        }
    }
    
    #endregion
    
    #region System Abstraction
    
    /// <summary>
    /// Base class for ECS systems with dependency tracking and batching
    /// </summary>
    public abstract class SystemBase
    {
        public readonly string Name;
        public readonly int Priority;
        public readonly bool IsParallel;
        
        protected readonly EntityManager EntityManager;
        public readonly Dictionary<Type, ComponentAccess> ComponentAccesses = new();
        
        protected SystemBase(EntityManager entityManager, string name, int priority = 0, bool isParallel = false)
        {
            EntityManager = entityManager;
            Name = name;
            Priority = priority;
            IsParallel = isParallel;
        }
        
        protected void ReadComponent<T>() where T : struct
        {
            ComponentAccesses[typeof(T)] = ComponentAccess.Read;
        }
        
        protected void WriteComponent<T>() where T : struct
        {
            ComponentAccesses[typeof(T)] = ComponentAccess.Write;
        }
        
        protected void ReadWriteComponent<T>() where T : struct
        {
            ComponentAccesses[typeof(T)] = ComponentAccess.ReadWrite;
        }
        
        public abstract void Update(float deltaTime);
        
        public virtual void OnStart() { }
        public virtual void OnStop() { }
        
        public bool HasDependencyConflict(SystemBase other)
        {
            foreach (var kvp in ComponentAccesses)
            {
                if (other.ComponentAccesses.TryGetValue(kvp.Key, out var otherAccess))
                {
                    if (kvp.Value == ComponentAccess.Write || otherAccess == ComponentAccess.Write)
                        return true;
                }
            }
            return false;
        }
    }
    
    public enum ComponentAccess
    {
        Read,
        Write,
        ReadWrite
    }
    
    /// <summary>
    /// System scheduler that handles dependencies and parallel execution
    /// </summary>
    public class SystemScheduler
    {
        private readonly List<SystemBase> _systems = new();
        private readonly Dictionary<SystemBase, List<SystemBase>> _dependencies = new();
        private readonly Dictionary<SystemBase, List<SystemBase>> _dependents = new();
        private readonly Dictionary<SystemBase, ActionBlock<float>> _systemTasks = new();
        private readonly ExecutionDataflowBlockOptions _executionOptions;
        private readonly DataflowLinkOptions _linkOptions;
        private bool _isInitialized = false;
        
        public SystemScheduler(int maxDegreeOfParallelism = -1)
        {
            _executionOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism == -1 ? Environment.ProcessorCount : maxDegreeOfParallelism,
                BoundedCapacity = 1000,
                EnsureOrdered = false
            };
            
            _linkOptions = new DataflowLinkOptions
            {
                PropagateCompletion = true
            };
        }
        
        public void AddSystem(SystemBase system)
        {
            if (_isInitialized)
                throw new InvalidOperationException("Cannot add systems after scheduler is initialized");
                
            _systems.Add(system);
            _dependencies[system] = new List<SystemBase>();
            _dependents[system] = new List<SystemBase>();
        }
        
        /// <summary>
        /// Initialize the task graph for parallel execution
        /// </summary>
        public void InitializeTaskGraph()
        {
            if (_isInitialized) return;
            
            // Create task blocks for each system
            foreach (var system in _systems)
            {
                var taskBlock = new ActionBlock<float>(deltaTime =>
                {
                    try
                    {
                        system.Update(deltaTime);
                    }
                    catch (Exception ex)
                    {
                        // Log system execution errors
                        Console.WriteLine($"Error in system {system.Name}: {ex.Message}");
                    }
                }, _executionOptions);
                
                _systemTasks[system] = taskBlock;
            }
            
            // Build dependency graph
            BuildDependencyGraph();
            
            _isInitialized = true;
        }
        
        /// <summary>
        /// Build dependency graph based on component access patterns
        /// </summary>
        private void BuildDependencyGraph()
        {
            for (int i = 0; i < _systems.Count; i++)
            {
                for (int j = i + 1; j < _systems.Count; j++)
                {
                    var system1 = _systems[i];
                    var system2 = _systems[j];
                    
                    if (HasDependency(system1, system2))
                    {
                        _dependencies[system2].Add(system1);
                        _dependents[system1].Add(system2);
                    }
                    else if (HasDependency(system2, system1))
                    {
                        _dependencies[system1].Add(system2);
                        _dependents[system2].Add(system1);
                    }
                }
            }
        }
        

        
        /// <summary>
        /// Update all systems using parallel task graph
        /// </summary>
        public async Task UpdateAsync(float deltaTime)
        {
            if (!_isInitialized)
                InitializeTaskGraph();
            
            // Post delta time to all root systems (systems with no dependencies)
            var rootSystems = _systems.Where(s => _dependencies[s].Count == 0).ToList();
            
            foreach (var rootSystem in rootSystems)
            {
                var taskBlock = _systemTasks[rootSystem];
                await taskBlock.SendAsync(deltaTime);
            }
            
            // Wait for all systems to complete
            var allTasks = _systemTasks.Values.ToArray();
            await Task.WhenAll(allTasks.Select(t => t.Completion));
        }
        
        /// <summary>
        /// Update all systems synchronously (for backward compatibility)
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!_isInitialized)
                InitializeTaskGraph();
            
            // Execute systems in dependency order
            var sortedSystems = TopologicalSort(_systems);
            
            foreach (var system in sortedSystems)
            {
                system.Update(deltaTime);
            }
        }
        
        /// <summary>
        /// Execute systems in parallel groups based on dependencies
        /// </summary>
        public async Task UpdateParallelAsync(float deltaTime)
        {
            if (!_isInitialized)
                InitializeTaskGraph();
            
            var sortedSystems = TopologicalSort(_systems);
            var parallelGroups = GroupParallelSystems(sortedSystems);
            
            // Execute each group in parallel, but groups sequentially
            foreach (var group in parallelGroups)
            {
                var tasks = group.Select(system => Task.Run(() => system.Update(deltaTime))).ToArray();
                await Task.WhenAll(tasks);
            }
        }
        
        /// <summary>
        /// Group systems that can run in parallel (no dependencies between them)
        /// </summary>
        private List<List<SystemBase>> GroupParallelSystems(List<SystemBase> sortedSystems)
        {
            var groups = new List<List<SystemBase>>();
            var remainingSystems = new HashSet<SystemBase>(sortedSystems);
            
            while (remainingSystems.Count > 0)
            {
                var currentGroup = new List<SystemBase>();
                var systemsToRemove = new List<SystemBase>();
                
                foreach (var system in remainingSystems)
                {
                    // Check if all dependencies are satisfied (already processed)
                    var dependencies = _dependencies[system];
                    if (dependencies.All(dep => !remainingSystems.Contains(dep)))
                    {
                        currentGroup.Add(system);
                        systemsToRemove.Add(system);
                    }
                }
                
                if (currentGroup.Count == 0)
                {
                    // Handle circular dependencies by adding remaining systems
                    currentGroup.AddRange(remainingSystems);
                    systemsToRemove.AddRange(remainingSystems);
                }
                
                groups.Add(currentGroup);
                
                foreach (var system in systemsToRemove)
                {
                    remainingSystems.Remove(system);
                }
            }
            
            return groups;
        }
        
        /// <summary>
        /// Check if system1 depends on system2
        /// </summary>
        private bool HasDependency(SystemBase system1, SystemBase system2)
        {
            // Check for component access conflicts
            foreach (var kvp1 in system1.ComponentAccesses)
            {
                var componentType = kvp1.Key;
                var access1 = kvp1.Value;
                
                if (system2.ComponentAccesses.TryGetValue(componentType, out var access2))
                {
                    // Write-Write or Write-Read conflict
                    if (access1 == ComponentAccess.Write || access2 == ComponentAccess.Write)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Topological sort for dependency resolution
        /// </summary>
        private List<SystemBase> TopologicalSort(List<SystemBase> systems)
        {
            var result = new List<SystemBase>();
            var visited = new HashSet<SystemBase>();
            var temp = new HashSet<SystemBase>();
            
            foreach (var system in systems)
            {
                if (!visited.Contains(system))
                {
                    Visit(system, visited, temp, result);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// DFS visit for topological sort
        /// </summary>
        private void Visit(SystemBase system, HashSet<SystemBase> visited, HashSet<SystemBase> temp, List<SystemBase> result)
        {
            if (temp.Contains(system))
            {
                // Circular dependency detected
                Console.WriteLine($"Warning: Circular dependency detected involving system {system.Name}");
                return;
            }
            
            if (visited.Contains(system))
                return;
            
            temp.Add(system);
            
            foreach (var dependent in _dependents[system])
            {
                Visit(dependent, visited, temp, result);
            }
            
            temp.Remove(system);
            visited.Add(system);
            result.Add(system);
        }
        
        /// <summary>
        /// Get execution statistics
        /// </summary>
        public (int totalSystems, int parallelGroups, int maxDegreeOfParallelism) GetStatistics()
        {
            if (!_isInitialized)
                return (_systems.Count, 0, _executionOptions.MaxDegreeOfParallelism);
            
            var sortedSystems = TopologicalSort(_systems);
            var parallelGroups = GroupParallelSystems(sortedSystems);
            
            return (_systems.Count, parallelGroups.Count, _executionOptions.MaxDegreeOfParallelism);
        }
        
        /// <summary>
        /// Shutdown the task graph
        /// </summary>
        public async Task ShutdownAsync()
        {
            foreach (var taskBlock in _systemTasks.Values)
            {
                taskBlock.Complete();
            }
            
            var allTasks = _systemTasks.Values.ToArray();
            await Task.WhenAll(allTasks.Select(t => t.Completion));
        }
    }
    
    #endregion
    
    #region Reflection Optimization (DEPRECATED)
    
    /// <summary>
    /// DEPRECATED: Optimized reflection with cached delegates and code generation
    /// 
    /// ⚠️ This class is deprecated and will be removed in a future version.
    /// Use source-generated methods from ECSComponentAccessGenerator instead.
    /// 
    /// Migration guide:
    /// - Replace ReflectionOptimizer.GetSetComponentDelegate with ECSDelegateStorage.GetSetter
    /// - Replace ReflectionOptimizer.GetGetComponentDelegate with ECSDelegateStorage.GetAccessor
    /// - Use generated extension methods: GetComponentOptimized, SetComponentOptimized
    /// </summary>
    [Obsolete("Use source-generated methods from ECSComponentAccessGenerator instead. This class will be removed in a future version.")]
    public static class ReflectionOptimizer
    {
        private static readonly Dictionary<Type, Delegate> _cachedDelegates = new();
        private static readonly Dictionary<Type, Action<object, object>> _setComponentDelegates = new();
        private static readonly Dictionary<Type, Func<object, object>> _getComponentDelegates = new();
        
        static ReflectionOptimizer()
        {
            // Pre-generate common component type delegates
            PreGenerateCommonDelegates();
        }
        
        private static void PreGenerateCommonDelegates()
        {
            // Generate delegates for common component types
            var commonTypes = new[] { typeof(Vector3), typeof(Vector4), typeof(Quaternion), typeof(float), typeof(int) };
            
            foreach (var type in commonTypes)
            {
                GenerateSetComponentDelegate(type);
                GenerateGetComponentDelegate(type);
            }
        }
        
        /// <summary>
        /// DEPRECATED: Use ECSDelegateStorage.GetSetter instead
        /// </summary>
        [Obsolete("Use ECSDelegateStorage.GetSetter<T>() instead")]
        public static Action<object, object> GetSetComponentDelegate(Type componentType)
        {
            if (_setComponentDelegates.TryGetValue(componentType, out var cached))
                return cached;
            
            var generated = GenerateSetComponentDelegate(componentType);
            _setComponentDelegates[componentType] = generated;
            return generated;
        }
        
        /// <summary>
        /// DEPRECATED: Use ECSDelegateStorage.GetAccessor instead
        /// </summary>
        [Obsolete("Use ECSDelegateStorage.GetAccessor<T>() instead")]
        public static Func<object, object> GetGetComponentDelegate(Type componentType)
        {
            if (_getComponentDelegates.TryGetValue(componentType, out var cached))
                return cached;
            
            var generated = GenerateGetComponentDelegate(componentType);
            _getComponentDelegates[componentType] = generated;
            return generated;
        }
        
        private static Action<object, object> GenerateSetComponentDelegate(Type componentType)
        {
            var assemblyName = new AssemblyName("DynamicAssembly");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");
            var typeBuilder = moduleBuilder.DefineType("SetComponentDelegate", TypeAttributes.Public | TypeAttributes.Sealed);
            
            var methodBuilder = typeBuilder.DefineMethod("SetComponent", 
                MethodAttributes.Public | MethodAttributes.Static, 
                typeof(void), 
                new[] { typeof(object), typeof(object) });
            
            var il = methodBuilder.GetILGenerator();
            
            // Cast parameters to correct types
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, componentType);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, componentType);
            
            // Set the value (assuming it's a field or property)
            // This is simplified - in practice you'd need to handle the specific storage mechanism
            il.Emit(OpCodes.Stobj, componentType);
            il.Emit(OpCodes.Ret);
            
            var delegateType = typeBuilder.CreateType();
            return (Action<object, object>)Delegate.CreateDelegate(typeof(Action<object, object>), delegateType.GetMethod("SetComponent")!);
        }
        
        private static Func<object, object> GenerateGetComponentDelegate(Type componentType)
        {
            // Similar implementation for get component
            // Simplified for brevity
            return (obj) => obj;
        }
        
        /// <summary>
        /// DEPRECATED: Clear cached delegates
        /// </summary>
        [Obsolete("Use ECSDelegateStorage.ClearAllDelegates() instead")]
        public static void ClearCachedDelegates()
        {
            _cachedDelegates.Clear();
            _setComponentDelegates.Clear();
            _getComponentDelegates.Clear();
        }
    }
    
    #endregion
    
    #region Event System
    
    /// <summary>
    /// Event raised when a component is added, removed, or modified
    /// </summary>
    public class ComponentChangedEvent<T> where T : struct
    {
        public EntityId Entity { get; }
        public T? OldValue { get; }
        public T NewValue { get; }
        public ComponentChangeType ChangeType { get; }
        
        public ComponentChangedEvent(EntityId entity, T? oldValue, T newValue, ComponentChangeType changeType)
        {
            Entity = entity;
            OldValue = oldValue;
            NewValue = newValue;
            ChangeType = changeType;
        }
    }
    
    /// <summary>
    /// Non-generic event for internal storage
    /// </summary>
    public class ComponentChangedEvent
    {
        public EntityId Entity { get; }
        public object? OldValue { get; }
        public object? NewValue { get; }
        public ComponentChangeType ChangeType { get; }
        
        public ComponentChangedEvent(EntityId entity, object? oldValue, object? newValue, ComponentChangeType changeType)
        {
            Entity = entity;
            OldValue = oldValue;
            NewValue = newValue;
            ChangeType = changeType;
        }
    }
    
    public enum ComponentChangeType
    {
        Added,
        Removed,
        Modified
    }
    
    /// <summary>
    /// Manages component change events with efficient subscription handling
    /// </summary>
    public class EventManager
    {
        private readonly Dictionary<Type, List<Delegate>> _eventHandlers = new();
        private readonly Dictionary<Type, List<ComponentChangedEvent>> _pendingEvents = new();
        private bool _isProcessingEvents = false;
        
        public void Subscribe<T>(Action<ComponentChangedEvent<T>> handler) where T : struct
        {
            var type = typeof(T);
            if (!_eventHandlers.ContainsKey(type))
                _eventHandlers[type] = new List<Delegate>();
            _eventHandlers[type].Add(handler);
        }
        
        public void Unsubscribe<T>(Action<ComponentChangedEvent<T>> handler) where T : struct
        {
            var type = typeof(T);
            if (_eventHandlers.ContainsKey(type))
                _eventHandlers[type].Remove(handler);
        }
        
        public void RaiseEvent<T>(ComponentChangedEvent<T> evt) where T : struct
        {
            var type = typeof(T);
            if (_eventHandlers.ContainsKey(type))
            {
                foreach (var handler in _eventHandlers[type])
                {
                    try
                    {
                        ((Action<ComponentChangedEvent<T>>)handler)(evt);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in event handler: {ex.Message}");
                    }
                }
            }
        }
        
        public void QueueEvent<T>(ComponentChangedEvent<T> evt) where T : struct
        {
            var type = typeof(T);
            if (!_pendingEvents.ContainsKey(type))
                _pendingEvents[type] = new List<ComponentChangedEvent>();
            
            // Convert to object for storage
            var objEvt = new ComponentChangedEvent(evt.Entity, evt.OldValue, evt.NewValue, evt.ChangeType);
            _pendingEvents[type].Add(objEvt);
        }
        
        public void ProcessPendingEvents()
        {
            if (_isProcessingEvents) return;
            _isProcessingEvents = true;
            
            foreach (var kvp in _pendingEvents)
            {
                var type = kvp.Key;
                var events = kvp.Value;
                
                if (_eventHandlers.ContainsKey(type))
                {
                    foreach (var evt in events)
                    {
                        foreach (var handler in _eventHandlers[type])
                        {
                            try
                            {
                                // Convert back to typed event using reflection
                                var genericType = handler.GetType().GetGenericArguments()[0];
                                var typedEventType = typeof(ComponentChangedEvent<>).MakeGenericType(genericType);
                                var typedEvt = Activator.CreateInstance(typedEventType, evt.Entity, evt.OldValue, evt.NewValue, evt.ChangeType);
                                handler.DynamicInvoke(typedEvt);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error processing pending event: {ex.Message}");
                            }
                        }
                    }
                }
                
                events.Clear();
            }
            
            _isProcessingEvents = false;
        }
    }
    
    #endregion
    
    #region Enhanced Memory Pooling
    
    /// <summary>
    /// Thread-safe memory pool for component arrays with SIMD alignment
    /// </summary>
    public class ComponentArrayPool<T> where T : struct
    {
        private readonly Stack<T[]> _pool = new();
        private readonly object _lock = new();
        private readonly int _arraySize;
        private readonly int _maxPoolSize;
        private int _totalAllocated;
        private int _totalReturned;
        
        public ComponentArrayPool(int arraySize = 1024, int maxPoolSize = 100)
        {
            _arraySize = arraySize;
            _maxPoolSize = maxPoolSize;
        }
        
        public T[] Rent()
        {
            lock (_lock)
            {
                if (_pool.Count > 0)
                {
                    _totalReturned--;
                    return _pool.Pop();
                }
                
                _totalAllocated++;
                return new T[_arraySize];
            }
        }
        
        public void Return(T[] array)
        {
            if (array == null || array.Length != _arraySize) return;
            
            lock (_lock)
            {
                if (_pool.Count < _maxPoolSize)
                {
                    // Clear the array to prevent memory leaks
                    Array.Clear(array, 0, array.Length);
                    _pool.Push(array);
                    _totalReturned++;
                }
            }
        }
        
        public (int totalAllocated, int totalReturned, int poolSize) GetStatistics()
        {
            lock (_lock)
            {
                return (_totalAllocated, _totalReturned, _pool.Count);
            }
        }
        
        public void Clear()
        {
            lock (_lock)
            {
                _pool.Clear();
            }
        }
    }
    
    /// <summary>
    /// Global memory pool manager for all component types
    /// </summary>
    public static class MemoryPoolManager
    {
        private static readonly Dictionary<Type, object> _pools = new();
        private static readonly object _lock = new();
        
        public static ComponentArrayPool<T> GetPool<T>(int arraySize = 1024) where T : struct
        {
            var type = typeof(T);
            
            lock (_lock)
            {
                if (!_pools.ContainsKey(type))
                {
                    _pools[type] = new ComponentArrayPool<T>(arraySize);
                }
                
                return (ComponentArrayPool<T>)_pools[type];
            }
        }
        
        public static void ClearAllPools()
        {
            lock (_lock)
            {
                foreach (var pool in _pools.Values)
                {
                    var clearMethod = pool.GetType().GetMethod("Clear");
                    clearMethod?.Invoke(pool, null);
                }
            }
        }
        
        public static Dictionary<Type, (int totalAllocated, int totalReturned, int poolSize)> GetStatistics()
        {
            var stats = new Dictionary<Type, (int totalAllocated, int totalReturned, int poolSize)>();
            
            lock (_lock)
            {
                foreach (var kvp in _pools)
                {
                    var getStatsMethod = kvp.Value.GetType().GetMethod("GetStatistics");
                    var result = getStatsMethod?.Invoke(kvp.Value, null);
                    if (result is ValueTuple<int, int, int> statsTuple)
                    {
                        stats[kvp.Key] = statsTuple;
                    }
                }
            }
            
            return stats;
        }
    }
    
    #endregion
    
    #region SIMD Operations
    
    /// <summary>
    /// SIMD-optimized operations for component processing
    /// </summary>
    public static class SimdOperations
    {
        /// <summary>
        /// Update positions using SIMD operations
        /// </summary>
        public static void UpdatePositions(Vector3[] positions, Vector3[] velocities, int count, float deltaTime = 1.0f)
        {
            // Temporarily disable SIMD to fix stability issues
            UpdatePositionsScalar(positions, velocities, count, deltaTime);
        }
        
        private static void UpdatePositionsSimd(Vector3[] positions, Vector3[] velocities, int count, float deltaTime)
        {
            // Process 4 positions at a time using SIMD
            int simdCount = count - (count % 4);
            
            for (int i = 0; i < simdCount; i += 4)
            {
                // Create arrays for SIMD operations
                var posXArray = new float[4];
                var posYArray = new float[4];
                var posZArray = new float[4];
                var velXArray = new float[4];
                var velYArray = new float[4];
                var velZArray = new float[4];
                
                // Load 4 positions and velocities
                for (int j = 0; j < 4; j++)
                {
                    posXArray[j] = positions[i + j].X;
                    posYArray[j] = positions[i + j].Y;
                    posZArray[j] = positions[i + j].Z;
                    velXArray[j] = velocities[i + j].X;
                    velYArray[j] = velocities[i + j].Y;
                    velZArray[j] = velocities[i + j].Z;
                }
                
                // Create SIMD vectors
                var posX = new Vector<float>(posXArray);
                var posY = new Vector<float>(posYArray);
                var posZ = new Vector<float>(posZArray);
                var velX = new Vector<float>(velXArray);
                var velY = new Vector<float>(velYArray);
                var velZ = new Vector<float>(velZArray);
                
                // Update positions
                var deltaTimeVec = new Vector<float>(deltaTime);
                posX += velX * deltaTimeVec;
                posY += velY * deltaTimeVec;
                posZ += velZ * deltaTimeVec;
                
                // Store updated positions back to arrays
                posX.CopyTo(posXArray);
                posY.CopyTo(posYArray);
                posZ.CopyTo(posZArray);
                
                // Update the original positions
                for (int j = 0; j < 4; j++)
                {
                    positions[i + j].X = posXArray[j];
                    positions[i + j].Y = posYArray[j];
                    positions[i + j].Z = posZArray[j];
                }
            }
            
            // Handle remaining elements
            for (int i = simdCount; i < count; i++)
            {
                positions[i].X += velocities[i].X * deltaTime;
                positions[i].Y += velocities[i].Y * deltaTime;
                positions[i].Z += velocities[i].Z * deltaTime;
            }
        }
        
        private static void UpdatePositionsScalar(Vector3[] positions, Vector3[] velocities, int count, float deltaTime)
        {
            for (int i = 0; i < count; i++)
            {
                positions[i].X += velocities[i].X * deltaTime;
                positions[i].Y += velocities[i].Y * deltaTime;
                positions[i].Z += velocities[i].Z * deltaTime;
            }
        }
        
        /// <summary>
        /// Apply forces to velocities using SIMD
        /// </summary>
        public static void ApplyForces(Vector3[] velocities, Vector3[] forces, int count, float deltaTime = 1.0f)
        {
            // Temporarily disable SIMD to fix stability issues
            ApplyForcesScalar(velocities, forces, count, deltaTime);
        }
        
        private static void ApplyForcesSimd(Vector3[] velocities, Vector3[] forces, int count, float deltaTime)
        {
            int simdCount = count - (count % 4);
            
            for (int i = 0; i < simdCount; i += 4)
            {
                // Create arrays for SIMD operations
                var velXArray = new float[4];
                var velYArray = new float[4];
                var velZArray = new float[4];
                var forceXArray = new float[4];
                var forceYArray = new float[4];
                var forceZArray = new float[4];
                
                // Load 4 velocities and forces
                for (int j = 0; j < 4; j++)
                {
                    velXArray[j] = velocities[i + j].X;
                    velYArray[j] = velocities[i + j].Y;
                    velZArray[j] = velocities[i + j].Z;
                    forceXArray[j] = forces[i + j].X;
                    forceYArray[j] = forces[i + j].Y;
                    forceZArray[j] = forces[i + j].Z;
                }
                
                // Create SIMD vectors
                var velX = new Vector<float>(velXArray);
                var velY = new Vector<float>(velYArray);
                var velZ = new Vector<float>(velZArray);
                var forceX = new Vector<float>(forceXArray);
                var forceY = new Vector<float>(forceYArray);
                var forceZ = new Vector<float>(forceZArray);
                
                // Apply forces
                var deltaTimeVec = new Vector<float>(deltaTime);
                velX += forceX * deltaTimeVec;
                velY += forceY * deltaTimeVec;
                velZ += forceZ * deltaTimeVec;
                
                // Store updated velocities back to arrays
                velX.CopyTo(velXArray);
                velY.CopyTo(velYArray);
                velZ.CopyTo(velZArray);
                
                // Update the original velocities
                for (int j = 0; j < 4; j++)
                {
                    velocities[i + j].X = velXArray[j];
                    velocities[i + j].Y = velYArray[j];
                    velocities[i + j].Z = velZArray[j];
                }
            }
            
            // Handle remaining elements
            for (int i = simdCount; i < count; i++)
            {
                velocities[i].X += forces[i].X * deltaTime;
                velocities[i].Y += forces[i].Y * deltaTime;
                velocities[i].Z += forces[i].Z * deltaTime;
            }
        }
        
        private static void ApplyForcesScalar(Vector3[] velocities, Vector3[] forces, int count, float deltaTime)
        {
            for (int i = 0; i < count; i++)
            {
                velocities[i].X += forces[i].X * deltaTime;
                velocities[i].Y += forces[i].Y * deltaTime;
                velocities[i].Z += forces[i].Z * deltaTime;
            }
        }
        
        /// <summary>
        /// Calculate distances between positions using SIMD
        /// </summary>
        public static void CalculateDistances(Vector3[] positions1, Vector3[] positions2, float[] distances, int count)
        {
            // Temporarily disable SIMD to fix stability issues
            CalculateDistancesScalar(positions1, positions2, distances, count);
        }
        
        private static void CalculateDistancesSimd(Vector3[] positions1, Vector3[] positions2, float[] distances, int count)
        {
            int simdCount = count - (count % 4);
            
            for (int i = 0; i < simdCount; i += 4)
            {
                // Create arrays for SIMD operations
                var pos1XArray = new float[4];
                var pos1YArray = new float[4];
                var pos1ZArray = new float[4];
                var pos2XArray = new float[4];
                var pos2YArray = new float[4];
                var pos2ZArray = new float[4];
                
                // Load 4 positions
                for (int j = 0; j < 4; j++)
                {
                    pos1XArray[j] = positions1[i + j].X;
                    pos1YArray[j] = positions1[i + j].Y;
                    pos1ZArray[j] = positions1[i + j].Z;
                    pos2XArray[j] = positions2[i + j].X;
                    pos2YArray[j] = positions2[i + j].Y;
                    pos2ZArray[j] = positions2[i + j].Z;
                }
                
                // Create SIMD vectors
                var pos1X = new Vector<float>(pos1XArray);
                var pos1Y = new Vector<float>(pos1YArray);
                var pos1Z = new Vector<float>(pos1ZArray);
                var pos2X = new Vector<float>(pos2XArray);
                var pos2Y = new Vector<float>(pos2YArray);
                var pos2Z = new Vector<float>(pos2ZArray);
                
                // Calculate differences
                var diffX = pos1X - pos2X;
                var diffY = pos1Y - pos2Y;
                var diffZ = pos1Z - pos2Z;
                
                // Calculate squared distances
                var distSquared = diffX * diffX + diffY * diffY + diffZ * diffZ;
                
                // Calculate square roots (approximate for performance)
                var distancesVec = Vector.SquareRoot(distSquared);
                
                // Store distances
                var distancesArray = new float[4];
                distancesVec.CopyTo(distancesArray);
                
                for (int j = 0; j < 4; j++)
                {
                    distances[i + j] = distancesArray[j];
                }
            }
            
            // Handle remaining elements
            for (int i = simdCount; i < count; i++)
            {
                var diffX = positions1[i].X - positions2[i].X;
                var diffY = positions1[i].Y - positions2[i].Y;
                var diffZ = positions1[i].Z - positions2[i].Z;
                distances[i] = (float)Math.Sqrt(diffX * diffX + diffY * diffY + diffZ * diffZ);
            }
        }
        
        private static void CalculateDistancesScalar(Vector3[] positions1, Vector3[] positions2, float[] distances, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var diffX = positions1[i].X - positions2[i].X;
                var diffY = positions1[i].Y - positions2[i].Y;
                var diffZ = positions1[i].Z - positions2[i].Z;
                distances[i] = (float)Math.Sqrt(diffX * diffX + diffY * diffY + diffZ * diffZ);
            }
        }
    }
    
    #endregion
    
    #region Example Systems
    
    /// <summary>
    /// Example physics system with SIMD optimization
    /// </summary>
    public class PhysicsSystem : SystemBase
    {
        public PhysicsSystem(EntityManager entityManager) : base(entityManager, "Physics", 100, true)
        {
            ReadComponent<Position>();
            ReadWriteComponent<Velocity>();
            ReadComponent<Mass>();
        }
        
        public override void Update(float deltaTime)
        {
            // Process physics in batches for SIMD optimization
            EntityManager.ProcessHotComponentsBatch<Position, Velocity>(
                (positions, velocities, count) =>
                {
                    for (int i = 0; i < count; i++)
                    {
                        // SIMD-optimized physics update
                        var gravity = new Vector3(0, -9.81f * deltaTime, 0);
                        velocities[i] = new Velocity { Value = velocities[i].Value + gravity };
                        positions[i] = new Position { Value = positions[i].Value + velocities[i].Value * deltaTime };
                    }
                });
        }
    }
    
    /// <summary>
    /// Example rendering system
    /// </summary>
    public class RenderingSystem : SystemBase
    {
        public RenderingSystem(EntityManager entityManager) : base(entityManager, "Rendering", 200)
        {
            ReadComponent<Position>();
            ReadComponent<Rotation>();
            ReadComponent<Scale>();
            ReadComponent<Mesh>();
        }
        
        public override void Update(float deltaTime)
        {
            // Process rendering
            foreach (var (entity, position, rotation, scale, mesh) in EntityManager.For<Position, Rotation, Scale, Mesh>())
            {
                // Render entity
                RenderEntity(entity, position, rotation, scale, mesh);
            }
        }
        
        private void RenderEntity(EntityId entity, Position position, Rotation rotation, Scale scale, Mesh mesh)
        {
            // Rendering logic here
        }
    }
    
    #endregion
    
    #region Example Components with Attributes
    
    // Note: Basic components (Position, Velocity, Name, Health, Transform) are now defined in Components.cs
    // to avoid conflicts with the main EntityManager system
    
    [HotComponent]
    [SimdLayout(16)]
    public struct Rotation
    {
        public Quaternion Value;
    }
    
    [HotComponent]
    [SimdLayout(16)]
    public struct Scale
    {
        public Vector3 Value;
    }
    
    [ColdComponent]
    public struct Description
    {
        public string Value;
    }
    
    [HotComponent]
    public struct Mass
    {
        public float Value;
    }
    
    [ColdComponent]
    public struct Mesh
    {
        public int MeshId;
        public int MaterialId;
    }
    
    [HotComponent]
    public struct Physics
    {
        public float Mass;
        public Vector3 Velocity;
        public Vector3 Force;
        public bool IsStatic;
    }
    
    [ColdComponent]
    public struct Metadata
    {
        public string Name;
        public string Description;
        public Dictionary<string, object> Properties;
    }
    
        // Note: Health component is now defined in Components.cs

    [ColdComponent]
    public struct Tag
    {
        public string Value;
    }

    [ColdComponent]
    public struct UI
    {
        public bool IsVisible;
        public Vector2 Position;
        public Vector2 Size;
    }

    [ColdComponent]
    public struct Audio
    {
        public int SoundId;
        public float Volume;
        public bool IsPlaying;
    }
    
    public struct Vector2
    {
        public float X, Y;
        public Vector2(float x, float y) { X = x; Y = y; }
    }
    
    #endregion
    
    #region Vector Types (Simplified)
    
    public struct Vector3
    {
        public float X, Y, Z;
        public Vector3(float x, float y, float z) { X = x; Y = y; Z = z; }
        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3 operator *(Vector3 a, float b) => new(a.X * b, a.Y * b, a.Z * b);
        public static Vector3 operator *(float a, Vector3 b) => new(a * b.X, a * b.Y, a * b.Z);
        
        public static float Distance(Vector3 a, Vector3 b)
        {
            var diff = a - b;
            return (float)Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y + diff.Z * diff.Z);
        }
        
        public static Vector3 Normalize(Vector3 v)
        {
            var length = (float)Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
            if (length == 0) return new Vector3(0, 0, 0);
            return new Vector3(v.X / length, v.Y / length, v.Z / length);
        }
    }
    
    public struct Vector4
    {
        public float X, Y, Z, W;
        public Vector4(float x, float y, float z, float w) { X = x; Y = y; Z = z; W = w; }
    }
    
    public struct Quaternion
    {
        public float X, Y, Z, W;
        public Quaternion(float x, float y, float z, float w) { X = x; Y = y; Z = z; W = w; }
    }
    
    #endregion

    #region Serialization Support
    
    /// <summary>
    /// Interface for component serialization
    /// </summary>
    public interface IComponentSerializer<T> where T : struct
    {
        void Serialize(BinaryWriter writer, T component);
        T Deserialize(BinaryReader reader);
    }
    
    /// <summary>
    /// Default serializer for simple value types
    /// </summary>
    public class DefaultComponentSerializer<T> : IComponentSerializer<T> where T : struct
    {
        public void Serialize(BinaryWriter writer, T component)
        {
            var bytes = new byte[Marshal.SizeOf<T>()];
            var handle = GCHandle.Alloc(component, GCHandleType.Pinned);
            try
            {
                Marshal.Copy(handle.AddrOfPinnedObject(), bytes, 0, bytes.Length);
                writer.Write(bytes);
            }
            finally
            {
                handle.Free();
            }
        }
        
        public T Deserialize(BinaryReader reader)
        {
            var size = Marshal.SizeOf<T>();
            var bytes = reader.ReadBytes(size);
            var handle = GCHandle.Alloc(new byte[size], GCHandleType.Pinned);
            try
            {
                Marshal.Copy(bytes, 0, handle.AddrOfPinnedObject(), size);
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }
    }
    
    /// <summary>
    /// Serializer for Vector3 components
    /// </summary>
    public class Vector3Serializer : IComponentSerializer<Vector3>
    {
        public void Serialize(BinaryWriter writer, Vector3 component)
        {
            writer.Write(component.X);
            writer.Write(component.Y);
            writer.Write(component.Z);
        }
        
        public Vector3 Deserialize(BinaryReader reader)
        {
            return new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
        }
    }
    
    /// <summary>
    /// Serializer for string-based components
    /// </summary>
    public class StringComponentSerializer<T> : IComponentSerializer<T> where T : struct
    {
        private readonly Func<T, string> _toString;
        private readonly Func<string, T> _fromString;
        
        public StringComponentSerializer(Func<T, string> toString, Func<string, T> fromString)
        {
            _toString = toString;
            _fromString = fromString;
        }
        
        public void Serialize(BinaryWriter writer, T component)
        {
            var str = _toString(component);
            writer.Write(str ?? "");
        }
        
        public T Deserialize(BinaryReader reader)
        {
            var str = reader.ReadString();
            return _fromString(str);
        }
    }
    
    /// <summary>
    /// Entity and component serialization manager
    /// </summary>
    public class EntitySerializer
    {
        private readonly Dictionary<Type, object> _serializers = new();
        private readonly EntityManager _entityManager;
        
        public EntitySerializer(EntityManager entityManager)
        {
            _entityManager = entityManager;
            RegisterDefaultSerializers();
        }
        
        private void RegisterDefaultSerializers()
        {
            RegisterSerializer(new Vector3Serializer());
            RegisterSerializer(new StringComponentSerializer<Name>(
                name => name.Value,
                str => new Name { Value = str }
            ));
            RegisterSerializer(new StringComponentSerializer<Description>(
                desc => desc.Value,
                str => new Description { Value = str }
            ));
        }
        
        public void RegisterSerializer<T>(IComponentSerializer<T> serializer) where T : struct
        {
            _serializers[typeof(T)] = serializer;
        }
        
        public void SerializeEntity(BinaryWriter writer, EntityId entity)
        {
            // Write entity ID
            writer.Write(entity.Id);
            writer.Write(entity.Generation);
            
            // Get entity location using public methods
            var archetypeChunks = _entityManager.GetArchetypeChunks();
            var entityLocation = _entityManager.GetEntityLocation(entity);
            if (entityLocation == null)
            {
                writer.Write(0); // No components
                return;
            }
            
            var archetype = entityLocation.Value.archetype;
            var chunkIndex = entityLocation.Value.chunkIndex;
            var entityIndex = entityLocation.Value.entityIndex;
            var chunk = archetypeChunks[archetype][chunkIndex];
            
            // Write component count
            writer.Write(archetype.ComponentTypes.Length);
            
            // Write each component
            foreach (var componentType in archetype.ComponentTypes)
            {
                writer.Write(componentType.Id);
                SerializeComponent(writer, chunk, entityIndex, componentType);
            }
        }
        
        private void SerializeComponent(BinaryWriter writer, ArchetypeChunk chunk, int entityIndex, ComponentType componentType)
        {
            // Use reflection to call GetComponent with the correct type
            var getComponentMethod = typeof(ArchetypeChunk).GetMethod("GetComponent").MakeGenericMethod(componentType.Type);
            var component = getComponentMethod.Invoke(chunk, new object[] { entityIndex, componentType });
            
            if (_serializers.TryGetValue(componentType.Type, out var serializer))
            {
                var genericSerializer = serializer.GetType().GetInterface("IComponentSerializer`1");
                var serializeMethod = genericSerializer.GetMethod("Serialize");
                serializeMethod.Invoke(serializer, new[] { writer, component });
            }
            else
            {
                // Use reflection-based serialization for unknown types
                var bytes = new byte[Marshal.SizeOf(componentType.Type)];
                var handle = GCHandle.Alloc(component, GCHandleType.Pinned);
                try
                {
                    Marshal.Copy(handle.AddrOfPinnedObject(), bytes, 0, bytes.Length);
                    writer.Write(bytes);
                }
                finally
                {
                    handle.Free();
                }
            }
        }
        
        public EntityId DeserializeEntity(BinaryReader reader)
        {
            // Read entity ID
            var id = reader.ReadInt32();
            var generation = reader.ReadInt32();
            var entityId = new EntityId(id, generation);
            
            // Read component count
            var componentCount = reader.ReadInt32();
            if (componentCount == 0) return entityId;
            
            // Read components and create entity
            var componentTypes = new ComponentType[componentCount];
            var components = new object[componentCount];
            
            for (int i = 0; i < componentCount; i++)
            {
                var componentId = reader.ReadInt32();
                var componentType = ComponentTypeRegistry.GetType(componentId);
                if (componentType != null)
                {
                    componentTypes[i] = ComponentTypeRegistry.Get(componentType);
                }
                components[i] = DeserializeComponent(reader, componentTypes[i]);
            }
            
            // Create entity with components
            var newEntity = _entityManager.CreateEntity(componentTypes);
            
            // Set component values using reflection
            for (int i = 0; i < componentCount; i++)
            {
                if (componentTypes[i].Type == null)
                {
                    throw new InvalidOperationException($"Component type is null for index {i}");
                }
                
                var setComponentMethod = typeof(EntityManager).GetMethod("SetComponent", new[] { typeof(EntityId), typeof(ComponentType), componentTypes[i].Type }).MakeGenericMethod(componentTypes[i].Type);
                if (setComponentMethod == null)
                {
                    throw new InvalidOperationException($"Could not find SetComponent method for type {componentTypes[i].Type}");
                }
                
                setComponentMethod.Invoke(_entityManager, new object[] { newEntity, componentTypes[i], components[i] });
            }
            
            return newEntity;
        }
        
        private object DeserializeComponent(BinaryReader reader, ComponentType componentType)
        {
            if (_serializers.TryGetValue(componentType.Type, out var serializer))
            {
                var genericSerializer = serializer.GetType().GetInterface("IComponentSerializer`1");
                var deserializeMethod = genericSerializer.GetMethod("Deserialize");
                return deserializeMethod.Invoke(serializer, new[] { reader });
            }
            else
            {
                // Use reflection-based deserialization for unknown types
                var size = Marshal.SizeOf(componentType.Type);
                var bytes = reader.ReadBytes(size);
                var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                try
                {
                    return Marshal.PtrToStructure(handle.AddrOfPinnedObject(), componentType.Type);
                }
                finally
                {
                    handle.Free();
                }
            }
        }
        
        public void SerializeWorld(BinaryWriter writer)
        {
            var entities = _entityManager.GetAllEntities().ToList();
            writer.Write(entities.Count);
            
            foreach (var entity in entities)
            {
                SerializeEntity(writer, entity);
            }
        }
        
        public void DeserializeWorld(BinaryReader reader)
        {
            var entityCount = reader.ReadInt32();
            
            for (int i = 0; i < entityCount; i++)
            {
                DeserializeEntity(reader);
            }
        }
    }
    
    #endregion
    
    #region Enhanced System Framework
    
    /// <summary>
    /// Enhanced system interface with dependency injection support
    /// </summary>
    public interface ISystem
    {
        string Name { get; }
        int Priority { get; }
        bool IsParallel { get; }
        IEnumerable<Type> RequiredComponents { get; }
        IEnumerable<Type> OptionalComponents { get; }
        
        void Update(float deltaTime);
        void OnStart();
        void OnStop();
        void OnEntityAdded(EntityId entity);
        void OnEntityRemoved(EntityId entity);
        void OnComponentAdded<T>(EntityId entity, T component) where T : struct;
        void OnComponentRemoved<T>(EntityId entity, T component) where T : struct;
        void OnComponentChanged<T>(EntityId entity, T oldValue, T newValue) where T : struct;
    }
    
    /// <summary>
    /// Enhanced system base class with dependency tracking
    /// </summary>
    public abstract class EnhancedSystemBase : ISystem
    {
        public string Name { get; }
        public int Priority { get; }
        public bool IsParallel { get; }
        
        protected readonly EntityManager EntityManager;
        protected readonly EventManager EventManager;
        public readonly Dictionary<Type, ComponentAccess> ComponentAccesses = new();
        
        private readonly List<Type> _requiredComponents = new();
        private readonly List<Type> _optionalComponents = new();
        private readonly Dictionary<Type, List<EntityId>> _trackedEntities = new();
        
        protected EnhancedSystemBase(EntityManager entityManager, EventManager eventManager, string name, int priority = 0, bool isParallel = false)
        {
            EntityManager = entityManager;
            EventManager = eventManager;
            Name = name;
            Priority = priority;
            IsParallel = isParallel;
        }
        
        protected void RequireComponent<T>() where T : struct
        {
            var type = typeof(T);
            if (!_requiredComponents.Contains(type))
                _requiredComponents.Add(type);
            ComponentAccesses[type] = ComponentAccess.Read;
        }
        
        protected void RequireComponentWrite<T>() where T : struct
        {
            var type = typeof(T);
            if (!_requiredComponents.Contains(type))
                _requiredComponents.Add(type);
            ComponentAccesses[type] = ComponentAccess.Write;
        }
        
        protected void RequireComponentReadWrite<T>() where T : struct
        {
            var type = typeof(T);
            if (!_requiredComponents.Contains(type))
                _requiredComponents.Add(type);
            ComponentAccesses[type] = ComponentAccess.ReadWrite;
        }
        
        protected void OptionalComponent<T>() where T : struct
        {
            var type = typeof(T);
            if (!_optionalComponents.Contains(type))
                _optionalComponents.Add(type);
        }
        
        public IEnumerable<Type> RequiredComponents => _requiredComponents;
        public IEnumerable<Type> OptionalComponents => _optionalComponents;
        
        public abstract void Update(float deltaTime);
        
        public virtual void OnStart() { }
        public virtual void OnStop() { }
        public virtual void OnEntityAdded(EntityId entity) { }
        public virtual void OnEntityRemoved(EntityId entity) { }
        public virtual void OnComponentAdded<T>(EntityId entity, T component) where T : struct { }
        public virtual void OnComponentRemoved<T>(EntityId entity, T component) where T : struct { }
        public virtual void OnComponentChanged<T>(EntityId entity, T oldValue, T newValue) where T : struct { }
        
        protected IEnumerable<EntityId> GetTrackedEntities<T>() where T : struct
        {
            var type = typeof(T);
            return _trackedEntities.TryGetValue(type, out var entities) ? entities : Enumerable.Empty<EntityId>();
        }
        
        protected void TrackEntity<T>(EntityId entity) where T : struct
        {
            var type = typeof(T);
            if (!_trackedEntities.ContainsKey(type))
                _trackedEntities[type] = new List<EntityId>();
            if (!_trackedEntities[type].Contains(entity))
                _trackedEntities[type].Add(entity);
        }
        
        protected void UntrackEntity<T>(EntityId entity) where T : struct
        {
            var type = typeof(T);
            if (_trackedEntities.ContainsKey(type))
                _trackedEntities[type].Remove(entity);
        }
        
        public bool HasDependencyConflict(ISystem other)
        {
            foreach (var requiredType in RequiredComponents)
            {
                if (other.RequiredComponents.Contains(requiredType))
                {
                    // Check if both systems need write access
                    if (ComponentAccesses.TryGetValue(requiredType, out var myAccess) &&
                        ((EnhancedSystemBase)other).ComponentAccesses.TryGetValue(requiredType, out var otherAccess))
                    {
                        if (myAccess == ComponentAccess.Write || myAccess == ComponentAccess.ReadWrite ||
                            otherAccess == ComponentAccess.Write || otherAccess == ComponentAccess.ReadWrite)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
    
    /// <summary>
    /// Enhanced system scheduler with dependency resolution and parallel execution
    /// </summary>
    public class EnhancedSystemScheduler
    {
        private readonly List<ISystem> _systems = new();
        private readonly Dictionary<ISystem, List<ISystem>> _dependencies = new();
        private readonly Dictionary<ISystem, List<ISystem>> _dependents = new();
        private readonly EventManager _eventManager;
        private bool _isRunning = false;
        
        public EnhancedSystemScheduler(EventManager eventManager)
        {
            _eventManager = eventManager;
        }
        
        public void AddSystem(ISystem system)
        {
            if (_isRunning)
                throw new InvalidOperationException("Cannot add system while scheduler is running");
                
            _systems.Add(system);
            _dependencies[system] = new List<ISystem>();
            _dependents[system] = new List<ISystem>();
            
            // Build dependency graph
            foreach (var existingSystem in _systems)
            {
                if (existingSystem != system)
                {
                    if (HasDependency(existingSystem, system))
                    {
                        _dependencies[system].Add(existingSystem);
                        _dependents[existingSystem].Add(system);
                    }
                    else if (HasDependency(system, existingSystem))
                    {
                        _dependencies[existingSystem].Add(system);
                        _dependents[system].Add(existingSystem);
                    }
                }
            }
        }
        
        private bool HasDependency(ISystem system1, ISystem system2)
        {
            // Check if system1 depends on system2 (system2 must run before system1)
            foreach (var componentType in system1.RequiredComponents)
            {
                if (system2.RequiredComponents.Contains(componentType))
                {
                    // Check write access conflicts
                    if (((EnhancedSystemBase)system1).ComponentAccesses.TryGetValue(componentType, out var access1) &&
                        ((EnhancedSystemBase)system2).ComponentAccesses.TryGetValue(componentType, out var access2))
                    {
                        if ((access1 == ComponentAccess.Write || access1 == ComponentAccess.ReadWrite) ||
                            (access2 == ComponentAccess.Write || access2 == ComponentAccess.ReadWrite))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        
        public void Update(float deltaTime)
        {
            _isRunning = true;
            
            try
            {
                // Sort systems by dependencies
                var sortedSystems = TopologicalSort(_systems);
                
                // Group systems that can run in parallel
                var parallelGroups = GroupParallelSystems(sortedSystems);
                
                // Execute systems
                foreach (var group in parallelGroups)
                {
                    if (group.Count == 1)
                    {
                        // Single system - execute directly
                        var system = group[0];
                        try
                        {
                            system.Update(deltaTime);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error in system {system.Name}: {ex.Message}");
                        }
                    }
                    else
                    {
                        // Multiple systems - execute in parallel
                        var tasks = group.Select(system => Task.Run(() =>
                        {
                            try
                            {
                                system.Update(deltaTime);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error in parallel system {system.Name}: {ex.Message}");
                            }
                        })).ToArray();
                        
                        Task.WaitAll(tasks);
                    }
                }
                
                // Process pending events
                _eventManager.ProcessPendingEvents();
            }
            finally
            {
                _isRunning = false;
            }
        }
        
        private List<List<ISystem>> GroupParallelSystems(List<ISystem> sortedSystems)
        {
            var groups = new List<List<ISystem>>();
            var currentGroup = new List<ISystem>();
            
            foreach (var system in sortedSystems)
            {
                if (system.IsParallel && currentGroup.Count < Environment.ProcessorCount)
                {
                    // Check if this system can run in parallel with current group
                    bool canRunInParallel = true;
                    foreach (var groupSystem in currentGroup)
                    {
                        if (HasDependency(system, groupSystem) || HasDependency(groupSystem, system))
                        {
                            canRunInParallel = false;
                            break;
                        }
                    }
                    
                    if (canRunInParallel)
                    {
                        currentGroup.Add(system);
                    }
                    else
                    {
                        if (currentGroup.Count > 0)
                        {
                            groups.Add(currentGroup);
                            currentGroup = new List<ISystem>();
                        }
                        currentGroup.Add(system);
                    }
                }
                else
                {
                    if (currentGroup.Count > 0)
                    {
                        groups.Add(currentGroup);
                        currentGroup = new List<ISystem>();
                    }
                    currentGroup.Add(system);
                }
            }
            
            if (currentGroup.Count > 0)
                groups.Add(currentGroup);
                
            return groups;
        }
        
        private List<ISystem> TopologicalSort(List<ISystem> systems)
        {
            var result = new List<ISystem>();
            var visited = new HashSet<ISystem>();
            var temp = new HashSet<ISystem>();
            
            foreach (var system in systems)
            {
                if (!visited.Contains(system))
                    Visit(system, visited, temp, result);
            }
            
            return result;
        }
        
        private void Visit(ISystem system, HashSet<ISystem> visited, HashSet<ISystem> temp, List<ISystem> result)
        {
            if (temp.Contains(system))
                throw new InvalidOperationException($"Circular dependency detected involving system {system.Name}");
                
            if (visited.Contains(system))
                return;
                
            temp.Add(system);
            
            foreach (var dependent in _dependents[system])
            {
                Visit(dependent, visited, temp, result);
            }
            
            temp.Remove(system);
            visited.Add(system);
            result.Add(system);
        }
        
        public void Start()
        {
            foreach (var system in _systems)
            {
                system.OnStart();
            }
        }
        
        public void Stop()
        {
            foreach (var system in _systems)
            {
                system.OnStop();
            }
        }
        
        public List<ISystem> GetSystems() => _systems.ToList();
        
        public Dictionary<ISystem, List<ISystem>> GetDependencyGraph() => _dependencies.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList());
    }
    
    #endregion
} 