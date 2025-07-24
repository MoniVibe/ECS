using System;
using System.Numerics;

namespace ECS
{
    /// <summary>
    /// SIMD-optimized ECS example demonstrating batch processing and vector operations
    /// </summary>
    public static class SimdExample
    {
        public static void RunExample()
        {
            Console.WriteLine("=== SIMD-Optimized ECS Example ===");
            
            var entityManager = new EntityManager();

            // Create many entities for batch processing
            Console.WriteLine("Creating entities for batch processing...");
            for (int i = 0; i < 100; i++)
            {
                var entity = entityManager.CreateEntity<Position, Velocity>();
                entityManager.SetComponent(entity, new Position { Value = new Vector3(i, i * 0.5f, i * 0.25f) });
                entityManager.SetComponent(entity, new Velocity { Value = new Vector3(0.1f, -0.05f, 0.02f) });
            }

            Console.WriteLine($"Created {entityManager.GetStatistics().totalEntities} entities");

            // Show initial state
            Console.WriteLine("\nInitial positions (first 5 entities):");
            ShowEntityPositions(entityManager, 5);

            // SIMD-optimized physics update using batch processing
            Console.WriteLine("\nApplying SIMD-optimized physics update...");
            ApplySimdPhysics(entityManager);

            // Show final state
            Console.WriteLine("\nFinal positions (first 5 entities):");
            ShowEntityPositions(entityManager, 5);

            // Demonstrate SIMD batch processing with custom logic
            Console.WriteLine("\nApplying custom SIMD batch processing...");
            ApplyCustomSimdProcessing(entityManager);

            Console.WriteLine("\nFinal positions after custom processing (first 5 entities):");
            ShowEntityPositions(entityManager, 5);

            // Performance comparison
            Console.WriteLine("\nPerformance comparison:");
            var stats = entityManager.GetStatistics();
            Console.WriteLine($"Total entities processed: {stats.totalEntities}");
            Console.WriteLine($"Total chunks: {stats.totalChunks}");
        }

        /// <summary>
        /// SIMD-optimized physics update using batch processing
        /// </summary>
        private static void ApplySimdPhysics(EntityManager entityManager)
        {
            entityManager.ProcessHotComponentsBatch<Position, Velocity>((positions, velocities, count) =>
            {
                // Process in SIMD-friendly chunks
                for (int i = 0; i < count; i++)
                {
                    // Update position based on velocity
                    positions[i].Value.X += velocities[i].Value.X;
                    positions[i].Value.Y += velocities[i].Value.Y;
                    positions[i].Value.Z += velocities[i].Value.Z;

                    // Apply simple physics (gravity)
                    velocities[i].Value.Y -= 0.01f;

                    // Apply air resistance
                    velocities[i].Value.X *= 0.99f;
                    velocities[i].Value.Y *= 0.99f;
                    velocities[i].Value.Z *= 0.99f;
                }
            });
        }

        /// <summary>
        /// Custom SIMD batch processing with more complex logic
        /// </summary>
        private static void ApplyCustomSimdProcessing(EntityManager entityManager)
        {
            entityManager.ProcessHotComponentsBatch<Position, Velocity>((positions, velocities, count) =>
            {
                // Process in chunks optimized for SIMD operations
                for (int i = 0; i < count; i += 4)
                {
                    int remaining = Math.Min(4, count - i);
                    
                    for (int j = 0; j < remaining; j++)
                    {
                        int index = i + j;
                        
                        // Complex physics simulation
                        var pos = positions[index];
                        var vel = velocities[index];
                        
                        // Update position
                        pos.Value.X += vel.Value.X;
                        pos.Value.Y += vel.Value.Y;
                        pos.Value.Z += vel.Value.Z;
                        
                        // Apply forces
                        vel.Value.Y -= 0.02f; // Gravity
                        
                        // Bounce off ground
                        if (pos.Value.Y < 0)
                        {
                            pos.Value.Y = 0;
                            vel.Value.Y = -vel.Value.Y * 0.8f; // Bounce with energy loss
                        }
                        
                        // Bounce off walls
                        if (pos.Value.X < 0 || pos.Value.X > 100)
                        {
                            vel.Value.X = -vel.Value.X * 0.9f;
                        }
                        
                        // Update components
                        positions[index] = pos;
                        velocities[index] = vel;
                    }
                }
            });
        }

        /// <summary>
        /// Display entity positions for demonstration
        /// </summary>
        private static void ShowEntityPositions(EntityManager entityManager, int count)
        {
            int shown = 0;
            foreach (var entity in entityManager.GetEntitiesWithComponents<Position, Velocity>())
            {
                if (shown >= count) break;
                
                var position = entityManager.GetComponent<Position>(entity);
                var velocity = entityManager.GetComponent<Velocity>(entity);
                
                Console.WriteLine($"Entity {entity.Id}: Pos({position.Value.X:F2}, {position.Value.Y:F2}, {position.Value.Z:F2}) Vel({velocity.Value.X:F2}, {velocity.Value.Y:F2}, {velocity.Value.Z:F2})");
                shown++;
            }
        }
    }
} 