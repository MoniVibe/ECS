using System;
using System.Numerics;

namespace ECS
{
    /// <summary>
    /// Basic ECS example demonstrating simple entity creation, component management, and queries
    /// </summary>
    public static class BasicExample
    {
        public static void RunExample()
        {
            Console.WriteLine("=== Basic ECS Example ===");
            
            var entityManager = new EntityManager();

            // Create entities with different component combinations
            var player = entityManager.CreateEntity<Position, Velocity, Name>();
            var enemy = entityManager.CreateEntity<Position, Velocity, Name, Health>();
            var item = entityManager.CreateEntity<Position, Name>();

            // Set component values
            entityManager.SetComponent(player, new Position { Value = new Vector3(0, 0, 0) });
            entityManager.SetComponent(player, new Velocity { Value = new Vector3(1, 0, 0) });
            entityManager.SetComponent(player, new Name { Value = "Player" });

            entityManager.SetComponent(enemy, new Position { Value = new Vector3(10, 0, 0) });
            entityManager.SetComponent(enemy, new Velocity { Value = new Vector3(-0.5f, 0, 0) });
            entityManager.SetComponent(enemy, new Name { Value = "Enemy" });
            entityManager.SetComponent(enemy, new Health { Value = 100 });

            entityManager.SetComponent(item, new Position { Value = new Vector3(5, 0, 0) });
            entityManager.SetComponent(item, new Name { Value = "Health Potion" });

            // Simple movement logic
            Console.WriteLine("Applying movement logic...");
            ApplyMovement(entityManager);

            // Query entities with different component combinations
            Console.WriteLine("\nQuerying entities:");
            
            var movingEntities = entityManager.GetEntitiesWithComponents<Position, Velocity>();
            Console.WriteLine($"Moving entities: {movingEntities.Count()}");

            var namedEntities = entityManager.GetEntitiesWithComponents<Name>();
            Console.WriteLine($"Named entities: {namedEntities.Count()}");

            var healthyEntities = entityManager.GetEntitiesWithComponents<Health>();
            Console.WriteLine($"Healthy entities: {healthyEntities.Count()}");

            // Display entity details
            Console.WriteLine("\nEntity details:");
            foreach (var entity in entityManager.GetAllEntities())
            {
                var name = entityManager.GetComponent<Name>(entity);
                var position = entityManager.GetComponent<Position>(entity);
                
                Console.WriteLine($"Entity {entity.Id} ({name.Value}): Position({position.Value.X:F1}, {position.Value.Y:F1}, {position.Value.Z:F1})");
                
                if (entityManager.HasComponent<Velocity>(entity))
                {
                    var velocity = entityManager.GetComponent<Velocity>(entity);
                    Console.WriteLine($"  Velocity: ({velocity.Value.X:F1}, {velocity.Value.Y:F1}, {velocity.Value.Z:F1})");
                }
                
                if (entityManager.HasComponent<Health>(entity))
                {
                    var health = entityManager.GetComponent<Health>(entity);
                    Console.WriteLine($"  Health: {health.Value}");
                }
            }

            // Get system statistics
            var stats = entityManager.GetStatistics();
            Console.WriteLine($"\nSystem stats: {stats.totalEntities} entities, {stats.totalChunks} chunks");
        }

        /// <summary>
        /// Simple movement system that updates positions based on velocities
        /// </summary>
        private static void ApplyMovement(EntityManager entityManager)
        {
            foreach (var entity in entityManager.GetEntitiesWithComponents<Position, Velocity>())
            {
                var position = entityManager.GetComponent<Position>(entity);
                var velocity = entityManager.GetComponent<Velocity>(entity);

                // Update position based on velocity
                position.Value.X += velocity.Value.X;
                position.Value.Y += velocity.Value.Y;
                position.Value.Z += velocity.Value.Z;

                // Update the component
                entityManager.SetComponent(entity, position);
            }
        }
    }
} 