using System;
using System.Collections.Generic;
using System.Text;
using ECS.SourceGenerators;

namespace ECS.SourceGenerators
{
    /// <summary>
    /// Helper methods for generating component registry code
    /// </summary>
    internal static class ComponentRegistryGeneratorHelpers
    {
        /// <summary>
        /// Generates component registry extensions for all component types
        /// </summary>
        public static void GenerateComponentRegistryExtensions(StringBuilder sourceBuilder, List<ComponentTypeInfo> componentTypes)
        {
            sourceBuilder.AppendLine("using System;");
            sourceBuilder.AppendLine("using System.Collections.Generic;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("namespace ECS.Generated");
            sourceBuilder.AppendLine("{");
            sourceBuilder.AppendLine("    /// <summary>");
            sourceBuilder.AppendLine("    /// Generated component registry extensions");
            sourceBuilder.AppendLine("    /// </summary>");
            sourceBuilder.AppendLine("    public static class ComponentRegistryExtensions");
            sourceBuilder.AppendLine("    {");
            
            foreach (var componentType in componentTypes)
            {
                GenerateComponentRegistration(sourceBuilder, componentType);
            }
            
            GenerateAllComponentsRegistration(sourceBuilder, componentTypes);
            
            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine("}");
        }

        /// <summary>
        /// Generates registration method for a specific component type
        /// </summary>
        private static void GenerateComponentRegistration(StringBuilder sourceBuilder, ComponentTypeInfo componentType)
        {
            var typeName = componentType.TypeName;
            var fullTypeName = componentType.FullTypeName;
            var isHot = componentType.IsHot;
            var isSimdOptimized = componentType.IsSimdOptimized;
            var alignment = componentType.Alignment;
            
            sourceBuilder.AppendLine($"        /// <summary>");
            sourceBuilder.AppendLine($"        /// Register {typeName} component type");
            sourceBuilder.AppendLine($"        /// </summary>");
            sourceBuilder.AppendLine($"        public static void Register{typeName}()");
            sourceBuilder.AppendLine($"        {{");
            sourceBuilder.AppendLine($"            EnhancedComponentType.RegisterComponentType<{fullTypeName}>(");
            sourceBuilder.AppendLine($"                heat: ComponentHeat.{(isHot ? "Hot" : "Cold")},");
            sourceBuilder.AppendLine($"                alignment: {alignment},");
            sourceBuilder.AppendLine($"                isSimdOptimized: {isSimdOptimized.ToString().ToLower()});");
            sourceBuilder.AppendLine($"        }}");
            sourceBuilder.AppendLine();
        }

        /// <summary>
        /// Generates the method to register all component types
        /// </summary>
        private static void GenerateAllComponentsRegistration(StringBuilder sourceBuilder, List<ComponentTypeInfo> componentTypes)
        {
            sourceBuilder.AppendLine("        /// <summary>");
            sourceBuilder.AppendLine("        /// Register all component types");
            sourceBuilder.AppendLine("        /// </summary>");
            sourceBuilder.AppendLine("        public static void RegisterAllComponents()");
            sourceBuilder.AppendLine("        {");
            
            foreach (var componentType in componentTypes)
            {
                sourceBuilder.AppendLine($"            Register{componentType.TypeName}();");
            }
            
            sourceBuilder.AppendLine("        }");
        }
    }
} 