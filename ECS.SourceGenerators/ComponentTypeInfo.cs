namespace ECS.SourceGenerators
{
    /// <summary>
    /// Information about a component type for source generation
    /// </summary>
    public class ComponentTypeInfo
    {
        public string TypeName { get; set; } = "";
        public string FullTypeName { get; set; } = "";
        public bool IsHot { get; set; }
        public bool IsCold { get; set; }
        public bool IsVectorType { get; set; }
        public bool IsSimdOptimized { get; set; }
        public int Alignment { get; set; } = 4;
        public string Namespace { get; set; } = "";
    }
} 