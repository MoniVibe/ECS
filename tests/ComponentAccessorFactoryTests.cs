using System;

namespace ECS.Tests
{
    public class ComponentAccessorFactoryTests
    {
        private ComponentAccessorFactory _factory;

        public ComponentAccessorFactoryTests()
        {
            _factory = new ComponentAccessorFactory();
        }

        public void CreateAccessor_ReturnsValidDelegate()
        {
            // Arrange
            var manager = new EntityManager();
            var entity = manager.CreateEntity();
            var position = new Position { Value = new Vector3(10, 20, 0) };
            manager.AddComponent(entity, position);

            // Act
            var accessor = _factory.CreateAccessor<Position>();

            // Assert
            if (accessor == null) throw new Exception("Accessor should not be null");
            var result = accessor(manager, entity);
            if (result.Value.X != position.Value.X) throw new Exception($"Expected X={position.Value.X}, got {result.Value.X}");
            if (result.Value.Y != position.Value.Y) throw new Exception($"Expected Y={position.Value.Y}, got {result.Value.Y}");
        }

        public void CreateAccessorWithValidation_ThrowsOnNullManager()
        {
            // Arrange
            var entity = new EntityId(1, 0);

            // Act & Assert
            var accessor = _factory.CreateAccessorWithValidation<Position>();
            try
            {
                accessor(null!, entity);
                throw new Exception("Expected ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
        }

        public void CreateAccessorWithValidation_ThrowsOnMissingComponent()
        {
            // Arrange
            var manager = new EntityManager();
            var entity = manager.CreateEntity();

            // Act & Assert
            var accessor = _factory.CreateAccessorWithValidation<Position>();
            try
            {
                accessor(manager, entity);
                throw new Exception("Expected InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
                // Expected
            }
        }

        public void CreateAccessorWithDefault_ReturnsDefaultWhenComponentMissing()
        {
            // Arrange
            var manager = new EntityManager();
            var entity = manager.CreateEntity();

            // Act
            var accessor = _factory.CreateAccessorWithDefault<Position>();
            var result = accessor(manager, entity);

            // Assert
            if (result.Value.X != default(Position).Value.X || result.Value.Y != default(Position).Value.Y)
                throw new Exception("Expected default position");
        }

        public void CreateAccessorWithDefault_ReturnsComponentWhenPresent()
        {
            // Arrange
            var manager = new EntityManager();
            var entity = manager.CreateEntity();
            var position = new Position { Value = new Vector3(15, 25, 0) };
            manager.AddComponent(entity, position);

            // Act
            var accessor = _factory.CreateAccessorWithDefault<Position>();
            var result = accessor(manager, entity);

            // Assert
            if (result.Value.X != position.Value.X) throw new Exception($"Expected X={position.Value.X}, got {result.Value.X}");
            if (result.Value.Y != position.Value.Y) throw new Exception($"Expected Y={position.Value.Y}, got {result.Value.Y}");
        }

        public void CreateAccessorWithDefault_ThrowsOnNullManager()
        {
            // Arrange
            var entity = new EntityId(1, 0);

            // Act & Assert
            var accessor = _factory.CreateAccessorWithDefault<Position>();
            try
            {
                accessor(null!, entity);
                throw new Exception("Expected ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
        }
    }
} 