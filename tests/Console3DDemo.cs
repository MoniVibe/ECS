using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

namespace ECS.Tests
{
    public class Console3DDemo
    {
        private ComprehensiveEntityManager entityManager;
        private Random random;
        private int screenWidth = 80;
        private int screenHeight = 24;
        private char[,] screen;
        private float time;

        public Console3DDemo()
        {
            entityManager = new ComprehensiveEntityManager();
            random = new Random();
            screen = new char[screenWidth, screenHeight];
            time = 0.0f;
        }

        public void InitializeGraphics()
        {
            Console.WriteLine("🎨 Initializing Console 3D Graphics System...");
            Console.WriteLine("📺 You will see a 3D visualization in the console!");
            
            // Set console properties for better display
            try
            {
                Console.WindowWidth = screenWidth + 2;
                Console.WindowHeight = screenHeight + 2;
                Console.CursorVisible = false;
            }
            catch
            {
                // Ignore if we can't set console properties
            }
            
            Console.WriteLine("✅ Console 3D graphics system initialized");
        }

        public void CreatePhysicsDemo()
        {
            Console.WriteLine("\n🎯 Creating Console 3D Physics Demo Scene...");
            
            // Create ground plane
            var ground = entityManager.CreateEntity();
            entityManager.SetComponent(ground, new CompTestPosition { Value = new Vector3(0, -2, 0) });
            entityManager.SetComponent(ground, new Transform { Position = new Vector3(0, -2, 0), Scale = new Vector3(20, 0.1f, 20), Rotation = Quaternion.Identity });
            entityManager.SetComponent(ground, new Renderable { Mesh = "Plane", Texture = "Ground", Color = new Vector3(0.3f, 0.5f, 0.3f) });
            entityManager.SetComponent(ground, new PhysicsBody { Mass = 0, IsStatic = true });
            entityManager.SetComponent(ground, new Material { Color = new Vector3(0.3f, 0.5f, 0.3f), Metallic = 0.0f, Roughness = 0.8f, Opacity = 1.0f });

            // Create falling cubes
            for (int i = 0; i < 6; i++)
            {
                var cube = entityManager.CreateEntity();
                var x = (float)(random.NextDouble() * 16 - 8);
                var z = (float)(random.NextDouble() * 16 - 8);
                var y = 8 + (float)(random.NextDouble() * 4);
                
                entityManager.SetComponent(cube, new CompTestPosition { Value = new Vector3(x, y, z) });
                entityManager.SetComponent(cube, new Transform { Position = new Vector3(x, y, z), Scale = Vector3.One, Rotation = Quaternion.Identity });
                entityManager.SetComponent(cube, new Renderable { Mesh = "Cube", Texture = "Stone", Color = new Vector3(0.7f, 0.7f, 0.7f) });
                entityManager.SetComponent(cube, new PhysicsBody { Mass = 1.0f, Velocity = Vector3.Zero, IsStatic = false });
                entityManager.SetComponent(cube, new Material { Color = new Vector3(0.7f, 0.7f, 0.7f), Metallic = 0.1f, Roughness = 0.6f, Opacity = 1.0f });
            }

            // Create bouncing spheres
            for (int i = 0; i < 3; i++)
            {
                var sphere = entityManager.CreateEntity();
                var x = (float)(random.NextDouble() * 12 - 6);
                var z = (float)(random.NextDouble() * 12 - 6);
                var y = 6 + (float)(random.NextDouble() * 3);
                
                entityManager.SetComponent(sphere, new CompTestPosition { Value = new Vector3(x, y, z) });
                entityManager.SetComponent(sphere, new Transform { Position = new Vector3(x, y, z), Scale = Vector3.One, Rotation = Quaternion.Identity });
                entityManager.SetComponent(sphere, new Renderable { Mesh = "Sphere", Texture = "Metal", Color = new Vector3(0.8f, 0.6f, 0.2f) });
                entityManager.SetComponent(sphere, new PhysicsBody { Mass = 0.5f, Velocity = Vector3.Zero, IsStatic = false });
                entityManager.SetComponent(sphere, new Material { Color = new Vector3(0.8f, 0.6f, 0.2f), Metallic = 0.8f, Roughness = 0.2f, Opacity = 1.0f });
            }

            Console.WriteLine($"Created physics demo with {entityManager.GetEntityCount()} entities");
        }

        public void RenderFrame()
        {
            // Clear screen buffer
            for (int x = 0; x < screenWidth; x++)
            {
                for (int y = 0; y < screenHeight; y++)
                {
                    screen[x, y] = ' ';
                }
            }

            // Draw ground
            for (int x = 0; x < screenWidth; x++)
            {
                int groundY = screenHeight - 2;
                screen[x, groundY] = '=';
                screen[x, groundY + 1] = '=';
            }

            // Render all entities
            var renderables = entityManager.GetEntitiesWithComponents<Renderable>();
            foreach (var entity in renderables)
            {
                if (entityManager.HasComponent<Transform>(entity))
                {
                    var transform = entityManager.GetComponent<Transform>(entity);
                    var renderable = entityManager.GetComponent<Renderable>(entity);
                    
                    // Project 3D position to 2D screen coordinates
                    var screenPos = Project3DToScreen(transform.Position);
                    
                    if (screenPos.X >= 0 && screenPos.X < screenWidth && 
                        screenPos.Y >= 0 && screenPos.Y < screenHeight)
                    {
                        char symbol = GetSymbolForMesh(renderable.Mesh);
                        screen[(int)screenPos.X, (int)screenPos.Y] = symbol;
                        
                        // Add some depth info
                        if ((int)screenPos.X + 1 < screenWidth && screen[(int)screenPos.X + 1, (int)screenPos.Y] == ' ')
                        {
                            screen[(int)screenPos.X + 1, (int)screenPos.Y] = '.';
                        }
                    }
                }
            }

            // Draw screen
            Console.SetCursorPosition(0, 0);
            Console.WriteLine("🎮 ECS 3D Console Demo - Physics Simulation");
            Console.WriteLine("=" + new string('=', screenWidth) + "=");
            
            for (int y = 0; y < screenHeight; y++)
            {
                Console.Write("|");
                for (int x = 0; x < screenWidth; x++)
                {
                    Console.Write(screen[x, y]);
                }
                Console.WriteLine("|");
            }
            
            Console.WriteLine("=" + new string('=', screenWidth) + "=");
            Console.WriteLine($"Time: {time:F2}s | Entities: {renderables.Count()} | Legend: █=Cube ●=Sphere =Ground");
        }

        private Vector2 Project3DToScreen(Vector3 worldPos)
        {
            // Simple orthographic projection
            float scale = 2.0f;
            int centerX = screenWidth / 2;
            int centerY = screenHeight / 2;
            
            int screenX = (int)(centerX + worldPos.X * scale);
            int screenY = (int)(centerY - worldPos.Y * scale - worldPos.Z * 0.5f);
            
            return new Vector2((float)screenX, (float)screenY);
        }

        private char GetSymbolForMesh(string meshType)
        {
            return meshType switch
            {
                "Cube" => '█',
                "Sphere" => '●',
                "Plane" => '=',
                _ => '?'
            };
        }

        public void SimulatePhysics(float deltaTime)
        {
            var physicsEntities = entityManager.GetEntitiesWithComponents<CompTestPosition, PhysicsBody>();
            
            foreach (var entity in physicsEntities)
            {
                var position = entityManager.GetComponent<CompTestPosition>(entity);
                var physics = entityManager.GetComponent<PhysicsBody>(entity);
                
                if (!physics.IsStatic)
                {
                    // Enhanced physics simulation
                    physics.Velocity.Y -= 9.81f * deltaTime;
                    position.Value += physics.Velocity * deltaTime;
                    
                    // Ground collision with bounce
                    if (position.Value.Y < -1.9f)
                    {
                        position.Value.Y = -1.9f;
                        physics.Velocity.Y = -physics.Velocity.Y * 0.7f; // Bounce with energy loss
                        
                        // Stop if velocity is very small
                        if (Math.Abs(physics.Velocity.Y) < 0.1f)
                        {
                            physics.Velocity.Y = 0;
                        }
                    }
                    
                    // Air resistance
                    physics.Velocity *= 0.99f;
                    
                    // Update transform
                    var transform = entityManager.GetComponent<Transform>(entity);
                    transform.Position = position.Value;
                    entityManager.SetComponent(entity, transform);
                    
                    entityManager.SetComponent(entity, position);
                    entityManager.SetComponent(entity, physics);
                }
            }
        }

        public void RunConsole3DDemo()
        {
            Console.WriteLine("🎮 Console 3D ECS Demo");
            Console.WriteLine("======================");
            
            try
            {
                InitializeGraphics();
                
                // Create demo scene
                CreatePhysicsDemo();
                
                Console.WriteLine($"\n📊 Total entities: {entityManager.GetEntityCount()}");
                Console.WriteLine("🎬 Starting 3D console rendering...");
                Console.WriteLine("👀 Watch the physics simulation in the console!");
                Console.WriteLine("⏸️  Press any key to stop...");
                
                // Simulate and render for several frames
                for (int frame = 0; frame < 50; frame++)
                {
                    time += 0.016f;
                    SimulatePhysics(0.016f);
                    RenderFrame();
                    
                    // Show some entity details
                    var physicsEntities = entityManager.GetEntitiesWithComponents<PhysicsBody>();
                    var movingEntities = physicsEntities.Where(e => !entityManager.GetComponent<PhysicsBody>(e).IsStatic).Take(2);
                    
                    Console.WriteLine();
                    foreach (var entity in movingEntities)
                    {
                        var position = entityManager.GetComponent<CompTestPosition>(entity);
                        var physics = entityManager.GetComponent<PhysicsBody>(entity);
                        Console.WriteLine($"  🏃 Physics: pos=({position.Value.X:F1}, {position.Value.Y:F1}, {position.Value.Z:F1}) vel=({physics.Velocity.Y:F1})");
                    }
                    
                    // Check for key press to stop
                    if (Console.KeyAvailable)
                    {
                        Console.ReadKey(true);
                        break;
                    }
                    
                    System.Threading.Thread.Sleep(100);
                }
                
                // Final statistics
                var stats = entityManager.GetStatistics();
                Console.WriteLine($"\n=== Final Statistics ===");
                Console.WriteLine($"Total entities: {stats["totalEntities"]}");
                Console.WriteLine($"Total components: {stats["totalComponents"]}");
                Console.WriteLine($"Unique component types: {stats["uniqueComponentTypes"]}");
                
                Console.WriteLine("\n🎉 Console 3D demo completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Console 3D demo failed: {ex.Message}");
            }
        }
    }
} 