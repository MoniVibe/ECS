using System;

namespace ECS.Tests
{
    public class ComponentSetterFactoryTests
    {
        private ComponentSetterFactory _factory;

        public ComponentSetterFactoryTests()
        {
            _factory = new ComponentSetterFactory();
        }

        public void CreateSetter_ReturnsValidDelegate()
        {
            // Arrange
            var manager = new EntityManager();
            var entity = manager.CreateEntity();
            var position = new Position { Value = new Vector3(10, 20, 0) };
            
            // Add the component first so the entity has the component type in its archetype
            manager.AddComponent(entity, position);

            // Act
            var setter = _factory.CreateSetter<Position>();

            // Assert
            if (setter == null) throw new Exception("Setter should not be null");
            setter(manager, entity, position);
            var result = manager.GetComponent<Position>(entity);
            if (result.Value.X != position.Value.X) throw new Exception($"Expected X={position.Value.X}, got {result.Value.X}");
        }

        public void CreateSetterWithValidation_ThrowsOnNullManager()
        {
            // Arrange
            var entity = new EntityId(1, 0);
            var position = new Position { Value = new Vector3(10, 20, 0) };

            // Act & Assert
            var setter = _factory.CreateSetterWithValidation<Position>();
            try
            {
                setter(null!, entity, position);
                throw new Exception("Expected ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
        }

        public void CreateSetterWithValidation_ThrowsOnMissingComponent()
        {
            // Arrange
            var manager = new EntityManager();
            var entity = manager.CreateEntity();
            var position = new Position { Value = new Vector3(10, 20, 0) };

            // Act & Assert
            var setter = _factory.CreateSetterWithValidation<Position>();
            try
            {
                setter(manager, entity, position);
                throw new Exception("Expected InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
                // Expected
            }
        }

        public void CreateSetterWithValidation_SucceedsWhenComponentExists()
        {
            // Arrange
            var manager = new EntityManager();
            var entity = manager.CreateEntity();
            var originalPosition = new Position { Value = new Vector3(5, 5, 0) };
            manager.AddComponent(entity, originalPosition);
            var newPosition = new Position { Value = new Vector3(15, 25, 0) };

            // Act
            var setter = _factory.CreateSetterWithValidation<Position>();
            setter(manager, entity, newPosition);

            // Assert
            var result = manager.GetComponent<Position>(entity);
            if (result.Value.X != newPosition.Value.X) throw new Exception($"Expected X={newPosition.Value.X}, got {result.Value.X}");
        }

        public void CreateSetterWithAdd_AddsComponentWhenMissing()
        {
            // Arrange
            var manager = new EntityManager();
            var entity = manager.CreateEntity();
            var position = new Position { Value = new Vector3(10, 20, 0) };

            // Act
            var setter = _factory.CreateSetterWithAdd<Position>();
            setter(manager, entity, position);

            // Assert
            var result = manager.GetComponent<Position>(entity);
            if (result.Value.X != position.Value.X) throw new Exception($"Expected X={position.Value.X}, got {result.Value.X}");
        }

        public void CreateSetterWithAdd_UpdatesComponentWhenExists()
        {
            // Arrange
            var manager = new EntityManager();
            var entity = manager.CreateEntity();
            var originalPosition = new Position { Value = new Vector3(5, 5, 0) };
            manager.AddComponent(entity, originalPosition);
            var newPosition = new Position { Value = new Vector3(15, 25, 0) };

            // Act
            var setter = _factory.CreateSetterWithAdd<Position>();
            setter(manager, entity, newPosition);

            // Assert
            var result = manager.GetComponent<Position>(entity);
            if (result.Value.X != newPosition.Value.X) throw new Exception($"Expected X={newPosition.Value.X}, got {result.Value.X}");
        }

        public void CreateSetterWithAdd_ThrowsOnNullManager()
        {
            // Arrange
            var entity = new EntityId(1, 0);
            var position = new Position { Value = new Vector3(10, 20, 0) };

            // Act & Assert
            var setter = _factory.CreateSetterWithAdd<Position>();
            try
            {
                setter(null!, entity, position);
                throw new Exception("Expected ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
        }
    }
} 