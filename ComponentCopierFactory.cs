using System;
using System.Runtime.CompilerServices;

namespace ECS
{
    /// <summary>
    /// Factory for creating component copier delegates
    /// </summary>
    public class ComponentCopierFactory : DelegateContracts.IComponentCopierFactory
    {
        /// <summary>
        /// Creates a component copier delegate for the specified type
        /// </summary>
        public DelegateContracts.ComponentCopier<T> CreateCopier<T>() where T : struct
        {
            return (fromChunk, fromIndex, toChunk, toIndex, componentType) =>
            {
                var component = fromChunk.GetComponent<T>(fromIndex, componentType);
                toChunk.SetComponent(toIndex, componentType, component);
            };
        }

        /// <summary>
        /// Creates a component copier delegate with validation
        /// </summary>
        public DelegateContracts.ComponentCopier<T> CreateCopierWithValidation<T>() where T : struct
        {
            return (fromChunk, fromIndex, toChunk, toIndex, componentType) =>
            {
                if (fromChunk == null)
                    throw new ArgumentNullException(nameof(fromChunk));
                
                if (toChunk == null)
                    throw new ArgumentNullException(nameof(toChunk));
                
                if (fromIndex < 0 || fromIndex >= fromChunk.Count)
                    throw new ArgumentOutOfRangeException(nameof(fromIndex));
                
                if (toIndex < 0 || toIndex >= toChunk.Count)
                    throw new ArgumentOutOfRangeException(nameof(toIndex));
                
                var component = fromChunk.GetComponent<T>(fromIndex, componentType);
                toChunk.SetComponent(toIndex, componentType, component);
            };
        }

        /// <summary>
        /// Creates a component copier delegate with bounds checking
        /// </summary>
        public DelegateContracts.ComponentCopier<T> CreateCopierWithBoundsCheck<T>() where T : struct
        {
            return (fromChunk, fromIndex, toChunk, toIndex, componentType) =>
            {
                if (fromIndex >= 0 && fromIndex < fromChunk.Count && 
                    toIndex >= 0 && toIndex < toChunk.Count)
                {
                    var component = fromChunk.GetComponent<T>(fromIndex, componentType);
                    toChunk.SetComponent(toIndex, componentType, component);
                }
            };
        }
    }
} 