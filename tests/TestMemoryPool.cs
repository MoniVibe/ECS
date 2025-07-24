using System;

namespace ECS
{
    public class TestMemoryPool
    {
            public static void RunTest()
    {
        Console.WriteLine("=== Memory Pool Split Test ===\n");
        
        try
        {
            // Test 1: Generic Array Pool
            Console.WriteLine("Test 1: Generic Array Pool");
            TestGenericArrayPool();
            
            // Test 2: Chunk Pooling
            Console.WriteLine("\nTest 2: Chunk Pooling");
            TestChunkPooling();
            
            // Test 3: Memory Pool Optimizer
            Console.WriteLine("\nTest 3: Memory Pool Optimizer");
            TestMemoryPoolOptimizer();
            
            // Test 4: Integration
            Console.WriteLine("\nTest 4: Integration Test");
            TestIntegration();
            
            Console.WriteLine("\n🎉 All Memory Pool Split Tests Passed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Test failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
        
        private static void TestGenericArrayPool()
        {
            // Clear pools
            GenericArrayPool.ClearAllPools();
            
            // Test component array pooling
            var intArray = GenericArrayPool.RentComponentArray<int>(100);
            var stringArray = GenericArrayPool.RentComponentArray<string>(50);
            
            // Fill arrays
            for (int i = 0; i < 100; i++) intArray[i] = i;
            for (int i = 0; i < 50; i++) stringArray[i] = $"test{i}";
            
            // Return arrays
            GenericArrayPool.ReturnComponentArray(intArray);
            GenericArrayPool.ReturnComponentArray(stringArray);
            
            // Test object array pooling
            var objArray1 = GenericArrayPool.RentArray(64);
            var objArray2 = GenericArrayPool.RentArray(128);
            
            // Fill arrays
            for (int i = 0; i < 64; i++) objArray1[i] = i;
            for (int i = 0; i < 128; i++) objArray2[i] = $"object{i}";
            
            // Return arrays
            GenericArrayPool.ReturnArray(objArray1);
            GenericArrayPool.ReturnArray(objArray2);
            
            // Get statistics
            var stats = GenericArrayPool.GetPoolStatistics();
            Console.WriteLine($"  ✓ GenericArrayPool: {stats.typeSpecificPools} type-specific, {stats.sizeSpecificPools} size-specific, {stats.totalPooledArrays} total arrays");
        }
        
        private static void TestChunkPooling()
        {
            // Clear pools
            ChunkPooling.ClearAllChunkPools();
            
            // Create test archetypes
            var componentType1 = ComponentTypeRegistry.Get<int>();
            var componentType2 = ComponentTypeRegistry.Get<string>();
            var componentType3 = ComponentTypeRegistry.Get<float>();
            
            var archetype1 = new Archetype(new[] { componentType1, componentType2 });
            var archetype2 = new Archetype(new[] { componentType1, componentType3 });
            
            // Test chunk rental and return
            var chunk1 = ChunkPooling.RentChunk(archetype1, 512);
            var chunk2 = ChunkPooling.RentChunk(archetype2, 1024);
            
            // Add entities
            var entity1 = new EntityId(1, 1);
            var entity2 = new EntityId(2, 1);
            var entity3 = new EntityId(3, 1);
            
            chunk1.AddEntity(entity1);
            chunk1.AddEntity(entity2);
            chunk2.AddEntity(entity3);
            
            // Set components
            chunk1.SetComponent(0, componentType1, 42);
            chunk1.SetComponent(0, componentType2, "test");
            chunk1.SetComponent(1, componentType1, 100);
            chunk1.SetComponent(1, componentType2, "hello");
            chunk2.SetComponent(0, componentType1, 200);
            chunk2.SetComponent(0, componentType3, 3.14f);
            
            // Verify components
            if (chunk1.GetComponent<int>(0, componentType1) != 42) throw new Exception("Component retrieval failed");
            if (chunk1.GetComponent<string>(0, componentType2) != "test") throw new Exception("Component retrieval failed");
            if (chunk2.GetComponent<float>(0, componentType3) != 3.14f) throw new Exception("Component retrieval failed");
            
            // Return chunks to pool
            ChunkPooling.ReturnChunk(chunk1);
            ChunkPooling.ReturnChunk(chunk2);
            
            // Get statistics
            var stats = ChunkPooling.GetChunkPoolStatistics();
            Console.WriteLine($"  ✓ ChunkPool: {stats.archetypePools} archetype pools, {stats.totalPooledChunks} total chunks, {stats.estimatedMemorySaved} bytes saved");
        }
        
        private static void TestMemoryPoolOptimizer()
        {
            // Clear all pools
            MemoryPoolOptimizer.ClearAllPools();
            
            // Test comprehensive statistics
            var comprehensiveStats = MemoryPoolOptimizer.GetComprehensivePoolStatistics();
            Console.WriteLine($"  ✓ Comprehensive Stats: {comprehensiveStats}");
            
            // Test detailed statistics
            var detailedStats = MemoryPoolOptimizer.GetDetailedPoolStatistics();
            Console.WriteLine($"  ✓ Detailed Stats: {detailedStats.Count} pool types tracked");
            
            // Test memory usage per pool
            var memoryUsage = MemoryPoolOptimizer.GetMemoryUsagePerPool();
            Console.WriteLine($"  ✓ Memory Usage: {memoryUsage.Count} pool types with memory tracking");
        }
        
        private static void TestIntegration()
        {
            // Clear all pools
            MemoryPoolOptimizer.ClearAllPools();
            
            // Create some data to populate pools
            var componentType1 = ComponentTypeRegistry.Get<int>();
            var componentType2 = ComponentTypeRegistry.Get<string>();
            var archetype = new Archetype(new[] { componentType1, componentType2 });
            
            // Use both pool types
            var intArray = GenericArrayPool.RentComponentArray<int>(100);
            var objArray = GenericArrayPool.RentArray(64);
            var chunk = ChunkPooling.RentChunk(archetype, 256);
            
            // Fill with data
            for (int i = 0; i < 100; i++) intArray[i] = i * 2;
            for (int i = 0; i < 64; i++) objArray[i] = $"data{i}";
            
            var entity = new EntityId(1, 1);
            chunk.AddEntity(entity);
            chunk.SetComponent(0, componentType1, 999);
            chunk.SetComponent(0, componentType2, "integration test");
            
            // Return everything
            GenericArrayPool.ReturnComponentArray(intArray);
            GenericArrayPool.ReturnArray(objArray);
            ChunkPooling.ReturnChunk(chunk);
            
            // Verify statistics reflect the usage
            var comprehensiveStats = MemoryPoolOptimizer.GetComprehensivePoolStatistics();
            if (comprehensiveStats.TotalPooledArrays == 0) throw new Exception("Statistics should reflect pooled items");
            
            var detailedStats = MemoryPoolOptimizer.GetDetailedPoolStatistics();
            if (!detailedStats.ContainsKey("GenericArrayPool")) throw new Exception("Should track GenericArrayPool");
            if (!detailedStats.ContainsKey("ChunkPool")) throw new Exception("Should track ChunkPool");
            
            var memoryUsage = MemoryPoolOptimizer.GetMemoryUsagePerPool();
            if (memoryUsage.Count != 2) throw new Exception("Should track memory for both pool types");
            
            Console.WriteLine("  ✓ Integration test passed - all pool types working together");
        }
    }
} 