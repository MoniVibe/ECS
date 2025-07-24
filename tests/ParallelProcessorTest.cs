using System;
using System.Collections.Generic;
using System.Linq;

namespace ECS
{
    /// <summary>
    /// Test component types for parallel processing
    /// </summary>
    public struct TestPosition
    {
        public float X, Y, Z;
        
        public TestPosition(float x, float y, float z)
        {
            X = x; Y = y; Z = z;
        }
    }
    
    public struct TestVelocity
    {
        public float X, Y, Z;
        
        public TestVelocity(float x, float y, float z)
        {
            X = x; Y = y; Z = z;
        }
    }
    
    public struct TestHealth
    {
        public float Current;
        public float Max;
        
        public TestHealth(float current, float max)
        {
            Current = current;
            Max = max;
        }
    }
    
    /// <summary>
    /// Test class for parallel processing functionality
    /// </summary>
    public static class ParallelProcessorTest
    {
        public static void RunTests()
        {
            Console.WriteLine("=== Parallel Processing Refactor Tests ===");
            
            TestBasicParallelProcessing();
            TestParallelProcessingWithEntityContext();
            TestParallelProcessingWithWriteBack();
            TestHotComponentsParallel();
            TestBatchRangeScheduler();
            TestComponentBatchLoader();
            
            Console.WriteLine("All tests completed successfully!");
        }
        
        private static void TestBasicParallelProcessing()
        {
            Console.WriteLine("Testing basic parallel processing...");
            
            var manager = new EntityManager();
            
            // Create test entities
            for (int i = 0; i < 100; i++)
            {
                var entity = manager.CreateEntity<TestPosition, TestVelocity>();
                manager.SetComponent(entity, new TestPosition(i, i * 2, i * 3));
                manager.SetComponent(entity, new TestVelocity(i * 0.1f, i * 0.2f, i * 0.3f));
            }
            
            var processedCount = 0;
            var lockObject = new object();
            
            manager.ProcessComponentsParallel<TestPosition, TestVelocity>((pos, vel) =>
            {
                // Simulate some processing
                lock (lockObject)
                {
                    processedCount++;
                }
            }, batchSize: 10);
            
            Console.WriteLine($"Processed {processedCount} entities in parallel");
            
            if (processedCount != 100)
                throw new Exception($"Expected 100 processed entities, got {processedCount}");
        }
        
        private static void TestParallelProcessingWithEntityContext()
        {
            Console.WriteLine("Testing parallel processing with entity context...");
            
            var manager = new EntityManager();
            
            // Create test entities
            for (int i = 0; i < 50; i++)
            {
                var entity = manager.CreateEntity<TestPosition, TestHealth>();
                manager.SetComponent(entity, new TestPosition(i, i * 2, i * 3));
                manager.SetComponent(entity, new TestHealth(i * 10, 100));
            }
            
            var processedEntities = new List<EntityId>();
            var lockObject = new object();
            
            manager.ProcessComponentsParallel<TestPosition, TestHealth>((entity, pos, health) =>
            {
                lock (lockObject)
                {
                    processedEntities.Add(entity);
                }
            }, batchSize: 5);
            
            Console.WriteLine($"Processed {processedEntities.Count} entities with context");
            
            if (processedEntities.Count != 50)
                throw new Exception($"Expected 50 processed entities, got {processedEntities.Count}");
        }
        
        private static void TestParallelProcessingWithWriteBack()
        {
            Console.WriteLine("Testing parallel processing with write-back...");
            
            var manager = new EntityManager();
            
            // Create test entities
            for (int i = 0; i < 25; i++)
            {
                var entity = manager.CreateEntity<TestPosition, TestVelocity>();
                manager.SetComponent(entity, new TestPosition(i, i * 2, i * 3));
                manager.SetComponent(entity, new TestVelocity(i * 0.1f, i * 0.2f, i * 0.3f));
            }
            
            manager.ProcessComponentsParallelWrite<TestPosition, TestVelocity>((pos, vel) =>
            {
                // Update position based on velocity
                return (new TestPosition(pos.X + vel.X, pos.Y + vel.Y, pos.Z + vel.Z), vel);
            }, batchSize: 5);
            
            // Verify updates
            var entities = manager.GetEntitiesWithComponents<TestPosition, TestVelocity>().ToList();
            var firstEntity = entities[0];
            var finalPos = manager.GetComponent<TestPosition>(firstEntity);
            
            Console.WriteLine($"Updated position: {finalPos.X}, {finalPos.Y}, {finalPos.Z}");
        }
        
        private static void TestHotComponentsParallel()
        {
            Console.WriteLine("Testing hot components parallel processing...");
            
            var manager = new EntityManager();
            
            // Create test entities with unmanaged components
            for (int i = 0; i < 30; i++)
            {
                var entity = manager.CreateEntity<TestPosition, TestVelocity>();
                manager.SetComponent(entity, new TestPosition(i, i * 2, i * 3));
                manager.SetComponent(entity, new TestVelocity(i * 0.1f, i * 0.2f, i * 0.3f));
            }
            
            var processedCount = 0;
            var lockObject = new object();
            
            manager.ProcessHotComponentsParallel<TestPosition, TestVelocity>((positions, velocities, count) =>
            {
                lock (lockObject)
                {
                    processedCount += count;
                }
            }, batchSize: 10);
            
            Console.WriteLine($"Processed {processedCount} hot components in parallel");
        }
        
        private static void TestBatchRangeScheduler()
        {
            Console.WriteLine("Testing batch range scheduler...");
            
            var ranges = BatchRangeScheduler.CreateBatchRanges(100, 10);
            
            if (ranges.Count != 10)
                throw new Exception($"Expected 10 ranges, got {ranges.Count}");
            
            var optimalBatchSize = BatchRangeScheduler.GetOptimalBatchSize(1000, 4);
            Console.WriteLine($"Optimal batch size for 1000 items: {optimalBatchSize}");
            
            var maxParallelism = BatchRangeScheduler.GetMaxDegreeOfParallelism();
            Console.WriteLine($"Max degree of parallelism: {maxParallelism}");
        }
        
        private static void TestComponentBatchLoader()
        {
            Console.WriteLine("Testing component batch loader...");
            
            var manager = new EntityManager();
            
            // Create test entities
            for (int i = 0; i < 20; i++)
            {
                var entity = manager.CreateEntity<TestPosition, TestHealth>();
                manager.SetComponent(entity, new TestPosition(i, i * 2, i * 3));
                manager.SetComponent(entity, new TestHealth(i * 5, 100));
            }
            
            var entities = manager.GetEntitiesWithComponents<TestPosition, TestHealth>().ToList();
            var componentPairs = ComponentBatchLoader.LoadComponentPairs<TestPosition, TestHealth>(manager, entities);
            
            Console.WriteLine($"Loaded {componentPairs.Count} component pairs");
            
            if (componentPairs.Count != 20)
                throw new Exception($"Expected 20 component pairs, got {componentPairs.Count}");
            
            // Test single component pair loading
            var firstEntity = entities[0];
            var (pos, health) = ComponentBatchLoader.LoadComponentPair<TestPosition, TestHealth>(manager, firstEntity);
            
            Console.WriteLine($"Single pair loaded: Position({pos.X}, {pos.Y}, {pos.Z}), Health({health.Current}/{health.Max})");
        }
    }
} 