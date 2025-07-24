using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using ECS.SourceGenerators;

namespace ECS.SourceGenerators
{
    /// <summary>
    /// Helper methods for generating SIMD batch processing code
    /// </summary>
    internal static class SimdBatchGeneratorHelpers
    {
        /// <summary>
        /// Generates SIMD batch processing methods for vector types
        /// </summary>
        public static void GenerateSimdBatchMethods(StringBuilder sourceBuilder, List<ComponentTypeInfo> componentTypes)
        {
            sourceBuilder.AppendLine("using System;");
            sourceBuilder.AppendLine("using System.Runtime.CompilerServices;");
            sourceBuilder.AppendLine("using System.Numerics;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("namespace ECS.Generated");
            sourceBuilder.AppendLine("{");
            sourceBuilder.AppendLine("    /// <summary>");
            sourceBuilder.AppendLine("    /// Generated SIMD batch processing methods");
            sourceBuilder.AppendLine("    /// </summary>");
            sourceBuilder.AppendLine("    public static class SimdBatchProcessors");
            sourceBuilder.AppendLine("    {");
            
            // Generate SIMD batch processors for vector types
            var simdComponents = componentTypes.Where(c => c.IsSimdOptimized).ToList();
            
            foreach (var component in simdComponents)
            {
                GenerateSimdProcessor(sourceBuilder, component);
            }
            
            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine("}");
        }

        /// <summary>
        /// Generates SIMD processor for a specific component type
        /// </summary>
        private static void GenerateSimdProcessor(StringBuilder sourceBuilder, ComponentTypeInfo componentType)
        {
            var typeName = componentType.TypeName;
            var fullTypeName = componentType.FullTypeName;
            var alignment = componentType.Alignment;
            
            sourceBuilder.AppendLine($"        /// <summary>");
            sourceBuilder.AppendLine($"        /// SIMD-optimized processor for {typeName}");
            sourceBuilder.AppendLine($"        /// </summary>");
            sourceBuilder.AppendLine($"        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sourceBuilder.AppendLine($"        public static void Process{typeName}Simd({fullTypeName}[] data, int count, float deltaTime = 1.0f)");
            sourceBuilder.AppendLine($"        {{");
            sourceBuilder.AppendLine($"            // Ensure {alignment}-byte alignment for SIMD operations");
            sourceBuilder.AppendLine($"            if (Vector.IsHardwareAccelerated && count >= Vector<{fullTypeName}>.Count)");
            sourceBuilder.AppendLine($"            {{");
            sourceBuilder.AppendLine($"                Process{typeName}SimdVectorized(data, count, deltaTime);");
            sourceBuilder.AppendLine($"            }}");
            sourceBuilder.AppendLine($"            else");
            sourceBuilder.AppendLine($"            {{");
            sourceBuilder.AppendLine($"                Process{typeName}Scalar(data, count, deltaTime);");
            sourceBuilder.AppendLine($"            }}");
            sourceBuilder.AppendLine($"        }}");
            sourceBuilder.AppendLine();
            
            // Generate vectorized implementation
            sourceBuilder.AppendLine($"        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sourceBuilder.AppendLine($"        private static void Process{typeName}SimdVectorized({fullTypeName}[] data, int count, float deltaTime)");
            sourceBuilder.AppendLine($"        {{");
            sourceBuilder.AppendLine($"            // SIMD vectorized processing implementation");
            sourceBuilder.AppendLine($"            // This would be implemented based on the specific component type");
            sourceBuilder.AppendLine($"        }}");
            sourceBuilder.AppendLine();
            
            // Generate scalar fallback
            sourceBuilder.AppendLine($"        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sourceBuilder.AppendLine($"        private static void Process{typeName}Scalar({fullTypeName}[] data, int count, float deltaTime)");
            sourceBuilder.AppendLine($"        {{");
            sourceBuilder.AppendLine($"            // Scalar fallback implementation");
            sourceBuilder.AppendLine($"            for (int i = 0; i < count; i++)");
            sourceBuilder.AppendLine($"            {{");
            sourceBuilder.AppendLine($"                // Process individual {typeName} component");
            sourceBuilder.AppendLine($"            }}");
            sourceBuilder.AppendLine($"        }}");
            sourceBuilder.AppendLine();
        }
    }
} 