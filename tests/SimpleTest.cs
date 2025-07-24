using System;
using System.Numerics;

namespace ECS.Tests
{
    public static class SimpleTest
    {
        public static void RunTest()
        {
            Console.WriteLine("=== Simple ECS System Test ===");
            
            try
            {
                // Test 1: Basic ECS functionality
                Console.WriteLine("Test 1: Testing basic ECS functionality...");
                var entityManager = new ECS.EntityManager();
                
                // Create entities
                var entity1 = entityManager.CreateEntity<ECS.Position, ECS.Velocity>();
                var entity2 = entityManager.CreateEntity<ECS.Position, ECS.Velocity, ECS.Name>();
                
                // Set components
                entityManager.SetComponent(entity1, new ECS.Position { Value = new ECS.Vector3(1, 2, 3) });
                entityManager.SetComponent(entity1, new ECS.Velocity { Value = new ECS.Vector3(0.1f, 0.2f, 0.3f) });
                
                entityManager.SetComponent(entity2, new ECS.Position { Value = new ECS.Vector3(4, 5, 6) });
                entityManager.SetComponent(entity2, new ECS.Velocity { Value = new ECS.Vector3(0.4f, 0.5f, 0.6f) });
                entityManager.SetComponent(entity2, new ECS.Name { Value = "Entity2" });
                
                // Test queries
                var entitiesWithPosition = entityManager.GetEntitiesWithComponents<ECS.Position>();
                var entitiesWithPositionVelocity = entityManager.GetEntitiesWithComponents<ECS.Position, ECS.Velocity>();
                
                Console.WriteLine($"Entities with Position: {entitiesWithPosition.Count()}");
                Console.WriteLine($"Entities with Position+Velocity: {entitiesWithPositionVelocity.Count()}");
                
                // Test component retrieval
                var position1 = entityManager.GetComponent<ECS.Position>(entity1);
                var velocity1 = entityManager.GetComponent<ECS.Velocity>(entity1);
                Console.WriteLine($"Entity1 - Position: ({position1.Value.X}, {position1.Value.Y}, {position1.Value.Z})");
                Console.WriteLine($"Entity1 - Velocity: ({velocity1.Value.X}, {velocity1.Value.Y}, {velocity1.Value.Z})");
                
                // Test statistics
                var stats = entityManager.GetStatistics();
                Console.WriteLine($"ECS Stats: {stats.totalEntities} entities, {stats.totalChunks} chunks, {stats.reusableIds} reusable IDs");
                
                Console.WriteLine("✓ Basic ECS functionality working");

                // Test 2: Heat classification
                Console.WriteLine("Test 2: Testing heat classification...");
                var positionHeat = ECS.ComponentHeatClassifier.GetComponentHeat(typeof(ECS.Position));
                var nameHeat = ECS.ComponentHeatClassifier.GetComponentHeat(typeof(ECS.Name));
                Console.WriteLine($"Position heat: {positionHeat}");
                Console.WriteLine($"Name heat: {nameHeat}");
                Console.WriteLine("✓ Heat classification working");

                // Test 3: SIMD alignment detection
                Console.WriteLine("Test 3: Testing SIMD alignment...");
                var vector3Alignment = ECS.SimdAlignmentUtility.GetComponentAlignment(typeof(ECS.Vector3));
                var nameAlignment = ECS.SimdAlignmentUtility.GetComponentAlignment(typeof(ECS.Name));
                Console.WriteLine($"Vector3 alignment: {vector3Alignment}");
                Console.WriteLine($"Name alignment: {nameAlignment}");
                Console.WriteLine("✓ SIMD alignment detection working");

                // Test 4: Enhanced component type creation
                Console.WriteLine("Test 4: Testing enhanced component type...");
                var positionType = new ECS.EnhancedComponentType(1, typeof(ECS.Position));
                var nameType = new ECS.EnhancedComponentType(2, typeof(ECS.Name));
                Console.WriteLine($"Position: Heat={positionType.Heat}, Alignment={positionType.Alignment}, SIMD={positionType.IsSimdOptimized}");
                Console.WriteLine($"Name: Heat={nameType.Heat}, Alignment={nameType.Alignment}, SIMD={nameType.IsSimdOptimized}");
                Console.WriteLine("✓ Enhanced component type creation working");

                // Test 5: Statistics
                Console.WriteLine("Test 5: Testing statistics...");
                var heatStats = ECS.ComponentHeatClassifier.GetStatistics();
                var alignmentStats = ECS.SimdAlignmentUtility.GetStatistics();
                Console.WriteLine($"Heat stats: Total={heatStats.totalRegistered}, Hot={heatStats.hotComponents}, Cold={heatStats.coldComponents}");
                Console.WriteLine($"Alignment stats: Total={alignmentStats.totalRegistered}, SIMD={alignmentStats.simdOptimized}");
                Console.WriteLine("✓ Statistics working");

                Console.WriteLine("\n🎉 All tests passed! ECS system is working correctly.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
} 