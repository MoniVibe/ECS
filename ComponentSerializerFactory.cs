using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ECS
{
    /// <summary>
    /// Factory for creating component serializer delegates
    /// </summary>
    public class ComponentSerializerFactory : DelegateContracts.IComponentSerializerFactory
    {
        /// <summary>
        /// Creates a component serializer delegate for the specified type
        /// </summary>
        public DelegateContracts.ComponentSerializer<T> CreateSerializer<T>() where T : struct
        {
            return (writer, component) =>
            {
                var bytes = new byte[Marshal.SizeOf<T>()];
                var handle = GCHandle.Alloc(component, GCHandleType.Pinned);
                try
                {
                    Marshal.Copy(handle.AddrOfPinnedObject(), bytes, 0, bytes.Length);
                    writer.Write(bytes);
                }
                finally
                {
                    handle.Free();
                }
            };
        }

        /// <summary>
        /// Creates a component serializer delegate with validation
        /// </summary>
        public DelegateContracts.ComponentSerializer<T> CreateSerializerWithValidation<T>() where T : struct
        {
            return (writer, component) =>
            {
                if (writer == null)
                    throw new ArgumentNullException(nameof(writer));
                
                var bytes = new byte[Marshal.SizeOf<T>()];
                var handle = GCHandle.Alloc(component, GCHandleType.Pinned);
                try
                {
                    Marshal.Copy(handle.AddrOfPinnedObject(), bytes, 0, bytes.Length);
                    writer.Write(bytes);
                }
                finally
                {
                    handle.Free();
                }
            };
        }

        /// <summary>
        /// Creates a component serializer delegate with size prefix
        /// </summary>
        public DelegateContracts.ComponentSerializer<T> CreateSerializerWithSizePrefix<T>() where T : struct
        {
            return (writer, component) =>
            {
                var size = Marshal.SizeOf<T>();
                writer.Write(size);
                
                var bytes = new byte[size];
                var handle = GCHandle.Alloc(component, GCHandleType.Pinned);
                try
                {
                    Marshal.Copy(handle.AddrOfPinnedObject(), bytes, 0, bytes.Length);
                    writer.Write(bytes);
                }
                finally
                {
                    handle.Free();
                }
            };
        }
    }
} 