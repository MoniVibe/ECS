using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ECS.SourceGenerators
{
    /// <summary>
    /// Roslyn source generator that creates optimized component access methods at compile time
    /// This eliminates reflection overhead and boxing in dynamic contexts
    /// Enhanced with readonly/ref support, SIMD alignment, and partial component traits
    /// </summary>
    [Generator]
    public class ECSComponentAccessGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            // Register for syntax notifications
            context.RegisterForSyntaxNotifications(() => new ComponentSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // Get the syntax receiver
            if (context.SyntaxReceiver is not ComponentSyntaxReceiver receiver)
                return;

            // Generate optimized component access methods
            GenerateComponentAccessMethods(context, receiver.ComponentTypes);
            
            // Generate component registry extensions
            GenerateComponentRegistryExtensions(context, receiver.ComponentTypes);
            
            // Generate SIMD batch processing methods
            GenerateSimdBatchMethods(context, receiver.ComponentTypes);
            
            // Generate partial component traits
            GeneratePartialComponentTraits(context, receiver.ComponentTypes);
            
            // Generate readonly/ref optimized methods
            GenerateReadonlyRefMethods(context, receiver.ComponentTypes);
        }

        /// <summary>
        /// Static preview mode for easier testing and debugging
        /// </summary>
        public static string GeneratePreview(List<ComponentTypeInfo> componentTypes)
        {
            var sourceBuilder = new StringBuilder();
            
            // Generate all code types for preview
            ComponentAccessGeneratorHelpers.GenerateComponentAccessMethods(sourceBuilder, componentTypes);
            
            var registryBuilder = new StringBuilder();
            ComponentRegistryGeneratorHelpers.GenerateComponentRegistryExtensions(registryBuilder, componentTypes);
            
            var simdBuilder = new StringBuilder();
            SimdBatchGeneratorHelpers.GenerateSimdBatchMethods(simdBuilder, componentTypes);
            
            var traitsBuilder = new StringBuilder();
            PartialComponentTraitsGeneratorHelpers.GeneratePartialComponentTraits(traitsBuilder, componentTypes);
            
            var readonlyBuilder = new StringBuilder();
            ReadonlyRefGeneratorHelpers.GenerateReadonlyRefMethods(readonlyBuilder, componentTypes);
            
            // Combine all generated code
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("// Component Registry Extensions:");
            sourceBuilder.Append(registryBuilder);
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("// SIMD Batch Processors:");
            sourceBuilder.Append(simdBuilder);
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("// Partial Component Traits:");
            sourceBuilder.Append(traitsBuilder);
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("// Readonly Ref Accessors:");
            sourceBuilder.Append(readonlyBuilder);
            
            return sourceBuilder.ToString();
        }

        private void GenerateComponentAccessMethods(GeneratorExecutionContext context, List<ComponentTypeInfo> componentTypes)
        {
            var sourceBuilder = new StringBuilder();
            ComponentAccessGeneratorHelpers.GenerateComponentAccessMethods(sourceBuilder, componentTypes);
            context.AddSource("ECSComponentAccess.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }





        private void GenerateComponentRegistryExtensions(GeneratorExecutionContext context, List<ComponentTypeInfo> componentTypes)
        {
            var sourceBuilder = new StringBuilder();
            ComponentRegistryGeneratorHelpers.GenerateComponentRegistryExtensions(sourceBuilder, componentTypes);
            context.AddSource("ECSComponentRegistry.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

        private void GenerateSimdBatchMethods(GeneratorExecutionContext context, List<ComponentTypeInfo> componentTypes)
        {
            var sourceBuilder = new StringBuilder();
            SimdBatchGeneratorHelpers.GenerateSimdBatchMethods(sourceBuilder, componentTypes);
            context.AddSource("ECSSimdBatchProcessors.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }



        private void GeneratePartialComponentTraits(GeneratorExecutionContext context, List<ComponentTypeInfo> componentTypes)
        {
            var sourceBuilder = new StringBuilder();
            PartialComponentTraitsGeneratorHelpers.GeneratePartialComponentTraits(sourceBuilder, componentTypes);
            context.AddSource("ECSPartialComponentTraits.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }



        private void GenerateReadonlyRefMethods(GeneratorExecutionContext context, List<ComponentTypeInfo> componentTypes)
        {
            var sourceBuilder = new StringBuilder();
            ReadonlyRefGeneratorHelpers.GenerateReadonlyRefMethods(sourceBuilder, componentTypes);
            context.AddSource("ECSReadonlyRefAccessors.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }


    }

    public class ComponentSyntaxReceiver : ISyntaxReceiver
    {
        public List<ComponentTypeInfo> ComponentTypes { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is StructDeclarationSyntax structDecl)
            {
                if (IsComponentType(structDecl))
                {
                    var componentType = new ComponentTypeInfo
                    {
                        TypeName = structDecl.Identifier.ValueText,
                        FullTypeName = GetFullTypeName(structDecl),
                        Namespace = GetNamespace(structDecl),
                        IsHot = HasHotComponentAttribute(structDecl),
                        IsCold = HasColdComponentAttribute(structDecl),
                        IsVectorType = IsVectorType(structDecl),
                        IsSimdOptimized = HasSimdLayoutAttribute(structDecl),
                        Alignment = GetSimdAlignment(structDecl)
                    };
                    
                    ComponentTypes.Add(componentType);
                }
            }
        }

        private bool IsComponentType(StructDeclarationSyntax structDecl)
        {
            // Check if it's a component type by looking for component attributes
            return HasComponentAttribute(structDecl) || 
                   HasHotComponentAttribute(structDecl) || 
                   HasColdComponentAttribute(structDecl) ||
                   IsVectorType(structDecl);
        }

        private bool HasComponentAttribute(StructDeclarationSyntax structDecl)
        {
            return structDecl.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(a => a.Name.ToString().Contains("Component"));
        }

        private bool HasHotComponentAttribute(StructDeclarationSyntax structDecl)
        {
            return structDecl.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(a => a.Name.ToString().Contains("HotComponent"));
        }

        private bool HasColdComponentAttribute(StructDeclarationSyntax structDecl)
        {
            return structDecl.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(a => a.Name.ToString().Contains("ColdComponent"));
        }

        private bool HasSimdLayoutAttribute(StructDeclarationSyntax structDecl)
        {
            return structDecl.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(a => a.Name.ToString().Contains("SimdLayout"));
        }

        private int GetSimdAlignment(StructDeclarationSyntax structDecl)
        {
            var simdAttr = structDecl.AttributeLists
                .SelectMany(al => al.Attributes)
                .FirstOrDefault(a => a.Name.ToString().Contains("SimdLayout"));
                
            if (simdAttr?.ArgumentList?.Arguments.Count > 0)
            {
                var arg = simdAttr.ArgumentList.Arguments[0];
                if (int.TryParse(arg.Expression.ToString(), out var alignment))
                    return alignment;
            }
            
            return 16; // Default SIMD alignment
        }

        private bool IsVectorType(StructDeclarationSyntax structDecl)
        {
            var name = structDecl.Identifier.ValueText;
            return name.Contains("Vector") || name.Contains("Position") || name.Contains("Velocity") || 
                   name.Contains("Rotation") || name.Contains("Scale");
        }

        private string GetFullTypeName(StructDeclarationSyntax structDecl)
        {
            var namespaceName = GetNamespace(structDecl);
            var typeName = structDecl.Identifier.ValueText;
            
            return string.IsNullOrEmpty(namespaceName) ? typeName : $"{namespaceName}.{typeName}";
        }

        private string GetNamespace(StructDeclarationSyntax structDecl)
        {
            var namespaceDecl = structDecl.Ancestors()
                .OfType<NamespaceDeclarationSyntax>()
                .FirstOrDefault();
                
            return namespaceDecl?.Name.ToString() ?? "";
        }
    }

} 