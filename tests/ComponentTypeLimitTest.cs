using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ECS.Tests
{
    /// <summary>
    /// Test to verify that the ECS system supports more than 64 component types
    /// </summary>
    public class ComponentTypeLimitTest
    {
        public void RunTest()
        {
            Console.WriteLine("🧪 Testing Extended Component Type Limit (256 types)...");
            
            try
            {
                // Test 1: Verify the new limit
                TestComponentTypeLimit();
                
                // Test 2: Test BitSet functionality
                TestBitSetFunctionality();
                
                // Test 3: Test entity creation with many components
                TestEntityCreationWithManyComponents();
                
                // Test 4: Test querying with many components
                TestQueryingWithManyComponents();
                
                // Test 5: Test performance with extended component types
                TestPerformanceWithExtendedTypes();
                
                Console.WriteLine("✅ All component type limit tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Component type limit test failed: {ex.Message}");
                throw;
            }
        }
        
        private void TestComponentTypeLimit()
        {
            Console.WriteLine("  📊 Testing component type limit...");
            
            // Verify the new limit is 256
            var maxTypes = ComponentTypeRegistry.GetMaxComponentTypes();
            if (maxTypes != 256)
            {
                throw new Exception($"Expected max component types to be 256, got {maxTypes}");
            }
            
            Console.WriteLine($"    ✅ Max component types: {maxTypes}");
        }
        
        private void TestBitSetFunctionality()
        {
            Console.WriteLine("  🔧 Testing BitSet functionality...");
            
            var bitSet = new BitSet(256);
            
            // Test setting bits
            bitSet.Set(0);
            bitSet.Set(64);
            bitSet.Set(128);
            bitSet.Set(255);
            
            // Test checking bits
            if (!bitSet.IsSet(0)) throw new Exception("Bit 0 should be set");
            if (!bitSet.IsSet(64)) throw new Exception("Bit 64 should be set");
            if (!bitSet.IsSet(128)) throw new Exception("Bit 128 should be set");
            if (!bitSet.IsSet(255)) throw new Exception("Bit 255 should be set");
            if (bitSet.IsSet(1)) throw new Exception("Bit 1 should not be set");
            
            // Test BitSet operations
            var otherBitSet = new BitSet(256);
            otherBitSet.Set(0);
            otherBitSet.Set(64);
            
            if (!bitSet.HasAll(otherBitSet)) throw new Exception("Should have all bits from other set");
            if (!bitSet.HasAny(otherBitSet)) throw new Exception("Should have any bits from other set");
            
            Console.WriteLine("    ✅ BitSet functionality works correctly");
        }
        
        private void TestEntityCreationWithManyComponents()
        {
            Console.WriteLine("  🏗️  Testing entity creation with many components...");
            
            var entityManager = new EntityManager();
            
            // Create many component types (simulate 100 different component types)
            var componentTypes = new List<ComponentType>();
            for (int i = 0; i < 100; i++)
            {
                // Create a unique component type for each index
                var componentType = ComponentTypeRegistry.Get($"TestComponent{i}");
                componentTypes.Add(componentType);
            }
            
            // Create an entity with many components
            var entity = entityManager.CreateEntity(componentTypes.ToArray());
            
            // Verify the entity was created successfully
            if (!entityManager.HasComponent(entity, componentTypes[0]))
            {
                throw new Exception("Entity should have the first component type");
            }
            
            Console.WriteLine($"    ✅ Created entity with {componentTypes.Count} component types");
        }
        
        private void TestQueryingWithManyComponents()
        {
            Console.WriteLine("  🔍 Testing querying with many components...");
            
            var entityManager = new EntityManager();
            
            // Create component types beyond the old 64 limit
            var componentType1 = ComponentTypeRegistry.Get("ExtendedComponent1");
            var componentType2 = ComponentTypeRegistry.Get("ExtendedComponent2");
            var componentType3 = ComponentTypeRegistry.Get("ExtendedComponent3");
            
            // Create entities with these components
            var entity1 = entityManager.CreateEntity(componentType1, componentType2);
            var entity2 = entityManager.CreateEntity(componentType2, componentType3);
            var entity3 = entityManager.CreateEntity(componentType1, componentType3);
            
            // Test querying with BitSet
            var queryMask = entityManager.CreateComponentMask(componentType1, componentType2);
            var entities = entityManager.GetEntitiesWithComponents(queryMask).ToList();
            
            if (entities.Count != 1 || entities[0] != entity1)
            {
                throw new Exception($"Expected 1 entity with components 1 and 2, got {entities.Count}");
            }
            
            Console.WriteLine("    ✅ Querying with extended component types works correctly");
        }
        
        private void TestPerformanceWithExtendedTypes()
        {
            Console.WriteLine("  ⚡ Testing performance with extended component types...");
            
            var entityManager = new EntityManager();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Create many entities with extended component types
            for (int i = 0; i < 1000; i++)
            {
                var componentType = ComponentTypeRegistry.Get($"PerformanceComponent{i % 100}");
                entityManager.CreateEntity(componentType);
            }
            
            stopwatch.Stop();
            
            var stats = entityManager.GetStatistics();
            Console.WriteLine($"    ✅ Created {stats.totalEntities} entities in {stopwatch.ElapsedMilliseconds}ms");
            
            if (stopwatch.ElapsedMilliseconds > 5000) // 5 seconds max
            {
                throw new Exception($"Performance test took too long: {stopwatch.ElapsedMilliseconds}ms");
            }
        }
    }
    
    // Helper component types for testing
    public struct TestComponent0 { public int Value; }
    public struct TestComponent1 { public int Value; }
    public struct TestComponent2 { public int Value; }
    public struct TestComponent3 { public int Value; }
    public struct TestComponent4 { public int Value; }
    public struct TestComponent5 { public int Value; }
    public struct TestComponent6 { public int Value; }
    public struct TestComponent7 { public int Value; }
    public struct TestComponent8 { public int Value; }
    public struct TestComponent9 { public int Value; }
    
    // Add more test components as needed...
    public struct ExtendedComponent1 { public float Value; }
    public struct ExtendedComponent2 { public string Value; }
    public struct ExtendedComponent3 { public Vector3 Value; }
    
    public struct PerformanceComponent0 { public int Value; }
    public struct PerformanceComponent1 { public float Value; }
    public struct PerformanceComponent2 { public string Value; }
    public struct PerformanceComponent3 { public Vector3 Value; }
    public struct PerformanceComponent4 { public bool Value; }
    public struct PerformanceComponent5 { public double Value; }
    public struct PerformanceComponent6 { public long Value; }
    public struct PerformanceComponent7 { public short Value; }
    public struct PerformanceComponent8 { public byte Value; }
    public struct PerformanceComponent9 { public char Value; }
} 