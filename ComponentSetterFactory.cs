using System;
using System.Runtime.CompilerServices;

namespace ECS
{
    /// <summary>
    /// Factory for creating component setter delegates
    /// </summary>
    public class ComponentSetterFactory : DelegateContracts.IComponentSetterFactory
    {
        /// <summary>
        /// Creates a component setter delegate for the specified type
        /// </summary>
        public DelegateContracts.ComponentSetter<T> CreateSetter<T>() where T : struct
        {
            return (manager, entity, component) => manager.SetComponent(entity, component);
        }

        /// <summary>
        /// Creates a component setter delegate with validation
        /// </summary>
        public DelegateContracts.ComponentSetter<T> CreateSetterWithValidation<T>() where T : struct
        {
            return (manager, entity, component) =>
            {
                if (manager == null)
                    throw new ArgumentNullException(nameof(manager));
                
                if (!manager.HasComponent<T>(entity))
                    throw new InvalidOperationException($"Entity {entity} does not have component of type {typeof(T).Name}");
                
                manager.SetComponent(entity, component);
            };
        }

        /// <summary>
        /// Creates a component setter delegate that adds the component if it doesn't exist
        /// </summary>
        public DelegateContracts.ComponentSetter<T> CreateSetterWithAdd<T>() where T : struct
        {
            return (manager, entity, component) =>
            {
                if (manager == null)
                    throw new ArgumentNullException(nameof(manager));
                
                if (manager.HasComponent<T>(entity))
                {
                    manager.SetComponent(entity, component);
                }
                else
                {
                    manager.AddComponent(entity, component);
                }
            };
        }
    }
} 