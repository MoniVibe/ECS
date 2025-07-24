using System;
using System.IO;

namespace ECS
{
    /// <summary>
    /// Delegate contracts for component operations
    /// </summary>
    public static class DelegateContracts
    {
        #region Component Access Delegates

        /// <summary>
        /// Type-safe component accessor delegate
        /// </summary>
        public delegate T ComponentAccessor<T>(EntityManager manager, EntityId entity) where T : struct;

        /// <summary>
        /// Type-safe component setter delegate
        /// </summary>
        public delegate void ComponentSetter<T>(EntityManager manager, EntityId entity, T component) where T : struct;

        /// <summary>
        /// Type-safe component copier delegate
        /// </summary>
        public delegate void ComponentCopier<T>(ArchetypeChunk fromChunk, int fromIndex, ArchetypeChunk toChunk, int toIndex, ComponentType componentType) where T : struct;

        /// <summary>
        /// Type-safe component serializer delegate
        /// </summary>
        public delegate void ComponentSerializer<T>(BinaryWriter writer, T component) where T : struct;

        /// <summary>
        /// Type-safe component deserializer delegate
        /// </summary>
        public delegate T ComponentDeserializer<T>(BinaryReader reader) where T : struct;

        #endregion

        #region Factory Interfaces

        /// <summary>
        /// Interface for creating component accessor delegates
        /// </summary>
        public interface IComponentAccessorFactory
        {
            ComponentAccessor<T> CreateAccessor<T>() where T : struct;
        }

        /// <summary>
        /// Interface for creating component setter delegates
        /// </summary>
        public interface IComponentSetterFactory
        {
            ComponentSetter<T> CreateSetter<T>() where T : struct;
        }

        /// <summary>
        /// Interface for creating component copier delegates
        /// </summary>
        public interface IComponentCopierFactory
        {
            ComponentCopier<T> CreateCopier<T>() where T : struct;
        }

        /// <summary>
        /// Interface for creating component serializer delegates
        /// </summary>
        public interface IComponentSerializerFactory
        {
            ComponentSerializer<T> CreateSerializer<T>() where T : struct;
        }

        /// <summary>
        /// Interface for creating component deserializer delegates
        /// </summary>
        public interface IComponentDeserializerFactory
        {
            ComponentDeserializer<T> CreateDeserializer<T>() where T : struct;
        }

        #endregion
    }
} 