using System;
using System.Runtime.CompilerServices;

namespace ECS
{
    /// <summary>
    /// Factory for creating component accessor delegates
    /// </summary>
    public class ComponentAccessorFactory : DelegateContracts.IComponentAccessorFactory
    {
        /// <summary>
        /// Creates a component accessor delegate for the specified type
        /// </summary>
        public DelegateContracts.ComponentAccessor<T> CreateAccessor<T>() where T : struct
        {
            return (manager, entity) => manager.GetComponent<T>(entity);
        }

        /// <summary>
        /// Creates a component accessor delegate with validation
        /// </summary>
        public DelegateContracts.ComponentAccessor<T> CreateAccessorWithValidation<T>() where T : struct
        {
            return (manager, entity) =>
            {
                if (manager == null)
                    throw new ArgumentNullException(nameof(manager));
                
                if (!manager.HasComponent<T>(entity))
                    throw new InvalidOperationException($"Entity {entity} does not have component of type {typeof(T).Name}");
                
                return manager.GetComponent<T>(entity);
            };
        }

        /// <summary>
        /// Creates a component accessor delegate with default value fallback
        /// </summary>
        public DelegateContracts.ComponentAccessor<T> CreateAccessorWithDefault<T>() where T : struct
        {
            return (manager, entity) =>
            {
                if (manager == null)
                    throw new ArgumentNullException(nameof(manager));
                
                return manager.HasComponent<T>(entity) ? manager.GetComponent<T>(entity) : default(T);
            };
        }
    }
} 