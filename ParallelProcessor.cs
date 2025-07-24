using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;
using System.Collections.Concurrent;

namespace ECS
{
    /// <summary>
    /// Parallel processing support for ECS batch operations
    /// </summary>
    public static class ParallelProcessor
    {
        /// <summary>
        /// Process components in parallel using multiple threads
        /// </summary>
        public static void ProcessComponentsParallel<T1, T2>(this EntityManager manager, 
            Action<T1, T2> processor, int batchSize = 1000)
        {
            ValidateInputs(manager, processor, batchSize);
            
            var entities = manager.GetEntitiesWithComponents<T1, T2>().ToList();
            if (entities.Count == 0) return;
            
            BatchRangeScheduler.ProcessBatchesParallel(entities.Count, batchSize, range =>
            {
                ComponentBatchLoader.LoadAndProcessComponentPairs(manager, entities, range, processor);
            });
        }
        
        /// <summary>
        /// Process components in parallel with entity context
        /// </summary>
        public static void ProcessComponentsParallel<T1, T2>(this EntityManager manager, 
            Action<EntityId, T1, T2> processor, int batchSize = 1000)
        {
            ValidateInputs(manager, processor, batchSize);
            
            var entities = manager.GetEntitiesWithComponents<T1, T2>().ToList();
            if (entities.Count == 0) return;
            
            BatchRangeScheduler.ProcessBatchesParallel(entities.Count, batchSize, range =>
            {
                ComponentBatchLoader.LoadAndProcessComponentPairs(manager, entities, range, processor);
            });
        }
        
        /// <summary>
        /// Process components in parallel with write-back support
        /// </summary>
        public static void ProcessComponentsParallelWrite<T1, T2>(this EntityManager manager, 
            Func<T1, T2, (T1, T2)> processor, int batchSize = 1000)
        {
            ValidateInputs(manager, processor, batchSize);
            
            var entities = manager.GetEntitiesWithComponents<T1, T2>().ToList();
            if (entities.Count == 0) return;
            
            BatchRangeScheduler.ProcessBatchesParallel(entities.Count, batchSize, range =>
            {
                ComponentBatchLoader.LoadAndProcessComponentPairsWrite(manager, entities, range, processor);
            });
        }
        
        /// <summary>
        /// SIMD-optimized parallel processing for hot components
        /// </summary>
        public static void ProcessHotComponentsParallel<T1, T2>(this EntityManager manager, 
            Action<T1[], T2[], int> processor, int batchSize = 1000) 
            where T1 : unmanaged where T2 : unmanaged
        {
            ValidateInputs(manager, processor, batchSize);
            
            ComponentBatchLoader.LoadAndProcessHotComponents(manager, processor, batchSize);
        }
        
        /// <summary>
        /// Parallel processing with cancellation support
        /// </summary>
        public static async Task ProcessComponentsParallelAsync<T1, T2>(this EntityManager manager, 
            Action<T1, T2> processor, CancellationToken cancellationToken = default, int batchSize = 1000)
        {
            ValidateInputs(manager, processor, batchSize);
            
            var entities = manager.GetEntitiesWithComponents<T1, T2>().ToList();
            if (entities.Count == 0) return;
            
            await BatchRangeScheduler.ProcessBatchesParallelAsync(entities.Count, batchSize, range =>
            {
                ComponentBatchLoader.LoadAndProcessComponentPairs(manager, entities, range, processor);
            }, cancellationToken);
        }
        
        /// <summary>
        /// Process components with custom parallelism settings
        /// </summary>
        public static void ProcessComponentsParallel<T1, T2>(this EntityManager manager, 
            Action<T1, T2> processor, int batchSize, int maxDegreeOfParallelism)
        {
            ValidateInputs(manager, processor, batchSize);
            
            if (maxDegreeOfParallelism <= 0)
                throw new ArgumentException("Max degree of parallelism must be positive", nameof(maxDegreeOfParallelism));
            
            var entities = manager.GetEntitiesWithComponents<T1, T2>().ToList();
            if (entities.Count == 0) return;
            
            BatchRangeScheduler.ProcessBatchesParallel(entities.Count, batchSize, range =>
            {
                ComponentBatchLoader.LoadAndProcessComponentPairs(manager, entities, range, processor);
            }, maxDegreeOfParallelism);
        }
        
        /// <summary>
        /// Process components with optimal batch size calculation
        /// </summary>
        public static void ProcessComponentsParallelOptimal<T1, T2>(this EntityManager manager, 
            Action<T1, T2> processor, int targetBatchesPerThread = 4)
        {
            ValidateInputs(manager, processor, 1); // Use minimum batch size for calculation
            
            var entities = manager.GetEntitiesWithComponents<T1, T2>().ToList();
            if (entities.Count == 0) return;
            
            var optimalBatchSize = BatchRangeScheduler.GetOptimalBatchSize(entities.Count, targetBatchesPerThread);
            
            BatchRangeScheduler.ProcessBatchesParallel(entities.Count, optimalBatchSize, range =>
            {
                ComponentBatchLoader.LoadAndProcessComponentPairs(manager, entities, range, processor);
            });
        }
        
        /// <summary>
        /// Validates input parameters for parallel processing
        /// </summary>
        private static void ValidateInputs(EntityManager manager, object processor, int batchSize)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));
            
            if (processor == null)
                throw new ArgumentNullException(nameof(processor));
            
            if (batchSize <= 0)
                throw new ArgumentException("Batch size must be positive", nameof(batchSize));
        }
    }
    
    /// <summary>
    /// Thread-safe component access for parallel processing
    /// </summary>
    public static class ThreadSafeAccess
    {
        private static readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        
        /// <summary>
        /// Thread-safe component read
        /// </summary>
        public static T GetComponentThreadSafe<T>(this EntityManager manager, EntityId entity)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));
            
            _lock.EnterReadLock();
            try
            {
                return manager.GetComponent<T>(entity);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Thread-safe component write
        /// </summary>
        public static void SetComponentThreadSafe<T>(this EntityManager manager, EntityId entity, T component)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));
            
            _lock.EnterWriteLock();
            try
            {
                manager.SetComponent(entity, component);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Thread-safe batch read
        /// </summary>
        public static List<(EntityId, T1, T2)> GetComponentsThreadSafe<T1, T2>(this EntityManager manager)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));
            
            _lock.EnterReadLock();
            try
            {
                return ComponentBatchLoader.LoadComponentPairs<T1, T2>(manager, manager.GetEntitiesWithComponents<T1, T2>());
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
    
    /// <summary>
    /// Parallel processing statistics
    /// </summary>
    public readonly struct ParallelStats
    {
        public readonly int MaxDegreeOfParallelism;
        public readonly int BatchSize;
        public readonly int TotalEntities;
        public readonly TimeSpan ProcessingTime;
        
        public ParallelStats(int maxDegreeOfParallelism, int batchSize, int totalEntities, TimeSpan processingTime)
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism;
            BatchSize = batchSize;
            TotalEntities = totalEntities;
            ProcessingTime = processingTime;
        }
        
        public double EntitiesPerSecond => TotalEntities / ProcessingTime.TotalSeconds;
        
        public override string ToString()
        {
            return $"Parallel: {MaxDegreeOfParallelism} threads, {BatchSize} batch size, {TotalEntities} entities, {ProcessingTime.TotalMilliseconds:F2}ms ({EntitiesPerSecond:F0} entities/sec)";
        }
    }
} 