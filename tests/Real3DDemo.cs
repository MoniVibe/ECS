using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace ECS.Tests
{
    // OpenGL constants and function imports
    public static class GL
    {
        public const uint GL_TRIANGLES = 0x0004;
        public const uint GL_FLOAT = 0x1406;
        public const uint GL_ARRAY_BUFFER = 0x8892;
        public const uint GL_ELEMENT_ARRAY_BUFFER = 0x8893;
        public const uint GL_STATIC_DRAW = 0x88E4;
        public const uint GL_VERTEX_SHADER = 0x8B31;
        public const uint GL_FRAGMENT_SHADER = 0x8B30;
        public const uint GL_COMPILE_STATUS = 0x8B81;
        public const uint GL_LINK_STATUS = 0x8B82;
        public const uint GL_INFO_LOG_LENGTH = 0x8B84;
        public const uint GL_DEPTH_TEST = 0x0B71;
        public const uint GL_CULL_FACE = 0x0B44;
        public const uint GL_BACK = 0x0405;
        public const uint GL_CCW = 0x0901;
        public const uint GL_COLOR_BUFFER_BIT = 0x00004000;
        public const uint GL_DEPTH_BUFFER_BIT = 0x00000100;
        public const uint GL_FALSE = 0;
        public const uint GL_TRUE = 1;
        public const uint GL_UNSIGNED_INT = 0x1405;

        // OpenGL function imports
        [DllImport("opengl32.dll")]
        public static extern void glClear(uint mask);
        
        [DllImport("opengl32.dll")]
        public static extern void glClearColor(float red, float green, float blue, float alpha);
        
        [DllImport("opengl32.dll")]
        public static extern void glEnable(uint cap);
        
        [DllImport("opengl32.dll")]
        public static extern void glDisable(uint cap);
        
        [DllImport("opengl32.dll")]
        public static extern void glCullFace(uint mode);
        
        [DllImport("opengl32.dll")]
        public static extern void glFrontFace(uint mode);
        
        [DllImport("opengl32.dll")]
        public static extern void glViewport(int x, int y, int width, int height);
        
        [DllImport("opengl32.dll")]
        public static extern void glGenBuffers(int n, out uint buffers);
        
        [DllImport("opengl32.dll")]
        public static extern void glBindBuffer(uint target, uint buffer);
        
        [DllImport("opengl32.dll")]
        public static extern void glBufferData(uint target, int size, IntPtr data, uint usage);
        
        [DllImport("opengl32.dll")]
        public static extern void glGenVertexArrays(int n, out uint arrays);
        
        [DllImport("opengl32.dll")]
        public static extern void glBindVertexArray(uint array);
        
        [DllImport("opengl32.dll")]
        public static extern void glEnableVertexAttribArray(uint index);
        
        [DllImport("opengl32.dll")]
        public static extern void glVertexAttribPointer(uint index, int size, uint type, bool normalized, int stride, IntPtr pointer);
        
        [DllImport("opengl32.dll")]
        public static extern void glDrawElements(uint mode, int count, uint type, IntPtr indices);
        
        [DllImport("opengl32.dll")]
        public static extern uint glCreateShader(uint type);
        
        [DllImport("opengl32.dll")]
        public static extern void glShaderSource(uint shader, int count, string[] source, int[] length);
        
        [DllImport("opengl32.dll")]
        public static extern void glCompileShader(uint shader);
        
        [DllImport("opengl32.dll")]
        public static extern void glGetShaderiv(uint shader, uint pname, out int parameters);
        
        [DllImport("opengl32.dll")]
        public static extern void glGetShaderInfoLog(uint shader, int maxLength, out int length, out string infoLog);
        
        [DllImport("opengl32.dll")]
        public static extern uint glCreateProgram();
        
        [DllImport("opengl32.dll")]
        public static extern void glAttachShader(uint program, uint shader);
        
        [DllImport("opengl32.dll")]
        public static extern void glLinkProgram(uint program);
        
        [DllImport("opengl32.dll")]
        public static extern void glUseProgram(uint program);
        
        [DllImport("opengl32.dll")]
        public static extern int glGetUniformLocation(uint program, string name);
        
        [DllImport("opengl32.dll")]
        public static extern void glUniformMatrix4fv(int location, int count, bool transpose, float[] value);
        
        [DllImport("opengl32.dll")]
        public static extern void glUniform3fv(int location, int count, float[] value);
    }

    public class Real3DDemo
    {
        private ComprehensiveEntityManager entityManager;
        private Random random;
        private uint shaderProgram;
        private uint cubeVAO, cubeVBO, cubeEBO;
        private uint sphereVAO, sphereVBO, sphereEBO;
        private bool isInitialized;
        private float time;

        public Real3DDemo()
        {
            entityManager = new ComprehensiveEntityManager();
            random = new Random();
            isInitialized = false;
            time = 0.0f;
        }

        public void InitializeGraphics()
        {
            Console.WriteLine("🎨 Initializing Real 3D Graphics System...");
            
            try
            {
                // Initialize OpenGL
                GL.glClearColor(0.1f, 0.1f, 0.2f, 1.0f);
                GL.glEnable(GL.GL_DEPTH_TEST);
                GL.glEnable(GL.GL_CULL_FACE);
                GL.glCullFace(GL.GL_BACK);
                GL.glFrontFace(GL.GL_CCW);
                
                // Create shaders
                CreateShaders();
                
                // Create meshes
                CreateCubeMesh();
                CreateSphereMesh();
                
                isInitialized = true;
                Console.WriteLine("✅ Real 3D graphics system initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to initialize graphics: {ex.Message}");
                Console.WriteLine("📝 Note: This demo requires OpenGL support. Running in simulation mode...");
                isInitialized = false;
            }
        }

        private void CreateShaders()
        {
            string vertexShaderSource = @"
                #version 330 core
                layout (location = 0) in vec3 aPos;
                
                uniform mat4 model;
                uniform mat4 view;
                uniform mat4 projection;
                
                void main()
                {
                    gl_Position = projection * view * model * vec4(aPos, 1.0);
                }";

            string fragmentShaderSource = @"
                #version 330 core
                out vec4 FragColor;
                
                uniform vec3 color;
                
                void main()
                {
                    FragColor = vec4(color, 1.0);
                }";

            // Create and compile vertex shader
            uint vertexShader = GL.glCreateShader(GL.GL_VERTEX_SHADER);
            GL.glShaderSource(vertexShader, 1, new string[] { vertexShaderSource }, null);
            GL.glCompileShader(vertexShader);
            
            // Create and compile fragment shader
            uint fragmentShader = GL.glCreateShader(GL.GL_FRAGMENT_SHADER);
            GL.glShaderSource(fragmentShader, 1, new string[] { fragmentShaderSource }, null);
            GL.glCompileShader(fragmentShader);
            
            // Create and link program
            shaderProgram = GL.glCreateProgram();
            GL.glAttachShader(shaderProgram, vertexShader);
            GL.glAttachShader(shaderProgram, fragmentShader);
            GL.glLinkProgram(shaderProgram);
        }

        private void CreateCubeMesh()
        {
            float[] vertices = {
                // Front face
                -0.5f, -0.5f,  0.5f,
                 0.5f, -0.5f,  0.5f,
                 0.5f,  0.5f,  0.5f,
                -0.5f,  0.5f,  0.5f,
                // Back face
                -0.5f, -0.5f, -0.5f,
                 0.5f, -0.5f, -0.5f,
                 0.5f,  0.5f, -0.5f,
                -0.5f,  0.5f, -0.5f
            };

            uint[] indices = {
                // Front
                0, 1, 2,  2, 3, 0,
                // Back
                5, 4, 7,  7, 6, 5,
                // Left
                4, 0, 3,  3, 7, 4,
                // Right
                1, 5, 6,  6, 2, 1,
                // Top
                3, 2, 6,  6, 7, 3,
                // Bottom
                4, 5, 1,  1, 0, 4
            };

            GL.glGenVertexArrays(1, out cubeVAO);
            GL.glGenBuffers(1, out cubeVBO);
            GL.glGenBuffers(1, out cubeEBO);

            GL.glBindVertexArray(cubeVAO);
            
            GL.glBindBuffer(GL.GL_ARRAY_BUFFER, cubeVBO);
            GL.glBufferData(GL.GL_ARRAY_BUFFER, vertices.Length * sizeof(float), IntPtr.Zero, GL.GL_STATIC_DRAW);
            
            GL.glBindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, cubeEBO);
            GL.glBufferData(GL.GL_ELEMENT_ARRAY_BUFFER, indices.Length * sizeof(uint), IntPtr.Zero, GL.GL_STATIC_DRAW);
            
            GL.glEnableVertexAttribArray(0);
            GL.glVertexAttribPointer(0, 3, GL.GL_FLOAT, false, 3 * sizeof(float), IntPtr.Zero);
        }

        private void CreateSphereMesh()
        {
            // Simplified sphere for demo
            var vertices = new List<float>();
            var indices = new List<uint>();
            
            int segments = 16;
            for (int lat = 0; lat <= segments; lat++)
            {
                float theta = lat * (float)Math.PI / segments;
                float sinTheta = (float)Math.Sin(theta);
                float cosTheta = (float)Math.Cos(theta);

                for (int lon = 0; lon <= segments; lon++)
                {
                    float phi = lon * 2.0f * (float)Math.PI / segments;
                    float sinPhi = (float)Math.Sin(phi);
                    float cosPhi = (float)Math.Cos(phi);

                    float x = cosPhi * sinTheta * 0.5f;
                    float y = cosTheta * 0.5f;
                    float z = sinPhi * sinTheta * 0.5f;

                    vertices.Add(x);
                    vertices.Add(y);
                    vertices.Add(z);
                }
            }

            // Generate indices
            for (int lat = 0; lat < segments; lat++)
            {
                for (int lon = 0; lon < segments; lon++)
                {
                    uint current = (uint)(lat * (segments + 1) + lon);
                    uint next = (uint)(lat * (segments + 1) + (lon + 1));
                    uint currentNext = (uint)((lat + 1) * (segments + 1) + lon);
                    uint nextNext = (uint)((lat + 1) * (segments + 1) + (lon + 1));

                    indices.Add(current);
                    indices.Add(currentNext);
                    indices.Add(next);

                    indices.Add(next);
                    indices.Add(currentNext);
                    indices.Add(nextNext);
                }
            }

            GL.glGenVertexArrays(1, out sphereVAO);
            GL.glGenBuffers(1, out sphereVBO);
            GL.glGenBuffers(1, out sphereEBO);

            GL.glBindVertexArray(sphereVAO);
            
            GL.glBindBuffer(GL.GL_ARRAY_BUFFER, sphereVBO);
            GL.glBufferData(GL.GL_ARRAY_BUFFER, vertices.Count * sizeof(float), IntPtr.Zero, GL.GL_STATIC_DRAW);
            
            GL.glBindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, sphereEBO);
            GL.glBufferData(GL.GL_ELEMENT_ARRAY_BUFFER, indices.Count * sizeof(uint), IntPtr.Zero, GL.GL_STATIC_DRAW);
            
            GL.glEnableVertexAttribArray(0);
            GL.glVertexAttribPointer(0, 3, GL.GL_FLOAT, false, 3 * sizeof(float), IntPtr.Zero);
        }

        public void CreatePhysicsDemo()
        {
            Console.WriteLine("\n🎯 Creating Real 3D Physics Demo Scene...");
            
            // Create ground plane
            var ground = entityManager.CreateEntity();
            entityManager.SetComponent(ground, new CompTestPosition { Value = new Vector3(0, -2, 0) });
            entityManager.SetComponent(ground, new Transform { Position = new Vector3(0, -2, 0), Scale = new Vector3(20, 0.1f, 20), Rotation = Quaternion.Identity });
            entityManager.SetComponent(ground, new Renderable { Mesh = "Plane", Texture = "Ground", Color = new Vector3(0.3f, 0.5f, 0.3f) });
            entityManager.SetComponent(ground, new PhysicsBody { Mass = 0, IsStatic = true });
            entityManager.SetComponent(ground, new Material { Color = new Vector3(0.3f, 0.5f, 0.3f), Metallic = 0.0f, Roughness = 0.8f, Opacity = 1.0f });

            // Create falling cubes
            for (int i = 0; i < 10; i++)
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
            for (int i = 0; i < 5; i++)
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
            if (!isInitialized)
            {
                Console.WriteLine("🎬 Simulating 3D rendering (OpenGL not available)...");
                return;
            }

            // Clear buffers
            GL.glClear(GL.GL_COLOR_BUFFER_BIT | GL.GL_DEPTH_BUFFER_BIT);
            
            // Set up viewport
            GL.glViewport(0, 0, 800, 600);
            
            // Use shader program
            GL.glUseProgram(shaderProgram);
            
            // Create view and projection matrices
            var view = Matrix4x4.CreateLookAt(
                new Vector3(0, 8, 15),
                Vector3.Zero,
                Vector3.UnitY
            );
            
            var projection = Matrix4x4.CreatePerspectiveFieldOfView(
                (float)Math.PI / 4.0f,
                800.0f / 600.0f,
                0.1f,
                100.0f
            );
            
            // Get uniform locations
            int viewLoc = GL.glGetUniformLocation(shaderProgram, "view");
            int projectionLoc = GL.glGetUniformLocation(shaderProgram, "projection");
            int modelLoc = GL.glGetUniformLocation(shaderProgram, "model");
            int colorLoc = GL.glGetUniformLocation(shaderProgram, "color");
            
            // Set view and projection uniforms
            GL.glUniformMatrix4fv(viewLoc, 1, false, GetMatrixArray(view));
            GL.glUniformMatrix4fv(projectionLoc, 1, false, GetMatrixArray(projection));
            
            // Render all entities
            var renderables = entityManager.GetEntitiesWithComponents<Renderable>();
            foreach (var entity in renderables)
            {
                if (entityManager.HasComponent<Transform>(entity))
                {
                    var transform = entityManager.GetComponent<Transform>(entity);
                    var renderable = entityManager.GetComponent<Renderable>(entity);
                    var material = entityManager.GetComponent<Material>(entity);
                    
                    // Create model matrix
                    var model = Matrix4x4.CreateScale(transform.Scale) *
                               Matrix4x4.CreateFromQuaternion(transform.Rotation) *
                               Matrix4x4.CreateTranslation(transform.Position);
                    
                    // Set uniforms
                    GL.glUniformMatrix4fv(modelLoc, 1, false, GetMatrixArray(model));
                    GL.glUniform3fv(colorLoc, 1, new float[] { material.Color.X, material.Color.Y, material.Color.Z });
                    
                    // Draw based on mesh type
                    if (renderable.Mesh == "Cube")
                    {
                        GL.glBindVertexArray(cubeVAO);
                        GL.glDrawElements(GL.GL_TRIANGLES, 36, GL.GL_UNSIGNED_INT, IntPtr.Zero);
                    }
                    else if (renderable.Mesh == "Sphere")
                    {
                        GL.glBindVertexArray(sphereVAO);
                        GL.glDrawElements(GL.GL_TRIANGLES, 16 * 16 * 6, GL.GL_UNSIGNED_INT, IntPtr.Zero);
                    }
                }
            }
            
            Console.WriteLine($"🎬 Rendered {renderables.Count()} 3D objects");
        }

        private float[] GetMatrixArray(Matrix4x4 matrix)
        {
            return new float[]
            {
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                matrix.M41, matrix.M42, matrix.M43, matrix.M44
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

        public void RunReal3DDemo()
        {
            Console.WriteLine("🎮 Real 3D ECS Demo");
            Console.WriteLine("====================");
            
            try
            {
                InitializeGraphics();
                
                // Create demo scene
                CreatePhysicsDemo();
                
                Console.WriteLine($"\n📊 Total entities: {entityManager.GetEntityCount()}");
                Console.WriteLine("🎬 Starting 3D rendering loop...");
                Console.WriteLine("📝 Note: If OpenGL is not available, this will run in simulation mode");
                
                // Simulate and render for several frames
                for (int frame = 0; frame < 15; frame++)
                {
                    Console.WriteLine($"\n=== Frame {frame} ===");
                    
                    time += 0.016f;
                    SimulatePhysics(0.016f);
                    RenderFrame();
                    
                    // Show some entity details
                    var physicsEntities = entityManager.GetEntitiesWithComponents<PhysicsBody>();
                    var movingEntities = physicsEntities.Where(e => !entityManager.GetComponent<PhysicsBody>(e).IsStatic).Take(3);
                    
                    foreach (var entity in movingEntities)
                    {
                        var position = entityManager.GetComponent<CompTestPosition>(entity);
                        var physics = entityManager.GetComponent<PhysicsBody>(entity);
                        Console.WriteLine($"  🏃 Physics object: pos=({position.Value.X:F2}, {position.Value.Y:F2}, {position.Value.Z:F2}), vel=({physics.Velocity.X:F2}, {physics.Velocity.Y:F2}, {physics.Velocity.Z:F2})");
                    }
                    
                    System.Threading.Thread.Sleep(100);
                }
                
                // Final statistics
                var stats = entityManager.GetStatistics();
                Console.WriteLine($"\n=== Final Statistics ===");
                Console.WriteLine($"Total entities: {stats["totalEntities"]}");
                Console.WriteLine($"Total components: {stats["totalComponents"]}");
                Console.WriteLine($"Unique component types: {stats["uniqueComponentTypes"]}");
                
                Console.WriteLine("\n🎉 Real 3D demo completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Real 3D demo failed: {ex.Message}");
                Console.WriteLine("📝 Running in simulation mode instead...");
                
                // Fallback to simulation mode
                CreatePhysicsDemo();
                for (int frame = 0; frame < 5; frame++)
                {
                    SimulatePhysics(0.016f);
                    Console.WriteLine($"Simulated frame {frame + 1}");
                    System.Threading.Thread.Sleep(200);
                }
            }
        }
    }
} 