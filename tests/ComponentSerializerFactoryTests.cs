using System;
using System.IO;

namespace ECS.Tests
{
    public class ComponentSerializerFactoryTests
    {
        private ComponentSerializerFactory _factory = new ComponentSerializerFactory();

        public void CreateSerializer_ReturnsValidDelegate()
        {
            // Arrange
            var position = new Position { Value = new Vector3(10, 20, 30) };
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            // Act
            var serializer = _factory.CreateSerializer<Position>();

            // Assert
            if (serializer == null) throw new Exception("Serializer should not be null");
            serializer(writer, position);
        }

        public void CreateSerializerWithValidation_ThrowsOnNullWriter()
        {
            // Arrange
            var position = new Position { Value = new Vector3(10, 20, 30) };

            // Act & Assert
            var serializer = _factory.CreateSerializerWithValidation<Position>();
            try
            {
                serializer(null!, position);
                throw new Exception("Expected ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
        }

        public void CreateSerializerWithValidation_SerializesCorrectly()
        {
            // Arrange
            var position = new Position { Value = new Vector3(15, 25, 35) };
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            // Act
            var serializer = _factory.CreateSerializerWithValidation<Position>();
            serializer(writer, position);

            // Assert
            if (stream.Length == 0) throw new Exception("Data should be written to stream");
        }

        public void CreateSerializerWithSizePrefix_WritesSizeAndData()
        {
            // Arrange
            var position = new Position { Value = new Vector3(20, 30, 40) };
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            // Act
            var serializer = _factory.CreateSerializerWithSizePrefix<Position>();
            serializer(writer, position);

            // Assert
            if (stream.Length < sizeof(int)) throw new Exception("Size prefix should be written");
        }

        public void CreateSerializerWithSizePrefix_HandlesMultipleSerializations()
        {
            // Arrange
            var position1 = new Position { Value = new Vector3(10, 20, 30) };
            var position2 = new Position { Value = new Vector3(40, 50, 60) };
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            // Act
            var serializer = _factory.CreateSerializerWithSizePrefix<Position>();
            serializer(writer, position1);
            serializer(writer, position2);

            // Assert
            if (stream.Length < sizeof(int) * 2) throw new Exception("Multiple size prefixes should be written");
        }
    }
} 