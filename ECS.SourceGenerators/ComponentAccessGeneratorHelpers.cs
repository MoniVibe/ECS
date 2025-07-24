using System;
using System.Collections.Generic;
using System.Text;
using System.Linq; // Added for .Where()
using ECS.SourceGenerators;

namespace ECS.SourceGenerators
{
    /// <summary>
    /// Helper methods for generating component access code
    /// </summary>
    internal static class ComponentAccessGeneratorHelpers
    {
        /// <summary>
        /// Generates optimized component access methods for each component type
        /// </summary>
        public static void GenerateComponentAccessMethods(StringBuilder sourceBuilder, List<ComponentTypeInfo> componentTypes)
        {
            // Add using statements
            sourceBuilder.AppendLine("using System;");
            sourceBuilder.AppendLine("using System.Collections.Generic;");
            sourceBuilder.AppendLine("using System.Runtime.CompilerServices;");
            sourceBuilder.AppendLine("using System.Runtime.InteropServices;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("namespace ECS.Generated");
            sourceBuilder.AppendLine("{");
            
            // Generate optimized access methods for each component type
            foreach (var componentType in componentTypes)
            {
                GenerateComponentAccessClass(sourceBuilder, componentType);
            }
            
            // Generate the main accessor class
            GenerateMainAccessorClass(sourceBuilder, componentTypes);
            
            sourceBuilder.AppendLine("}");
        }

        /// <summary>
        /// Generates access class for a specific component type
        /// </summary>
        private static void GenerateComponentAccessClass(StringBuilder sourceBuilder, ComponentTypeInfo componentType)
        {
            var typeName = componentType.TypeName;
            var fullTypeName = componentType.FullTypeName;
            var isSimdOptimized = componentType.IsSimdOptimized;
            var alignment = componentType.Alignment;
            
            sourceBuilder.AppendLine($"    /// <summary>");
            sourceBuilder.AppendLine($"    /// Generated optimized access methods for {typeName}");
            sourceBuilder.AppendLine($"    /// </summary>");
            sourceBuilder.AppendLine($"    public static class {typeName}Accessor");
            sourceBuilder.AppendLine($"    {{");
            
            // Generate GetComponent method with readonly support
            sourceBuilder.AppendLine($"        /// <summary>");
            sourceBuilder.AppendLine($"        /// Optimized component getter for {typeName}");
            sourceBuilder.AppendLine($"        /// </summary>");
            sourceBuilder.AppendLine($"        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sourceBuilder.AppendLine($"        public static {fullTypeName} GetComponent(this EntityManager manager, EntityId entity)");
            sourceBuilder.AppendLine($"        {{");
            sourceBuilder.AppendLine($"            return manager.GetComponent<{fullTypeName}>(entity);");
            sourceBuilder.AppendLine($"        }}");
            sourceBuilder.AppendLine();
            
            // Generate GetComponentRef method for direct access
            sourceBuilder.AppendLine($"        /// <summary>");
            sourceBuilder.AppendLine($"        /// Optimized component getter with ref access for {typeName}");
            sourceBuilder.AppendLine($"        /// </summary>");
            sourceBuilder.AppendLine($"        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sourceBuilder.AppendLine($"        public static ref readonly {fullTypeName} GetComponentRef(this EntityManager manager, EntityId entity)");
            sourceBuilder.AppendLine($"        {{");
            sourceBuilder.AppendLine($"            return ref manager.GetComponentRef<{fullTypeName}>(entity);");
            sourceBuilder.AppendLine($"        }}");
            sourceBuilder.AppendLine();
            
            // Generate SetComponent method
            sourceBuilder.AppendLine($"        /// <summary>");
            sourceBuilder.AppendLine($"        /// Optimized component setter for {typeName}");
            sourceBuilder.AppendLine($"        /// </summary>");
            sourceBuilder.AppendLine($"        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sourceBuilder.AppendLine($"        public static void SetComponent(this EntityManager manager, EntityId entity, {fullTypeName} component)");
            sourceBuilder.AppendLine($"        {{");
            sourceBuilder.AppendLine($"            manager.SetComponent(entity, component);");
            sourceBuilder.AppendLine($"        }}");
            sourceBuilder.AppendLine();
            
            // Generate SetComponentRef method for in-place modification
            sourceBuilder.AppendLine($"        /// <summary>");
            sourceBuilder.AppendLine($"        /// Optimized component setter with ref access for {typeName}");
            sourceBuilder.AppendLine($"        /// </summary>");
            sourceBuilder.AppendLine($"        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sourceBuilder.AppendLine($"        public static ref {fullTypeName} SetComponentRef(this EntityManager manager, EntityId entity)");
            sourceBuilder.AppendLine($"        {{");
            sourceBuilder.AppendLine($"            return ref manager.SetComponentRef<{fullTypeName}>(entity);");
            sourceBuilder.AppendLine($"        }}");
            sourceBuilder.AppendLine();
            
            // Generate HasComponent method
            sourceBuilder.AppendLine($"        /// <summary>");
            sourceBuilder.AppendLine($"        /// Optimized component checker for {typeName}");
            sourceBuilder.AppendLine($"        /// </summary>");
            sourceBuilder.AppendLine($"        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sourceBuilder.AppendLine($"        public static bool HasComponent(this EntityManager manager, EntityId entity)");
            sourceBuilder.AppendLine($"        {{");
            sourceBuilder.AppendLine($"            return manager.HasComponent<{fullTypeName}>(entity);");
            sourceBuilder.AppendLine($"        }}");
            sourceBuilder.AppendLine();
            
            // Generate SIMD-optimized batch processing if applicable
            if (isSimdOptimized)
            {
                GenerateSimdBatchMethods(sourceBuilder, componentType);
            }
            
            sourceBuilder.AppendLine($"    }}");
            sourceBuilder.AppendLine();
        }

        /// <summary>
        /// Generates SIMD batch processing methods for a component type
        /// </summary>
        private static void GenerateSimdBatchMethods(StringBuilder sourceBuilder, ComponentTypeInfo componentType)
        {
            var typeName = componentType.TypeName;
            var fullTypeName = componentType.FullTypeName;
            var alignment = componentType.Alignment;
            
            sourceBuilder.AppendLine($"        /// <summary>");
            sourceBuilder.AppendLine($"        /// SIMD-optimized batch processing for {typeName}");
            sourceBuilder.AppendLine($"        /// </summary>");
            sourceBuilder.AppendLine($"        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sourceBuilder.AppendLine($"        public static void ProcessSimdBatch(this EntityManager manager, Action<{fullTypeName}[], int> processor)");
            sourceBuilder.AppendLine($"        {{");
            sourceBuilder.AppendLine($"            manager.ProcessHotComponentsBatch<{fullTypeName}>(processor);");
            sourceBuilder.AppendLine($"        }}");
            sourceBuilder.AppendLine();
            
            sourceBuilder.AppendLine($"        /// <summary>");
            sourceBuilder.AppendLine($"        /// SIMD-optimized batch processing with alignment for {typeName}");
            sourceBuilder.AppendLine($"        /// </summary>");
            sourceBuilder.AppendLine($"        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sourceBuilder.AppendLine($"        public static void ProcessSimdBatchAligned(this EntityManager manager, Action<{fullTypeName}[], int> processor)");
            sourceBuilder.AppendLine($"        {{");
            sourceBuilder.AppendLine($"            // Ensure {alignment}-byte alignment for SIMD operations");
            sourceBuilder.AppendLine($"            manager.ProcessHotComponentsSimd<{fullTypeName}>(processor);");
            sourceBuilder.AppendLine($"        }}");
            sourceBuilder.AppendLine();
        }

        /// <summary>
        /// Generates the main accessor class with batch processing methods
        /// </summary>
        private static void GenerateMainAccessorClass(StringBuilder sourceBuilder, List<ComponentTypeInfo> componentTypes)
        {
            sourceBuilder.AppendLine($"    /// <summary>");
            sourceBuilder.AppendLine($"    /// Main accessor class with all generated component access methods");
            sourceBuilder.AppendLine($"    /// </summary>");
            sourceBuilder.AppendLine($"    public static class ComponentAccessors");
            sourceBuilder.AppendLine($"    {{");
            
            // Generate batch processing methods
            GenerateBatchProcessingMethods(sourceBuilder, componentTypes);
            
            sourceBuilder.AppendLine($"    }}");
        }

        /// <summary>
        /// Generates batch processing methods for hot components
        /// </summary>
        private static void GenerateBatchProcessingMethods(StringBuilder sourceBuilder, List<ComponentTypeInfo> componentTypes)
        {
            // Generate generic batch processing for hot components
            var hotComponents = componentTypes.Where(c => c.IsHot).ToList();
            
            foreach (var component in hotComponents)
            {
                var typeName = component.TypeName;
                var fullTypeName = component.FullTypeName;
                
                sourceBuilder.AppendLine($"        /// <summary>");
                sourceBuilder.AppendLine($"        /// Batch processing for {typeName}");
                sourceBuilder.AppendLine($"        /// </summary>");
                sourceBuilder.AppendLine($"        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
                sourceBuilder.AppendLine($"        public static void ProcessBatch(this EntityManager manager, Action<{fullTypeName}> processor)");
                sourceBuilder.AppendLine($"        {{");
                sourceBuilder.AppendLine($"            manager.ProcessBatchOptimized<{fullTypeName}>(processor);");
                sourceBuilder.AppendLine($"        }}");
                sourceBuilder.AppendLine();
            }
        }
    }
} 