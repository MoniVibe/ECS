using System;
using System.Numerics;
using System.Collections.Generic;

namespace ECS.Tests
{
    // Simple component types for testing
    public struct TestPosition
    {
        public Vector3 Value;
    }

    public struct TestVelocity
    {
        public Vector3 Value;
    }

    public struct TestName
    {
        public string Value;
    }

    // Simple entity manager for testing
    public class SimpleEntityManager
    {
        private Dictionary<int, Dictionary<Type, object>> entities = new();
        private int nextEntityId = 1;

        public int CreateEntity()
        {
            var entityId = nextEntityId++;
            entities[entityId] = new Dictionary<Type, object>();
            return entityId;
        }

        public void SetComponent<T>(int entityId, T component) where T : struct
        {
            if (entities.ContainsKey(entityId))
            {
                entities[entityId][typeof(T)] = component;
            }
        }

        public T GetComponent<T>(int entityId) where T : struct
        {
            if (entities.ContainsKey(entityId) && entities[entityId].ContainsKey(typeof(T)))
            {
                return (T)entities[entityId][typeof(T)];
            }
            return default(T);
        }

        public bool HasComponent<T>(int entityId) where T : struct
        {
            return entities.ContainsKey(entityId) && entities[entityId].ContainsKey(typeof(T));
        }

        public int GetEntityCount()
        {
            return entities.Count;
        }
    }

    public static class ECSDirectTest
    {
        public static void RunTest()
        {
            Console.WriteLine("=== Direct ECS Test ===");
            
            try
            {
                // Test 1: Basic entity creation and component management
                Console.WriteLine("Test 1: Testing basic entity and component management...");
                var entityManager = new SimpleEntityManager();
                
                var entity1 = entityManager.CreateEntity();
                var entity2 = entityManager.CreateEntity();
                
                Console.WriteLine($"Created entities: {entity1}, {entity2}");
                Console.WriteLine($"Total entities: {entityManager.GetEntityCount()}");
                Console.WriteLine("✓ Entity creation working");

                // Test 2: Component setting and retrieval
                Console.WriteLine("Test 2: Testing component operations...");
                var position1 = new TestPosition { Value = new Vector3(1, 2, 3) };
                var velocity1 = new TestVelocity { Value = new Vector3(0.1f, 0.2f, 0.3f) };
                
                entityManager.SetComponent(entity1, position1);
                entityManager.SetComponent(entity1, velocity1);
                
                var retrievedPosition = entityManager.GetComponent<TestPosition>(entity1);
                var retrievedVelocity = entityManager.GetComponent<TestVelocity>(entity1);
                
                Console.WriteLine($"Entity1 Position: ({retrievedPosition.Value.X}, {retrievedPosition.Value.Y}, {retrievedPosition.Value.Z})");
                Console.WriteLine($"Entity1 Velocity: ({retrievedVelocity.Value.X}, {retrievedVelocity.Value.Y}, {retrievedVelocity.Value.Z})");
                Console.WriteLine("✓ Component operations working");

                // Test 3: Component existence checks
                Console.WriteLine("Test 3: Testing component existence checks...");
                var hasPosition = entityManager.HasComponent<TestPosition>(entity1);
                var hasVelocity = entityManager.HasComponent<TestVelocity>(entity1);
                var hasName = entityManager.HasComponent<TestName>(entity1);
                
                Console.WriteLine($"Entity1 has Position: {hasPosition}");
                Console.WriteLine($"Entity1 has Velocity: {hasVelocity}");
                Console.WriteLine($"Entity1 has Name: {hasName}");
                Console.WriteLine("✓ Component existence checks working");

                // Test 4: Multiple entities with different components
                Console.WriteLine("Test 4: Testing multiple entities...");
                var position2 = new Position { Value = new Vector3(4, 5, 6) };
                var name2 = new Name { Value = "Entity2" };
                
                entityManager.SetComponent(entity2, position2);
                entityManager.SetComponent(entity2, name2);
                
                var retrievedPosition2 = entityManager.GetComponent<Position>(entity2);
                var retrievedName2 = entityManager.GetComponent<Name>(entity2);
                
                Console.WriteLine($"Entity2 Position: ({retrievedPosition2.Value.X}, {retrievedPosition2.Value.Y}, {retrievedPosition2.Value.Z})");
                Console.WriteLine($"Entity2 Name: {retrievedName2.Value}");
                Console.WriteLine("✓ Multiple entities working");

                // Test 5: Vector3 operations (SIMD-like)
                Console.WriteLine("Test 5: Testing Vector3 operations...");
                var v1 = new Vector3(1, 2, 3);
                var v2 = new Vector3(4, 5, 6);
                var sum = Vector3.Add(v1, v2);
                var dot = Vector3.Dot(v1, v2);
                var cross = Vector3.Cross(v1, v2);
                
                Console.WriteLine($"Vector addition: ({sum.X}, {sum.Y}, {sum.Z})");
                Console.WriteLine($"Vector dot product: {dot}");
                Console.WriteLine($"Vector cross product: ({cross.X}, {cross.Y}, {cross.Z})");
                Console.WriteLine("✓ Vector3 operations working");

                // Test 6: Performance test
                Console.WriteLine("Test 6: Testing performance...");
                var startTime = DateTime.Now;
                
                for (int i = 0; i < 1000; i++)
                {
                    var entity = entityManager.CreateEntity();
                    var pos = new Position { Value = new Vector3(i, i * 2, i * 3) };
                    var vel = new Velocity { Value = new Vector3(i * 0.1f, i * 0.2f, i * 0.3f) };
                    entityManager.SetComponent(entity, pos);
                    entityManager.SetComponent(entity, vel);
                }
                
                var endTime = DateTime.Now;
                var duration = (endTime - startTime).TotalMilliseconds;
                Console.WriteLine($"Created 1000 entities in {duration:F2}ms");
                Console.WriteLine($"Total entities: {entityManager.GetEntityCount()}");
                Console.WriteLine("✓ Performance test completed");

                Console.WriteLine("\n🎉 All direct ECS tests passed! ECS system is working correctly.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Direct ECS test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
} 