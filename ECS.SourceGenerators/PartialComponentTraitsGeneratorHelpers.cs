using System;
using System.Collections.Generic;
using System.Text;
using ECS.SourceGenerators;

namespace ECS.SourceGenerators
{
    /// <summary>
    /// Helper methods for generating partial component traits code
    /// </summary>
    internal static class PartialComponentTraitsGeneratorHelpers
    {
        /// <summary>
        /// Generates partial component traits for compile-time optimization
        /// </summary>
        public static void GeneratePartialComponentTraits(StringBuilder sourceBuilder, List<ComponentTypeInfo> componentTypes)
        {
            sourceBuilder.AppendLine("using System;");
            sourceBuilder.AppendLine("using System.Runtime.CompilerServices;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("namespace ECS.Generated");
            sourceBuilder.AppendLine("{");
            sourceBuilder.AppendLine("    /// <summary>");
            sourceBuilder.AppendLine("    /// Generated partial component traits for compile-time optimization");
            sourceBuilder.AppendLine("    /// </summary>");
            
            foreach (var componentType in componentTypes)
            {
                GeneratePartialComponentTrait(sourceBuilder, componentType);
            }
            
            sourceBuilder.AppendLine("}");
        }

        /// <summary>
        /// Generates partial component trait for a specific component type
        /// </summary>
        private static void GeneratePartialComponentTrait(StringBuilder sourceBuilder, ComponentTypeInfo componentType)
        {
            var typeName = componentType.TypeName;
            var fullTypeName = componentType.FullTypeName;
            var isHot = componentType.IsHot;
            var isSimdOptimized = componentType.IsSimdOptimized;
            var alignment = componentType.Alignment;
            
            sourceBuilder.AppendLine($"    /// <summary>");
            sourceBuilder.AppendLine($"    /// Partial component trait for {typeName}");
            sourceBuilder.AppendLine($"    /// </summary>");
            sourceBuilder.AppendLine($"    public static partial class {typeName}Trait");
            sourceBuilder.AppendLine($"    {{");
            sourceBuilder.AppendLine($"        /// <summary>");
            sourceBuilder.AppendLine($"        /// Component heat classification");
            sourceBuilder.AppendLine($"        /// </summary>");
            sourceBuilder.AppendLine($"        public const ComponentHeat Heat = ComponentHeat.{(isHot ? "Hot" : "Cold")};");
            sourceBuilder.AppendLine();
            
            sourceBuilder.AppendLine($"        /// <summary>");
            sourceBuilder.AppendLine($"        /// Memory alignment requirement");
            sourceBuilder.AppendLine($"        /// </summary>");
            sourceBuilder.AppendLine($"        public const int Alignment = {alignment};");
            sourceBuilder.AppendLine();
            
            sourceBuilder.AppendLine($"        /// <summary>");
            sourceBuilder.AppendLine($"        /// SIMD optimization support");
            sourceBuilder.AppendLine($"        /// </summary>");
            sourceBuilder.AppendLine($"        public const bool IsSimdOptimized = {isSimdOptimized.ToString().ToLower()};");
            sourceBuilder.AppendLine();
            
            sourceBuilder.AppendLine($"        /// <summary>");
            sourceBuilder.AppendLine($"        /// Component type size");
            sourceBuilder.AppendLine($"        /// </summary>");
            sourceBuilder.AppendLine($"        public static readonly int Size = System.Runtime.InteropServices.Marshal.SizeOf<{fullTypeName}>();");
            sourceBuilder.AppendLine();
            
            sourceBuilder.AppendLine($"        /// <summary>");
            sourceBuilder.AppendLine($"        /// Optimized component access");
            sourceBuilder.AppendLine($"        /// </summary>");
            sourceBuilder.AppendLine($"        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sourceBuilder.AppendLine($"        public static {fullTypeName} GetComponent(this EntityManager manager, EntityId entity)");
            sourceBuilder.AppendLine($"        {{");
            sourceBuilder.AppendLine($"            return manager.GetComponent<{fullTypeName}>(entity);");
            sourceBuilder.AppendLine($"        }}");
            sourceBuilder.AppendLine();
            
            sourceBuilder.AppendLine($"        /// <summary>");
            sourceBuilder.AppendLine($"        /// Optimized component setting");
            sourceBuilder.AppendLine($"        /// </summary>");
            sourceBuilder.AppendLine($"        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sourceBuilder.AppendLine($"        public static void SetComponent(this EntityManager manager, EntityId entity, {fullTypeName} component)");
            sourceBuilder.AppendLine($"        {{");
            sourceBuilder.AppendLine($"            manager.SetComponent(entity, component);");
            sourceBuilder.AppendLine($"        }}");
            sourceBuilder.AppendLine();
            
            sourceBuilder.AppendLine($"    }}");
            sourceBuilder.AppendLine();
        }
    }
} 