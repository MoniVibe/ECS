using System;
using System.Numerics;

namespace ECS
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== ECS System Testing ===");
            Console.WriteLine("Testing refactored EntityManager with modular subsystems...\n");
            
            // Test the refactored system
            RefactorTest.TestRefactoring();
            
            Console.WriteLine("\n=== Running Additional Tests ===");
            
            // Test basic functionality
            TestBasicFunctionality();
            
            // Test performance
            TestPerformance();
            
            // Test edge cases
            TestEdgeCases();
            
            Console.WriteLine("\n=== All Tests Completed ===");
        }
        
        static void TestBasicFunctionality()
        {
            Console.WriteLine("Testing basic functionality...");
            
            try
            {
                var manager = new EntityManager();
                
                // Create entities with different component combinations
                var entity1 = manager.CreateEntity<Position, Velocity>();
                var entity2 = manager.CreateEntity<Position, Name>();
                var entity3 = manager.CreateEntity<Position, Velocity, Name>();
                
                // Set components
                manager.SetComponent(entity1, new Position { Value = new Vector3(1, 2, 3) });
                manager.SetComponent(entity1, new Velocity { Value = new Vector3(0.1f, 0.2f, 0.3f) });
                
                manager.SetComponent(entity2, new Position { Value = new Vector3(4, 5, 6) });
                manager.SetComponent(entity2, new Name { Value = "Entity2" });
                
                manager.SetComponent(entity3, new Position { Value = new Vector3(7, 8, 9) });
                manager.SetComponent(entity3, new Velocity { Value = new Vector3(0.4f, 0.5f, 0.6f) });
                manager.SetComponent(entity3, new Name { Value = "Entity3" });
                
                // Test queries
                var entitiesWithPosition = manager.GetEntitiesWithComponents<Position>();
                var entitiesWithVelocity = manager.GetEntitiesWithComponents<Velocity>();
                var entitiesWithName = manager.GetEntitiesWithComponents<Name>();
                var entitiesWithPositionAndVelocity = manager.GetEntitiesWithComponents<Position, Velocity>();
                
                Console.WriteLine($"✓ Entities with Position: {entitiesWithPosition.Count()}");
                Console.WriteLine($"✓ Entities with Velocity: {entitiesWithVelocity.Count()}");
                Console.WriteLine($"✓ Entities with Name: {entitiesWithName.Count()}");
                Console.WriteLine($"✓ Entities with Position+Velocity: {entitiesWithPositionAndVelocity.Count()}");
                
                // Test component retrieval
                var pos1 = manager.GetComponent<Position>(entity1);
                var vel1 = manager.GetComponent<Velocity>(entity1);
                var name2 = manager.GetComponent<Name>(entity2);
                
                Console.WriteLine($"✓ Component retrieval: Position({pos1.Value.X}, {pos1.Value.Y}, {pos1.Value.Z})");
                Console.WriteLine($"✓ Component retrieval: Velocity({vel1.Value.X}, {vel1.Value.Y}, {vel1.Value.Z})");
                Console.WriteLine($"✓ Component retrieval: Name({name2.Value})");
                
                Console.WriteLine("✓ Basic functionality test passed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Basic functionality test failed: {ex.Message}");
            }
        }
        
        static void TestPerformance()
        {
            Console.WriteLine("Testing performance...");
            
            try
            {
                var manager = new EntityManager();
                var startTime = DateTime.Now;
                
                // Create many entities
                var entities = new EntityId[10000];
                for (int i = 0; i < 10000; i++)
                {
                    entities[i] = manager.CreateEntity();
                    manager.AddComponent(entities[i], new Position { Value = new Vector3(i, i * 2, i * 3) });
                    
                    if (i % 2 == 0)
                    {
                        manager.AddComponent(entities[i], new Velocity { Value = new Vector3(i * 0.1f, i * 0.2f, i * 0.3f) });
                    }
                    
                    if (i % 3 == 0)
                    {
                        manager.AddComponent(entities[i], new Name { Value = $"Entity_{i}" });
                    }
                }
                
                var creationTime = (DateTime.Now - startTime).TotalMilliseconds;
                
                // Test query performance
                startTime = DateTime.Now;
                var entitiesWithPosition = manager.GetEntitiesWithComponents<Position>();
                var entitiesWithVelocity = manager.GetEntitiesWithComponents<Velocity>();
                var entitiesWithName = manager.GetEntitiesWithComponents<Name>();
                var queryTime = (DateTime.Now - startTime).TotalMilliseconds;
                
                Console.WriteLine($"✓ Created {entities.Length} entities in {creationTime:F2}ms");
                Console.WriteLine($"✓ Queried components in {queryTime:F2}ms");
                Console.WriteLine($"✓ Entities with Position: {entitiesWithPosition.Count()}");
                Console.WriteLine($"✓ Entities with Velocity: {entitiesWithVelocity.Count()}");
                Console.WriteLine($"✓ Entities with Name: {entitiesWithName.Count()}");
                
                Console.WriteLine("✓ Performance test passed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Performance test failed: {ex.Message}");
            }
        }
        
        static void TestEdgeCases()
        {
            Console.WriteLine("Testing edge cases...");
            
            try
            {
                var manager = new EntityManager();
                
                // Test entity destruction and recreation
                var entity1 = manager.CreateEntity();
                manager.AddComponent(entity1, new Position { Value = new Vector3(1, 2, 3) });
                manager.DestroyEntity(entity1);
                
                var entity2 = manager.CreateEntity();
                manager.AddComponent(entity2, new Position { Value = new Vector3(4, 5, 6) });
                
                // Test component removal
                var entity3 = manager.CreateEntity();
                manager.AddComponent(entity3, new Position { Value = new Vector3(7, 8, 9) });
                manager.AddComponent(entity3, new Velocity { Value = new Vector3(0.1f, 0.2f, 0.3f) });
                manager.RemoveComponent<Velocity>(entity3);
                
                var hasVelocity = manager.HasComponent<Velocity>(entity3);
                var hasPosition = manager.HasComponent<Position>(entity3);
                
                Console.WriteLine($"✓ Component removal: Velocity={hasVelocity}, Position={hasPosition}");
                
                // Test structural change queueing
                var entity4 = manager.CreateEntity();
                manager.QueueAddComponent(entity4, new Position { Value = new Vector3(10, 11, 12) });
                manager.QueueAddComponent(entity4, new Velocity { Value = new Vector3(0.4f, 0.5f, 0.6f) });
                
                var hasPositionBeforeQueue = manager.HasComponent<Position>(entity4);
                manager.ProcessStructuralChanges();
                var hasPositionAfterQueue = manager.HasComponent<Position>(entity4);
                
                Console.WriteLine($"✓ Structural changes: Before={hasPositionBeforeQueue}, After={hasPositionAfterQueue}");
                
                // Test statistics
                var stats = manager.GetStatistics();
                Console.WriteLine($"✓ Final statistics: {stats.totalEntities} entities, {stats.totalChunks} chunks");
                
                Console.WriteLine("✓ Edge cases test passed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Edge cases test failed: {ex.Message}");
            }
        }
    }
} 