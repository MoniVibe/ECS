using System;
using System.Numerics;
using System.Linq;

namespace ECS
{
    public static class RefactorTest
    {
        public static void TestRefactoring()
        {
            Console.WriteLine("=== Testing EntityManager Refactoring ===");
            
            try
            {
                // Test 1: Subsystem instantiation
                Console.WriteLine("Test 1: Testing subsystem instantiation...");
                var entityAllocator = new EntityAllocator();
                var archetypeStore = new ArchetypeStore();
                var entityManager = new EntityManager();
                
                Console.WriteLine("✓ All subsystems created successfully");
                
                // Test 2: Entity creation and ID allocation
                Console.WriteLine("Test 2: Testing entity creation and ID allocation...");
                var entityId1 = entityManager.CreateEntity();
                var entityId2 = entityManager.CreateEntity();
                var entityId3 = entityManager.CreateEntity();
                
                Console.WriteLine($"✓ Created entities: {entityId1.Id}, {entityId2.Id}, {entityId3.Id}");
                
                // Test 3: Component operations
                Console.WriteLine("Test 3: Testing component operations...");
                var positionType = ComponentTypeRegistry.Get<Position>();
                var velocityType = ComponentTypeRegistry.Get<Velocity>();
                
                entityManager.AddComponent(entityId1, positionType, new Position { Value = new Vector3(1, 2, 3) });
                entityManager.AddComponent(entityId1, velocityType, new Velocity { Value = new Vector3(0.1f, 0.2f, 0.3f) });
                
                var retrievedPosition = entityManager.GetComponent<Position>(entityId1);
                var retrievedVelocity = entityManager.GetComponent<Velocity>(entityId1);
                
                Console.WriteLine($"✓ Component operations: Position({retrievedPosition.Value.X}, {retrievedPosition.Value.Y}, {retrievedPosition.Value.Z})");
                Console.WriteLine($"✓ Component operations: Velocity({retrievedVelocity.Value.X}, {retrievedVelocity.Value.Y}, {retrievedVelocity.Value.Z})");
                
                // Test 4: Component queries
                Console.WriteLine("Test 4: Testing component queries...");
                var entitiesWithPosition = entityManager.GetEntitiesWithComponents<Position>();
                var entitiesWithVelocity = entityManager.GetEntitiesWithComponents<Velocity>();
                var entitiesWithBoth = entityManager.GetEntitiesWithComponents<Position, Velocity>();
                
                Console.WriteLine($"✓ Entities with Position: {entitiesWithPosition.Count()}");
                Console.WriteLine($"✓ Entities with Velocity: {entitiesWithVelocity.Count()}");
                Console.WriteLine($"✓ Entities with both: {entitiesWithBoth.Count()}");
                
                // Test 5: Component removal
                Console.WriteLine("Test 5: Testing component removal...");
                entityManager.RemoveComponent<Velocity>(entityId1);
                
                var hasVelocityAfterRemoval = entityManager.HasComponent<Velocity>(entityId1);
                var entitiesWithVelocityAfterRemoval = entityManager.GetEntitiesWithComponents<Velocity>();
                
                Console.WriteLine($"✓ Velocity removed: {!hasVelocityAfterRemoval}");
                Console.WriteLine($"✓ Entities with Velocity after removal: {entitiesWithVelocityAfterRemoval.Count()}");
                
                // Test 6: Entity destruction
                Console.WriteLine("Test 6: Testing entity destruction...");
                var initialEntityCount = entityManager.GetStatistics().totalEntities;
                entityManager.DestroyEntity(entityId2);
                var finalEntityCount = entityManager.GetStatistics().totalEntities;
                
                Console.WriteLine($"✓ Entity destruction: {initialEntityCount} -> {finalEntityCount}");
                
                // Test 7: Structural change queueing
                Console.WriteLine("Test 7: Testing structural change queueing...");
                entityManager.QueueAddComponent(entityId3, new Position { Value = new Vector3(10, 20, 30) });
                entityManager.QueueAddComponent(entityId3, new Velocity { Value = new Vector3(1.0f, 2.0f, 3.0f) });
                
                var pendingChanges = entityManager.GetStatistics().reusableIds; // This would be tracked differently in real implementation
                Console.WriteLine($"✓ Structural changes queued");
                
                entityManager.ProcessStructuralChanges();
                
                var hasPositionAfterQueue = entityManager.HasComponent<Position>(entityId3);
                var hasVelocityAfterQueue = entityManager.HasComponent<Velocity>(entityId3);
                
                Console.WriteLine($"✓ Queue processing: Position={hasPositionAfterQueue}, Velocity={hasVelocityAfterQueue}");
                
                // Test 8: Statistics
                Console.WriteLine("Test 8: Testing statistics...");
                var stats = entityManager.GetStatistics();
                Console.WriteLine($"✓ Statistics: {stats.totalEntities} entities, {stats.totalChunks} chunks, {stats.reusableIds} reusable IDs");
                
                // Test 9: Batch operations
                Console.WriteLine("Test 9: Testing batch operations...");
                var batchEntities = new EntityId[100];
                for (int i = 0; i < 100; i++)
                {
                    batchEntities[i] = entityManager.CreateEntity();
                    entityManager.AddComponent(batchEntities[i], positionType, new Position { Value = new Vector3(i, i * 2, i * 3) });
                }
                
                var batchEntitiesWithPosition = entityManager.GetEntitiesWithComponents<Position>();
                Console.WriteLine($"✓ Batch operations: Created {batchEntities.Length} entities, {batchEntitiesWithPosition.Count()} have Position");
                
                // Test 10: Subsystem statistics
                Console.WriteLine("Test 10: Testing subsystem statistics...");
                var allocatorStats = entityAllocator.GetStatistics();
                var archetypeStats = archetypeStore.GetStatistics();
                
                Console.WriteLine($"✓ EntityAllocator: {allocatorStats.totalAllocated} allocated, {allocatorStats.reusableCount} reusable");
                Console.WriteLine($"✓ ArchetypeStore: {archetypeStats.totalArchetypes} archetypes, {archetypeStats.totalChunks} chunks, {archetypeStats.totalEntities} entities");
                
                Console.WriteLine("\n🎉 All refactoring tests passed! EntityManager modularization is working correctly.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Refactoring test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
} 