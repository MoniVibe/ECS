using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ECS
{
    /// <summary>
    /// Factory for creating component deserializer delegates
    /// </summary>
    public class ComponentDeserializerFactory : DelegateContracts.IComponentDeserializerFactory
    {
        /// <summary>
        /// Creates a component deserializer delegate for the specified type
        /// </summary>
        public DelegateContracts.ComponentDeserializer<T> CreateDeserializer<T>() where T : struct
        {
            return (reader) =>
            {
                var size = Marshal.SizeOf<T>();
                var bytes = reader.ReadBytes(size);
                var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                try
                {
                    return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
                }
                finally
                {
                    handle.Free();
                }
            };
        }

        /// <summary>
        /// Creates a component deserializer delegate with validation
        /// </summary>
        public DelegateContracts.ComponentDeserializer<T> CreateDeserializerWithValidation<T>() where T : struct
        {
            return (reader) =>
            {
                if (reader == null)
                    throw new ArgumentNullException(nameof(reader));
                
                var size = Marshal.SizeOf<T>();
                var bytes = reader.ReadBytes(size);
                
                if (bytes.Length != size)
                    throw new InvalidOperationException($"Expected {size} bytes but got {bytes.Length}");
                
                var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                try
                {
                    return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
                }
                finally
                {
                    handle.Free();
                }
            };
        }

        /// <summary>
        /// Creates a component deserializer delegate with size prefix
        /// </summary>
        public DelegateContracts.ComponentDeserializer<T> CreateDeserializerWithSizePrefix<T>() where T : struct
        {
            return (reader) =>
            {
                var expectedSize = reader.ReadInt32();
                var actualSize = Marshal.SizeOf<T>();
                
                if (expectedSize != actualSize)
                    throw new InvalidOperationException($"Size mismatch: expected {expectedSize} but type size is {actualSize}");
                
                var bytes = reader.ReadBytes(actualSize);
                var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                try
                {
                    return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
                }
                finally
                {
                    handle.Free();
                }
            };
        }
    }
} 