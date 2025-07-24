using System;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic; // Added for List
using System.Linq; // Added for ToList

namespace ECS
{
    /// <summary>
    /// Parallel processing ECS example demonstrating concurrent entity processing
    /// </summary>
    public static class ParallelExample
    {
        public static void RunExample()
        {
            Console.WriteLine("=== Parallel Processing ECS Example ===");
            
            var entityManager = new EntityManager();

            // Create entities for parallel processing
            Console.WriteLine("Creating entities for parallel processing...");
            for (int i = 0; i < 50; i++)
            {
                var entity = entityManager.CreateEntity<Position, Velocity, Name>();
                entityManager.SetComponent(entity, new Position { Value = new Vector3(i, 0, 0) });
                entityManager.SetComponent(entity, new Velocity { Value = new Vector3(0.1f, 0, 0) });
                entityManager.SetComponent(entity, new Name { Value = $"Entity_{i}" });
            }

            Console.WriteLine($"Created {entityManager.GetStatistics().totalEntities} entities");

            // Show initial state
            Console.WriteLine("\nInitial state (first 5 entities):");
            ShowEntityDetails(entityManager, 5);

            // Parallel movement processing
            Console.WriteLine("\nApplying parallel movement processing...");
            ApplyParallelMovement(entityManager);

            Console.WriteLine("\nAfter parallel movement (first 5 entities):");
            ShowEntityDetails(entityManager, 5);

            // Parallel physics simulation
            Console.WriteLine("\nApplying parallel physics simulation...");
            ApplyParallelPhysics(entityManager);

            Console.WriteLine("\nAfter parallel physics (first 5 entities):");
            ShowEntityDetails(entityManager, 5);

            // Demonstrate parallel queries
            Console.WriteLine("\nPerforming parallel queries...");
            PerformParallelQueries(entityManager);

            // Performance metrics
            Console.WriteLine("\nPerformance metrics:");
            var stats = entityManager.GetStatistics();
            Console.WriteLine($"Total entities: {stats.totalEntities}");
            Console.WriteLine($"Total chunks: {stats.totalChunks}");
        }

        /// <summary>
        /// Apply movement processing in parallel
        /// </summary>
        private static void ApplyParallelMovement(EntityManager entityManager)
        {
            var entities = entityManager.GetEntitiesWithComponents<Position, Velocity>().ToList();
            
            // Process entities in parallel
            Parallel.ForEach(entities, entity =>
            {
                var position = entityManager.GetComponent<Position>(entity);
                var velocity = entityManager.GetComponent<Velocity>(entity);

                // Update position based on velocity
                position.Value.X += velocity.Value.X;
                position.Value.Y += velocity.Value.Y;
                position.Value.Z += velocity.Value.Z;

                // Update the component
                entityManager.SetComponent(entity, position);
            });
        }

        /// <summary>
        /// Apply physics simulation in parallel
        /// </summary>
        private static void ApplyParallelPhysics(EntityManager entityManager)
        {
            var entities = entityManager.GetEntitiesWithComponents<Position, Velocity>().ToList();
            
            // Process entities in parallel with more complex physics
            Parallel.ForEach(entities, entity =>
            {
                var position = entityManager.GetComponent<Position>(entity);
                var velocity = entityManager.GetComponent<Velocity>(entity);

                // Apply gravity
                velocity.Value.Y -= 0.01f;

                // Apply air resistance
                velocity.Value.X *= 0.99f;
                velocity.Value.Y *= 0.99f;
                velocity.Value.Z *= 0.99f;

                // Update position
                position.Value.X += velocity.Value.X;
                position.Value.Y += velocity.Value.Y;
                position.Value.Z += velocity.Value.Z;

                // Bounce off ground
                if (position.Value.Y < 0)
                {
                    position.Value.Y = 0;
                    velocity.Value.Y = -velocity.Value.Y * 0.8f;
                }

                // Update components
                entityManager.SetComponent(entity, position);
                entityManager.SetComponent(entity, velocity);
            });
        }

        /// <summary>
        /// Perform parallel queries on the entity manager
        /// </summary>
        private static void PerformParallelQueries(EntityManager entityManager)
        {
            // Parallel query for entities with position and velocity
            var movingEntities = entityManager.GetEntitiesWithComponents<Position, Velocity>().ToList();
            Console.WriteLine($"Moving entities: {movingEntities.Count}");

            // Parallel query for named entities
            var namedEntities = entityManager.GetEntitiesWithComponents<Name>().ToList();
            Console.WriteLine($"Named entities: {namedEntities.Count}");

            // Parallel processing of query results
            var results = new List<string>();
            
            Parallel.ForEach(movingEntities, entity =>
            {
                var position = entityManager.GetComponent<Position>(entity);
                var velocity = entityManager.GetComponent<Velocity>(entity);
                var name = entityManager.GetComponent<Name>(entity);
                
                var result = $"Entity {entity.Id} ({name.Value}): Pos({position.Value.X:F2}, {position.Value.Y:F2}, {position.Value.Z:F2}) Vel({velocity.Value.X:F2}, {velocity.Value.Y:F2}, {velocity.Value.Z:F2})";
                
                lock (results)
                {
                    results.Add(result);
                }
            });

            // Display results
            Console.WriteLine("\nParallel query results (first 5):");
            for (int i = 0; i < Math.Min(5, results.Count); i++)
            {
                Console.WriteLine(results[i]);
            }
        }

        /// <summary>
        /// Display entity details for demonstration
        /// </summary>
        private static void ShowEntityDetails(EntityManager entityManager, int count)
        {
            int shown = 0;
            foreach (var entity in entityManager.GetEntitiesWithComponents<Position, Velocity, Name>())
            {
                if (shown >= count) break;
                
                var position = entityManager.GetComponent<Position>(entity);
                var velocity = entityManager.GetComponent<Velocity>(entity);
                var name = entityManager.GetComponent<Name>(entity);
                
                Console.WriteLine($"Entity {entity.Id} ({name.Value}): Pos({position.Value.X:F2}, {position.Value.Y:F2}, {position.Value.Z:F2}) Vel({velocity.Value.X:F2}, {velocity.Value.Y:F2}, {velocity.Value.Z:F2})");
                shown++;
            }
        }
    }
} 