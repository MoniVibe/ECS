using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;
using System.Linq; // Added for .ToList()

namespace ECS
{
    /// <summary>
    /// Serialization ECS example demonstrating component save/load capabilities
    /// </summary>
    public static class SerializationExample
    {
        public static void RunExample()
        {
            Console.WriteLine("=== Serialization ECS Example ===");
            
            var entityManager = new EntityManager();

            // Create entities with various components
            Console.WriteLine("Creating entities for serialization test...");
            
            var player = entityManager.CreateEntity<Position, Velocity, Name, Health>();
            entityManager.SetComponent(player, new Position { Value = new Vector3(10, 5, 0) });
            entityManager.SetComponent(player, new Velocity { Value = new Vector3(1, 0, 0) });
            entityManager.SetComponent(player, new Name { Value = "Player" });
            entityManager.SetComponent(player, new Health { Value = 100 });

            var enemy = entityManager.CreateEntity<Position, Velocity, Name, Health>();
            entityManager.SetComponent(enemy, new Position { Value = new Vector3(20, 0, 0) });
            entityManager.SetComponent(enemy, new Velocity { Value = new Vector3(-0.5f, 0, 0) });
            entityManager.SetComponent(enemy, new Name { Value = "Enemy" });
            entityManager.SetComponent(enemy, new Health { Value = 50 });

            var item = entityManager.CreateEntity<Position, Name>();
            entityManager.SetComponent(item, new Position { Value = new Vector3(15, 2, 0) });
            entityManager.SetComponent(item, new Name { Value = "Health Potion" });

            Console.WriteLine($"Created {entityManager.GetStatistics().totalEntities} entities");

            // Show initial state
            Console.WriteLine("\nInitial state:");
            ShowAllEntities(entityManager);

            // Serialize entities to file
            Console.WriteLine("\nSerializing entities to file...");
            SerializeEntities(entityManager, "entities_save.dat");

            // Create a new entity manager and deserialize
            Console.WriteLine("\nCreating new entity manager and deserializing...");
            var newEntityManager = new EntityManager();
            DeserializeEntities(newEntityManager, "entities_save.dat");

            // Show deserialized state
            Console.WriteLine("\nDeserialized state:");
            ShowAllEntities(newEntityManager);

            // Verify data integrity
            Console.WriteLine("\nVerifying data integrity...");
            VerifyDataIntegrity(entityManager, newEntityManager);

            // Demonstrate component-specific serialization
            Console.WriteLine("\nDemonstrating component-specific serialization...");
            DemonstrateComponentSerialization();
        }

        /// <summary>
        /// Serialize all entities to a file
        /// </summary>
        private static void SerializeEntities(EntityManager entityManager, string filename)
        {
            using var stream = new FileStream(filename, FileMode.Create);
            using var writer = new BinaryWriter(stream);

            // Write entity count
            var allEntities = entityManager.GetAllEntities().ToList();
            writer.Write(allEntities.Count);

            foreach (var entity in allEntities)
            {
                // Write entity ID
                writer.Write(entity.Id);
                writer.Write(entity.Generation);

                // Write component count
                var components = new List<(ComponentType, object)>();
                
                if (entityManager.HasComponent<Position>(entity))
                    components.Add((ComponentTypeRegistry.Get<Position>(), entityManager.GetComponent<Position>(entity)));
                if (entityManager.HasComponent<Velocity>(entity))
                    components.Add((ComponentTypeRegistry.Get<Velocity>(), entityManager.GetComponent<Velocity>(entity)));
                if (entityManager.HasComponent<Name>(entity))
                    components.Add((ComponentTypeRegistry.Get<Name>(), entityManager.GetComponent<Name>(entity)));
                if (entityManager.HasComponent<Health>(entity))
                    components.Add((ComponentTypeRegistry.Get<Health>(), entityManager.GetComponent<Health>(entity)));

                writer.Write(components.Count);

                // Write each component
                foreach (var (componentType, component) in components)
                {
                    writer.Write(componentType.Id);
                    SerializeComponent(writer, component);
                }
            }

            Console.WriteLine($"Serialized {allEntities.Count} entities to {filename}");
        }

        /// <summary>
        /// Deserialize entities from a file
        /// </summary>
        private static void DeserializeEntities(EntityManager entityManager, string filename)
        {
            using var stream = new FileStream(filename, FileMode.Open);
            using var reader = new BinaryReader(stream);

            // Read entity count
            var entityCount = reader.ReadInt32();

            for (int i = 0; i < entityCount; i++)
            {
                // Read entity ID
                var entityId = reader.ReadInt32();
                var generation = reader.ReadInt32();
                var entity = new EntityId(entityId, generation);

                // Read component count
                var componentCount = reader.ReadInt32();

                // Create entity with components
                var componentTypes = new List<ComponentType>();
                for (int j = 0; j < componentCount; j++)
                {
                    var componentTypeId = reader.ReadInt32();
                    var componentType = ComponentTypeRegistry.GetType(componentTypeId);
                    if (componentType != null)
                    {
                        componentTypes.Add(ComponentTypeRegistry.Get(componentType));
                    }
                }

                // Create entity and add components
                var newEntity = entityManager.CreateEntity(componentTypes.ToArray());

                // Read component data
                stream.Position -= componentCount * sizeof(int); // Go back to component data
                for (int j = 0; j < componentCount; j++)
                {
                    var componentTypeId = reader.ReadInt32();
                    var componentType = ComponentTypeRegistry.GetType(componentTypeId);
                    if (componentType != null)
                    {
                        var component = DeserializeComponent(reader, componentType);
                        var setComponentMethod = typeof(EntityManager).GetMethod("SetComponent", new[] { typeof(EntityId), typeof(ComponentType), componentType });
                        if (setComponentMethod != null)
                        {
                            var genericMethod = setComponentMethod.MakeGenericMethod(componentType);
                            genericMethod.Invoke(entityManager, new object[] { newEntity, ComponentTypeRegistry.Get(componentType), component });
                        }
                    }
                }
            }

            Console.WriteLine($"Deserialized {entityCount} entities from {filename}");
        }

        /// <summary>
        /// Serialize a component to binary format
        /// </summary>
        private static void SerializeComponent(BinaryWriter writer, object component)
        {
            switch (component)
            {
                case Position pos:
                    writer.Write(pos.Value.X);
                    writer.Write(pos.Value.Y);
                    writer.Write(pos.Value.Z);
                    break;
                case Velocity vel:
                    writer.Write(vel.Value.X);
                    writer.Write(vel.Value.Y);
                    writer.Write(vel.Value.Z);
                    break;
                case Name name:
                    writer.Write(name.Value);
                    break;
                case Health health:
                    writer.Write(health.Value);
                    break;
                default:
                    throw new NotSupportedException($"Component type {component.GetType()} not supported for serialization");
            }
        }

        /// <summary>
        /// Deserialize a component from binary format
        /// </summary>
        private static object DeserializeComponent(BinaryReader reader, Type componentType)
        {
            if (componentType == typeof(Position))
            {
                var x = reader.ReadSingle();
                var y = reader.ReadSingle();
                var z = reader.ReadSingle();
                return new Position { Value = new ECS.Vector3(x, y, z) };
            }
            else if (componentType == typeof(Velocity))
            {
                var x = reader.ReadSingle();
                var y = reader.ReadSingle();
                var z = reader.ReadSingle();
                return new Velocity { Value = new ECS.Vector3(x, y, z) };
            }
            else if (componentType == typeof(Name))
            {
                var value = reader.ReadString();
                return new Name { Value = value };
            }
            else if (componentType == typeof(Health))
            {
                var value = reader.ReadInt32();
                return new Health { Value = value };
            }
            else
            {
                throw new NotSupportedException($"Component type {componentType} not supported for deserialization");
            }
        }

        /// <summary>
        /// Verify that serialized and deserialized data match
        /// </summary>
        private static void VerifyDataIntegrity(EntityManager original, EntityManager deserialized)
        {
            var originalEntities = original.GetAllEntities().ToList();
            var deserializedEntities = deserialized.GetAllEntities().ToList();

            if (originalEntities.Count != deserializedEntities.Count)
            {
                Console.WriteLine("❌ Entity count mismatch!");
                return;
            }

            Console.WriteLine("✓ Entity count matches");

            // Compare component data
            for (int i = 0; i < originalEntities.Count; i++)
            {
                var originalEntity = originalEntities[i];
                var deserializedEntity = deserializedEntities[i];

                // Compare components
                if (original.HasComponent<Position>(originalEntity) && deserialized.HasComponent<Position>(deserializedEntity))
                {
                    var originalPos = original.GetComponent<Position>(originalEntity);
                    var deserializedPos = deserialized.GetComponent<Position>(deserializedEntity);
                    
                                    if (originalPos.Value.X != deserializedPos.Value.X || 
                    originalPos.Value.Y != deserializedPos.Value.Y || 
                    originalPos.Value.Z != deserializedPos.Value.Z)
                {
                    Console.WriteLine($"❌ Position mismatch for entity {originalEntity.Id}");
                    return;
                }
                }

                if (original.HasComponent<Name>(originalEntity) && deserialized.HasComponent<Name>(deserializedEntity))
                {
                    var originalName = original.GetComponent<Name>(originalEntity);
                    var deserializedName = deserialized.GetComponent<Name>(deserializedEntity);
                    
                    if (originalName.Value != deserializedName.Value)
                    {
                        Console.WriteLine($"❌ Name mismatch for entity {originalEntity.Id}");
                        return;
                    }
                }
            }

            Console.WriteLine("✓ All component data matches");
        }

        /// <summary>
        /// Demonstrate component-specific serialization
        /// </summary>
        private static void DemonstrateComponentSerialization()
        {
            // Create component accessor and serializer factories
            var accessorFactory = new ComponentAccessorFactory();
            var serializerFactory = new ComponentSerializerFactory();
            var deserializerFactory = new ComponentDeserializerFactory();

            // Create serializers for different component types
            var positionSerializer = serializerFactory.CreateSerializer<Position>();
            var positionDeserializer = deserializerFactory.CreateDeserializer<Position>();

            // Test serialization
            var position = new Position { Value = new Vector3(1, 2, 3) };
            
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            
            positionSerializer(writer, position);
            
            stream.Position = 0;
            using var reader = new BinaryReader(stream);
            var deserializedPosition = positionDeserializer(reader);
            
            Console.WriteLine($"Original position: ({position.Value.X}, {position.Value.Y}, {position.Value.Z})");
            Console.WriteLine($"Deserialized position: ({deserializedPosition.Value.X}, {deserializedPosition.Value.Y}, {deserializedPosition.Value.Z})");
            Console.WriteLine($"Serialization successful: {position.Value.X == deserializedPosition.Value.X && position.Value.Y == deserializedPosition.Value.Y && position.Value.Z == deserializedPosition.Value.Z}");
        }

        /// <summary>
        /// Display all entities in the entity manager
        /// </summary>
        private static void ShowAllEntities(EntityManager entityManager)
        {
            foreach (var entity in entityManager.GetAllEntities())
            {
                Console.Write($"Entity {entity.Id}: ");
                
                if (entityManager.HasComponent<Name>(entity))
                {
                    var name = entityManager.GetComponent<Name>(entity);
                    Console.Write($"{name.Value} ");
                }
                
                if (entityManager.HasComponent<Position>(entity))
                {
                    var position = entityManager.GetComponent<Position>(entity);
                    Console.Write($"Pos({position.Value.X:F1}, {position.Value.Y:F1}, {position.Value.Z:F1}) ");
                }
                
                if (entityManager.HasComponent<Velocity>(entity))
                {
                    var velocity = entityManager.GetComponent<Velocity>(entity);
                    Console.Write($"Vel({velocity.Value.X:F1}, {velocity.Value.Y:F1}, {velocity.Value.Z:F1}) ");
                }
                
                if (entityManager.HasComponent<Health>(entity))
                {
                    var health = entityManager.GetComponent<Health>(entity);
                    Console.Write($"Health({health.Value}) ");
                }
                
                Console.WriteLine();
            }
        }
    }
} 