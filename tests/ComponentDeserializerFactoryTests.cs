using System;
using System.IO;

namespace ECS.Tests
{
    public class ComponentDeserializerFactoryTests
    {
        private ComponentDeserializerFactory _factory = new ComponentDeserializerFactory();

        public void CreateDeserializer_ReturnsValidDelegate()
        {
            // Arrange
            var originalPosition = new Position { Value = new Vector3(10, 20, 30) };
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            
            // Write test data
            writer.Write(originalPosition.Value.X);
            writer.Write(originalPosition.Value.Y);
            writer.Write(originalPosition.Value.Z);
            
            stream.Position = 0;
            using var reader = new BinaryReader(stream);

            // Act
            var deserializer = _factory.CreateDeserializer<Position>();

            // Assert
            if (deserializer == null) throw new Exception("Deserializer should not be null");
            var result = deserializer(reader);
            if (result.Value.X != originalPosition.Value.X) throw new Exception($"Expected X={originalPosition.Value.X}, got {result.Value.X}");
        }

        public void CreateDeserializerWithValidation_ThrowsOnNullReader()
        {
            // Arrange
            var position = new Position { Value = new Vector3(10, 20, 30) };

            // Act & Assert
            var deserializer = _factory.CreateDeserializerWithValidation<Position>();
            try
            {
                deserializer(null!);
                throw new Exception("Expected ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
        }

        public void CreateDeserializerWithValidation_ThrowsOnInsufficientBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            // Write incomplete data - only 4 bytes instead of the full struct size
            writer.Write(10.0f); // Only one float, but Position struct needs 12 bytes (3 floats)
            stream.Position = 0;
            using var reader = new BinaryReader(stream);

            // Act & Assert
            var deserializer = _factory.CreateDeserializerWithValidation<Position>();
            try
            {
                deserializer(reader);
                throw new Exception("Expected InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
                // Expected - should throw because we didn't write enough bytes
            }
        }

        public void CreateDeserializerWithValidation_DeserializesCorrectly()
        {
            // Arrange
            var originalPosition = new Position { Value = new Vector3(15, 25, 35) };
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            
            // Write test data
            writer.Write(originalPosition.Value.X);
            writer.Write(originalPosition.Value.Y);
            writer.Write(originalPosition.Value.Z);
            
            stream.Position = 0;
            using var reader = new BinaryReader(stream);

            // Act
            var deserializer = _factory.CreateDeserializerWithValidation<Position>();
            var result = deserializer(reader);

            // Assert
            if (result.Value.X != originalPosition.Value.X) throw new Exception($"Expected X={originalPosition.Value.X}, got {result.Value.X}");
            if (result.Value.Y != originalPosition.Value.Y) throw new Exception($"Expected Y={originalPosition.Value.Y}, got {result.Value.Y}");
            if (result.Value.Z != originalPosition.Value.Z) throw new Exception($"Expected Z={originalPosition.Value.Z}, got {result.Value.Z}");
        }

        public void CreateDeserializerWithSizePrefix_ThrowsOnSizeMismatch()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(100); // Wrong size
            writer.Write(10.0f); // Incomplete data
            stream.Position = 0;
            using var reader = new BinaryReader(stream);

            // Act & Assert
            var deserializer = _factory.CreateDeserializerWithSizePrefix<Position>();
            try
            {
                deserializer(reader);
                throw new Exception("Expected InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
                // Expected
            }
        }

        public void CreateDeserializerWithSizePrefix_DeserializesCorrectly()
        {
            // Arrange
            var originalPosition = new Position { Value = new Vector3(20, 30, 40) };
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            
            // Write size prefix and data
            var dataSize = sizeof(float) * 3; // X, Y, Z
            writer.Write(dataSize);
            writer.Write(originalPosition.Value.X);
            writer.Write(originalPosition.Value.Y);
            writer.Write(originalPosition.Value.Z);
            
            stream.Position = 0;
            using var reader = new BinaryReader(stream);

            // Act
            var deserializer = _factory.CreateDeserializerWithSizePrefix<Position>();
            var result = deserializer(reader);

            // Assert
            if (result.Value.X != originalPosition.Value.X) throw new Exception($"Expected X={originalPosition.Value.X}, got {result.Value.X}");
            if (result.Value.Y != originalPosition.Value.Y) throw new Exception($"Expected Y={originalPosition.Value.Y}, got {result.Value.Y}");
            if (result.Value.Z != originalPosition.Value.Z) throw new Exception($"Expected Z={originalPosition.Value.Z}, got {result.Value.Z}");
        }

        public void CreateDeserializerWithSizePrefix_HandlesMultipleDeserializations()
        {
            // Arrange
            var position1 = new Position { Value = new Vector3(10, 20, 30) };
            var position2 = new Position { Value = new Vector3(40, 50, 60) };
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            
            // Write two positions with size prefixes
            var dataSize = sizeof(float) * 3;
            writer.Write(dataSize);
            writer.Write(position1.Value.X);
            writer.Write(position1.Value.Y);
            writer.Write(position1.Value.Z);
            
            writer.Write(dataSize);
            writer.Write(position2.Value.X);
            writer.Write(position2.Value.Y);
            writer.Write(position2.Value.Z);
            
            stream.Position = 0;
            using var reader = new BinaryReader(stream);

            // Act
            var deserializer = _factory.CreateDeserializerWithSizePrefix<Position>();
            var result1 = deserializer(reader);
            var result2 = deserializer(reader);

            // Assert
            if (result1.Value.X != position1.Value.X) throw new Exception($"Expected X={position1.Value.X}, got {result1.Value.X}");
            if (result2.Value.X != position2.Value.X) throw new Exception($"Expected X={position2.Value.X}, got {result2.Value.X}");
        }
    }
} 