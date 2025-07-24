using System;
using System.Numerics;

namespace ECS
{
    public static class SimpleTest
    {
        public static void RunTest()
        {
            Console.WriteLine("=== Simple ECS Test ===");
            
            try
            {
                var manager = new EntityManager();
                
                // Create entities
                var entity1 = manager.CreateEntity();
                var entity2 = manager.CreateEntity();
                
                // Add components
                manager.AddComponent(entity1, new Position { Value = new Vector3(1, 2, 3) });
                manager.AddComponent(entity1, new Velocity { Value = new Vector3(0.1f, 0.2f, 0.3f) });
                
                manager.AddComponent(entity2, new Position { Value = new Vector3(4, 5, 6) });
                manager.AddComponent(entity2, new Name { Value = "TestEntity" });
                
                // Test queries
                var entitiesWithPosition = manager.GetEntitiesWithComponents<Position>();
                var entitiesWithVelocity = manager.GetEntitiesWithComponents<Velocity>();
                var entitiesWithName = manager.GetEntitiesWithComponents<Name>();
                
                Console.WriteLine($"✓ Entities with Position: {entitiesWithPosition.Count()}");
                Console.WriteLine($"✓ Entities with Velocity: {entitiesWithVelocity.Count()}");
                Console.WriteLine($"✓ Entities with Name: {entitiesWithName.Count()}");
                
                // Test component retrieval
                var pos1 = manager.GetComponent<Position>(entity1);
                var vel1 = manager.GetComponent<Velocity>(entity1);
                var name2 = manager.GetComponent<Name>(entity2);
                
                Console.WriteLine($"✓ Position: ({pos1.Value.X}, {pos1.Value.Y}, {pos1.Value.Z})");
                Console.WriteLine($"✓ Velocity: ({vel1.Value.X}, {vel1.Value.Y}, {vel1.Value.Z})");
                Console.WriteLine($"✓ Name: {name2.Value}");
                
                Console.WriteLine("✓ Simple test passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Simple test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
} 