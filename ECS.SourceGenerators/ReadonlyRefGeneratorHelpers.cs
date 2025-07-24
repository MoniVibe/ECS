using System;
using System.Collections.Generic;
using System.Text;
using ECS.SourceGenerators;

namespace ECS.SourceGenerators
{
    /// <summary>
    /// Helper methods for generating readonly/ref optimized component access code
    /// </summary>
    internal static class ReadonlyRefGeneratorHelpers
    {
        /// <summary>
        /// Generates readonly/ref optimized component access methods
        /// </summary>
        public static void GenerateReadonlyRefMethods(StringBuilder sourceBuilder, List<ComponentTypeInfo> componentTypes)
        {
            sourceBuilder.AppendLine("using System;");
            sourceBuilder.AppendLine("using System.Runtime.CompilerServices;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("namespace ECS.Generated");
            sourceBuilder.AppendLine("{");
            sourceBuilder.AppendLine("    /// <summary>");
            sourceBuilder.AppendLine("    /// Generated readonly/ref optimized component access methods");
            sourceBuilder.AppendLine("    /// </summary>");
            sourceBuilder.AppendLine("    public static class ReadonlyRefAccessors");
            sourceBuilder.AppendLine("    {");
            
            foreach (var componentType in componentTypes)
            {
                GenerateReadonlyRefMethods(sourceBuilder, componentType);
            }
            
            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine("}");
        }

        /// <summary>
        /// Generates readonly/ref methods for a specific component type
        /// </summary>
        private static void GenerateReadonlyRefMethods(StringBuilder sourceBuilder, ComponentTypeInfo componentType)
        {
            var typeName = componentType.TypeName;
            var fullTypeName = componentType.FullTypeName;
            
            sourceBuilder.AppendLine($"        /// <summary>");
            sourceBuilder.AppendLine($"        /// Readonly ref access for {typeName}");
            sourceBuilder.AppendLine($"        /// </summary>");
            sourceBuilder.AppendLine($"        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sourceBuilder.AppendLine($"        public static ref readonly {fullTypeName} Get{typeName}Ref(this EntityManager manager, EntityId entity)");
            sourceBuilder.AppendLine($"        {{");
            sourceBuilder.AppendLine($"            return ref manager.GetComponentRef<{fullTypeName}>(entity);");
            sourceBuilder.AppendLine($"        }}");
            sourceBuilder.AppendLine();
            
            sourceBuilder.AppendLine($"        /// <summary>");
            sourceBuilder.AppendLine($"        /// Mutable ref access for {typeName}");
            sourceBuilder.AppendLine($"        /// </summary>");
            sourceBuilder.AppendLine($"        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sourceBuilder.AppendLine($"        public static ref {fullTypeName} Set{typeName}Ref(this EntityManager manager, EntityId entity)");
            sourceBuilder.AppendLine($"        {{");
            sourceBuilder.AppendLine($"            return ref manager.SetComponentRef<{fullTypeName}>(entity);");
            sourceBuilder.AppendLine($"        }}");
            sourceBuilder.AppendLine();
            
            sourceBuilder.AppendLine($"        /// <summary>");
            sourceBuilder.AppendLine($"        /// Batch readonly ref access for {typeName}");
            sourceBuilder.AppendLine($"        /// </summary>");
            sourceBuilder.AppendLine($"        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sourceBuilder.AppendLine($"        public static void Process{typeName}BatchRef(this EntityManager manager, Action<ref readonly {fullTypeName}> processor)");
            sourceBuilder.AppendLine($"        {{");
            sourceBuilder.AppendLine($"            manager.ProcessBatchOptimized<{fullTypeName}>(component => processor(ref component));");
            sourceBuilder.AppendLine($"        }}");
            sourceBuilder.AppendLine();
        }
    }
} 