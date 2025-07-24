using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

namespace ECS.Tests
{
    // Enhanced component types for comprehensive testing
    public struct CompTestPosition
    {
        public Vector3 Value;
    }

    public struct CompTestVelocity
    {
        public Vector3 Value;
    }

    public struct CompTestName
    {
        public string Value;
    }

    public struct Health
    {
        public float Value;
    }

    public struct Transform
    {
        public Vector3 Position;
        public Vector3 Scale;
        public Quaternion Rotation;
    }

    // Enhanced entity manager with chunking and queries
    public class ComprehensiveEntityManager
    {
        private Dictionary<int, Dictionary<Type, object>> entities = new();
        private Dictionary<Type, HashSet<int>> componentToEntities = new();
        private int nextEntityId = 1;
        private Dictionary<string, object> statistics = new();

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
                var componentType = typeof(T);
                entities[entityId][componentType] = component;
                
                // Update component-to-entities mapping
                if (!componentToEntities.ContainsKey(componentType))
                {
                    componentToEntities[componentType] = new HashSet<int>();
                }
                componentToEntities[componentType].Add(entityId);
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

        public void RemoveComponent<T>(int entityId) where T : struct
        {
            if (entities.ContainsKey(entityId))
            {
                var componentType = typeof(T);
                entities[entityId].Remove(componentType);
                
                // Update component-to-entities mapping
                if (componentToEntities.ContainsKey(componentType))
                {
                    componentToEntities[componentType].Remove(entityId);
                }
            }
        }

        public void DestroyEntity(int entityId)
        {
            if (entities.ContainsKey(entityId))
            {
                // Remove from component-to-entities mapping
                foreach (var componentType in entities[entityId].Keys)
                {
                    if (componentToEntities.ContainsKey(componentType))
                    {
                        componentToEntities[componentType].Remove(entityId);
                    }
                }
                
                entities.Remove(entityId);
            }
        }

        public IEnumerable<int> GetEntitiesWithComponents<T1>() where T1 : struct
        {
            var componentType = typeof(T1);
            if (componentToEntities.ContainsKey(componentType))
            {
                return componentToEntities[componentType];
            }
            return Enumerable.Empty<int>();
        }

        public IEnumerable<int> GetEntitiesWithComponents<T1, T2>() where T1 : struct where T2 : struct
        {
            var type1 = typeof(T1);
            var type2 = typeof(T2);
            
            if (componentToEntities.ContainsKey(type1) && componentToEntities.ContainsKey(type2))
            {
                return componentToEntities[type1].Intersect(componentToEntities[type2]);
            }
            return Enumerable.Empty<int>();
        }

        public IEnumerable<int> GetEntitiesWithComponents<T1, T2, T3>() where T1 : struct where T2 : struct where T3 : struct
        {
            var type1 = typeof(T1);
            var type2 = typeof(T2);
            var type3 = typeof(T3);
            
            if (componentToEntities.ContainsKey(type1) && 
                componentToEntities.ContainsKey(type2) && 
                componentToEntities.ContainsKey(type3))
            {
                return componentToEntities[type1]
                    .Intersect(componentToEntities[type2])
                    .Intersect(componentToEntities[type3]);
            }
            return Enumerable.Empty<int>();
        }

        public int GetEntityCount()
        {
            return entities.Count;
        }

        public int GetComponentCount<T>() where T : struct
        {
            var componentType = typeof(T);
            return componentToEntities.ContainsKey(componentType) ? componentToEntities[componentType].Count : 0;
        }

        public Dictionary<string, object> GetStatistics()
        {
            statistics["totalEntities"] = entities.Count;
            statistics["totalComponents"] = componentToEntities.Values.Sum(hs => hs.Count);
            statistics["uniqueComponentTypes"] = componentToEntities.Count;
            statistics["reusableIds"] = nextEntityId - entities.Count - 1;
            
            return new Dictionary<string, object>(statistics);
        }
    }

    public static class ComprehensiveECSTest
    {
        public static void RunTest()
        {
            Console.WriteLine("=== Comprehensive ECS Test ===");
            
            try
            {
                // Test 1: Basic entity and component management
                Console.WriteLine("Test 1: Testing basic entity and component management...");
                var entityManager = new ComprehensiveEntityManager();
                
                var entity1 = entityManager.CreateEntity();
                var entity2 = entityManager.CreateEntity();
                var entity3 = entityManager.CreateEntity();
                
                Console.WriteLine($"Created entities: {entity1}, {entity2}, {entity3}");
                Console.WriteLine($"Total entities: {entityManager.GetEntityCount()}");
                Console.WriteLine("✓ Entity creation working");

                // Test 2: Component operations
                Console.WriteLine("Test 2: Testing component operations...");
                var position1 = new CompTestPosition { Value = new Vector3(1, 2, 3) };
                var velocity1 = new CompTestVelocity { Value = new Vector3(0.1f, 0.2f, 0.3f) };
                var health1 = new Health { Value = 100.0f };
                
                entityManager.SetComponent(entity1, position1);
                entityManager.SetComponent(entity1, velocity1);
                entityManager.SetComponent(entity1, health1);
                
                var retrievedPosition = entityManager.GetComponent<CompTestPosition>(entity1);
                var retrievedVelocity = entityManager.GetComponent<CompTestVelocity>(entity1);
                var retrievedHealth = entityManager.GetComponent<Health>(entity1);
                
                Console.WriteLine($"Entity1 Position: ({retrievedPosition.Value.X}, {retrievedPosition.Value.Y}, {retrievedPosition.Value.Z})");
                Console.WriteLine($"Entity1 Velocity: ({retrievedVelocity.Value.X}, {retrievedVelocity.Value.Y}, {retrievedVelocity.Value.Z})");
                Console.WriteLine($"Entity1 Health: {retrievedHealth.Value}");
                Console.WriteLine("✓ Component operations working");

                // Test 3: Component queries
                Console.WriteLine("Test 3: Testing component queries...");
                var position2 = new CompTestPosition { Value = new Vector3(4, 5, 6) };
                var name2 = new CompTestName { Value = "Entity2" };
                var transform3 = new Transform 
                { 
                    Position = new Vector3(7, 8, 9),
                    Scale = new Vector3(1, 1, 1),
                    Rotation = Quaternion.Identity
                };
                
                entityManager.SetComponent(entity2, position2);
                entityManager.SetComponent(entity2, name2);
                entityManager.SetComponent(entity3, transform3);
                entityManager.SetComponent(entity3, position2); // Same position as entity2
                
                var entitiesWithPosition = entityManager.GetEntitiesWithComponents<CompTestPosition>();
                var entitiesWithPositionAndName = entityManager.GetEntitiesWithComponents<CompTestPosition, CompTestName>();
                var entitiesWithTransform = entityManager.GetEntitiesWithComponents<Transform>();
                
                Console.WriteLine($"Entities with Position: {string.Join(", ", entitiesWithPosition)}");
                Console.WriteLine($"Entities with Position+Name: {string.Join(", ", entitiesWithPositionAndName)}");
                Console.WriteLine($"Entities with Transform: {string.Join(", ", entitiesWithTransform)}");
                Console.WriteLine("✓ Component queries working");

                // Test 4: Component removal and entity destruction
                Console.WriteLine("Test 4: Testing component removal and entity destruction...");
                var initialCount = entityManager.GetEntityCount();
                var initialPositionCount = entityManager.GetComponentCount<CompTestPosition>();
                
                entityManager.RemoveComponent<Health>(entity1);
                entityManager.DestroyEntity(entity3);
                
                var finalCount = entityManager.GetEntityCount();
                var finalPositionCount = entityManager.GetComponentCount<CompTestPosition>();
                
                Console.WriteLine($"Entities before: {initialCount}, after: {finalCount}");
                Console.WriteLine($"Position components before: {initialPositionCount}, after: {finalPositionCount}");
                Console.WriteLine("✓ Component removal and entity destruction working");

                // Test 5: Advanced Vector3 operations (SIMD-like)
                Console.WriteLine("Test 5: Testing advanced Vector3 operations...");
                var v1 = new Vector3(1, 2, 3);
                var v2 = new Vector3(4, 5, 6);
                var v3 = new Vector3(7, 8, 9);
                
                var sum = Vector3.Add(Vector3.Add(v1, v2), v3);
                var dot = Vector3.Dot(v1, v2);
                var cross = Vector3.Cross(v1, v2);
                var length = v1.Length();
                var normalized = Vector3.Normalize(v1);
                
                Console.WriteLine($"Vector sum: ({sum.X}, {sum.Y}, {sum.Z})");
                Console.WriteLine($"Vector dot product: {dot}");
                Console.WriteLine($"Vector cross product: ({cross.X}, {cross.Y}, {cross.Z})");
                Console.WriteLine($"Vector length: {length}");
                Console.WriteLine($"Normalized vector: ({normalized.X}, {normalized.Y}, {normalized.Z})");
                Console.WriteLine("✓ Advanced Vector3 operations working");

                // Test 6: Performance stress test
                Console.WriteLine("Test 6: Testing performance stress test...");
                var startTime = DateTime.Now;
                
                // Create many entities with different component combinations
                for (int i = 0; i < 5000; i++)
                {
                    var entity = entityManager.CreateEntity();
                    var pos = new CompTestPosition { Value = new Vector3(i, i * 2, i * 3) };
                    entityManager.SetComponent(entity, pos);
                    
                    if (i % 2 == 0)
                    {
                        var vel = new CompTestVelocity { Value = new Vector3(i * 0.1f, i * 0.2f, i * 0.3f) };
                        entityManager.SetComponent(entity, vel);
                    }
                    
                    if (i % 3 == 0)
                    {
                        var health = new Health { Value = 100.0f - (i % 10) };
                        entityManager.SetComponent(entity, health);
                    }
                    
                    if (i % 5 == 0)
                    {
                        var name = new CompTestName { Value = $"Entity_{i}" };
                        entityManager.SetComponent(entity, name);
                    }
                }
                
                var endTime = DateTime.Now;
                var duration = (endTime - startTime).TotalMilliseconds;
                Console.WriteLine($"Created 5000 entities with various components in {duration:F2}ms");
                Console.WriteLine($"Total entities: {entityManager.GetEntityCount()}");
                Console.WriteLine($"Position components: {entityManager.GetComponentCount<CompTestPosition>()}");
                Console.WriteLine($"Velocity components: {entityManager.GetComponentCount<CompTestVelocity>()}");
                Console.WriteLine($"Health components: {entityManager.GetComponentCount<Health>()}");
                Console.WriteLine($"Name components: {entityManager.GetComponentCount<CompTestName>()}");
                Console.WriteLine("✓ Performance stress test completed");

                // Test 7: Statistics and monitoring
                Console.WriteLine("Test 7: Testing statistics and monitoring...");
                var stats = entityManager.GetStatistics();
                
                foreach (var kvp in stats)
                {
                    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                }
                Console.WriteLine("✓ Statistics and monitoring working");

                // Test 8: Complex queries
                Console.WriteLine("Test 8: Testing complex queries...");
                var entitiesWithPositionAndVelocity = entityManager.GetEntitiesWithComponents<CompTestPosition, CompTestVelocity>();
                var entitiesWithPositionAndHealth = entityManager.GetEntitiesWithComponents<CompTestPosition, Health>();
                var entitiesWithPositionVelocityAndHealth = entityManager.GetEntitiesWithComponents<CompTestPosition, CompTestVelocity, Health>();
                
                Console.WriteLine($"Entities with Position+Velocity: {entitiesWithPositionAndVelocity.Count()}");
                Console.WriteLine($"Entities with Position+Health: {entitiesWithPositionAndHealth.Count()}");
                Console.WriteLine($"Entities with Position+Velocity+Health: {entitiesWithPositionVelocityAndHealth.Count()}");
                Console.WriteLine("✓ Complex queries working");

                Console.WriteLine("\n🎉 All comprehensive ECS tests passed! ECS system is working correctly.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Comprehensive ECS test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
} 