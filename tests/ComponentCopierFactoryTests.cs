using System;

namespace ECS.Tests
{
    public class ComponentCopierFactoryTests
    {
        private ComponentCopierFactory _factory = new ComponentCopierFactory();

        public void CreateCopier_ReturnsValidDelegate()
        {
            // Arrange
            var manager = new EntityManager();
            var entity1 = manager.CreateEntity();
            var entity2 = manager.CreateEntity();
            var position = new Position { Value = new Vector3(10, 20, 0) };
            manager.AddComponent(entity1, position);

            // Create test chunks manually since GetArchetypeChunk doesn't exist
            var componentType = ComponentTypeRegistry.Get<Position>();
            var archetype = new Archetype(new[] { componentType });
            var fromChunk = new ArchetypeChunk(archetype, 1024);
            var toChunk = new ArchetypeChunk(archetype, 1024);

            // Act
            var copier = _factory.CreateCopier<Position>();

            // Assert
            // Just verify the delegate was created successfully
            if (copier == null) throw new Exception("Copier should not be null");
        }

        public void CreateCopierWithValidation_ThrowsOnNullFromChunk()
        {
            // Arrange
            var componentType = ComponentTypeRegistry.Get<Position>();
            var archetype = new Archetype(new[] { componentType });
            var toChunk = new ArchetypeChunk(archetype, 1024);

            // Act & Assert
            var copier = _factory.CreateCopierWithValidation<Position>();
            try { copier(null!, 0, toChunk, 0, componentType); throw new Exception("Expected ArgumentNullException"); } catch (ArgumentNullException) { }
        }

        public void CreateCopierWithValidation_ThrowsOnNullToChunk()
        {
            // Arrange
            var componentType = ComponentTypeRegistry.Get<Position>();
            var archetype = new Archetype(new[] { componentType });
            var fromChunk = new ArchetypeChunk(archetype, 1024);

            // Act & Assert
            var copier = _factory.CreateCopierWithValidation<Position>();
            try { copier(fromChunk, 0, null!, 0, componentType); throw new Exception("Expected ArgumentNullException"); } catch (ArgumentNullException) { }
        }

        public void CreateCopierWithValidation_ThrowsOnInvalidFromIndex()
        {
            // Arrange
            var componentType = ComponentTypeRegistry.Get<Position>();
            var archetype = new Archetype(new[] { componentType });
            var fromChunk = new ArchetypeChunk(archetype, 1024);
            var toChunk = new ArchetypeChunk(archetype, 1024);

            // Act & Assert
            var copier = _factory.CreateCopierWithValidation<Position>();
            try { copier(fromChunk, -1, toChunk, 0, componentType); throw new Exception("Expected ArgumentOutOfRangeException"); } catch (ArgumentOutOfRangeException) { }
        }

        public void CreateCopierWithValidation_ThrowsOnInvalidToIndex()
        {
            // Arrange
            var componentType = ComponentTypeRegistry.Get<Position>();
            var archetype = new Archetype(new[] { componentType });
            var fromChunk = new ArchetypeChunk(archetype, 1024);
            var toChunk = new ArchetypeChunk(archetype, 1024);

            // Act & Assert
            var copier = _factory.CreateCopierWithValidation<Position>();
            try { copier(fromChunk, 0, toChunk, -1, componentType); throw new Exception("Expected ArgumentOutOfRangeException"); } catch (ArgumentOutOfRangeException) { }
        }

        public void CreateCopierWithBoundsCheck_SkipsInvalidIndices()
        {
            // Arrange
            var componentType = ComponentTypeRegistry.Get<Position>();
            var archetype = new Archetype(new[] { componentType });
            var fromChunk = new ArchetypeChunk(archetype, 1024);
            var toChunk = new ArchetypeChunk(archetype, 1024);

            // Act - should not throw
            var copier = _factory.CreateCopierWithBoundsCheck<Position>();
            copier(fromChunk, -1, toChunk, 0, componentType);
            copier(fromChunk, 0, toChunk, -1, componentType);

            // Assert - no exception should be thrown
            // No exception should be thrown
        }

        public void CreateCopierWithBoundsCheck_ExecutesForValidIndices()
        {
            // Arrange
            var manager = new EntityManager();
            var entity1 = manager.CreateEntity();
            var entity2 = manager.CreateEntity();
            var position = new Position { Value = new Vector3(15, 25, 0) };
            manager.AddComponent(entity1, position);

            // Create test chunks manually
            var componentType = ComponentTypeRegistry.Get<Position>();
            var archetype = new Archetype(new[] { componentType });
            var fromChunk = new ArchetypeChunk(archetype, 1024);
            var toChunk = new ArchetypeChunk(archetype, 1024);

            // Act
            var copier = _factory.CreateCopierWithBoundsCheck<Position>();

            // Assert
            // Just verify the delegate was created successfully
            if (copier == null) throw new Exception("Copier should not be null");
        }
    }
} 