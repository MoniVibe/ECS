using System;
using System.Collections.Generic;

namespace ECS
{
    /// <summary>
    /// Test class to demonstrate the enhanced component type registry functionality
    /// </summary>
    public static class ComponentTypeRegistryTest
    {
        public static void RunTests()
        {
            Console.WriteLine("=== Component Type Registry Tests ===");
            
            // Test 1: Basic heat classification
            TestHeatClassification();
            
            // Test 2: SIMD alignment detection
            TestSimdAlignment();
            
            // Test 3: JSON configuration loading
            TestJsonConfiguration();
            
            // Test 4: Enhanced component type creation
            TestEnhancedComponentType();
            
            // Test 5: Statistics and validation
            TestStatistics();
            
            Console.WriteLine("=== All tests completed ===");
        }
        
        private static void TestHeatClassification()
        {
            Console.WriteLine("\n--- Testing Heat Classification ---");
            
            // Test built-in types
            var positionHeat = ComponentHeatClassifier.GetComponentHeat(typeof(Position));
            var velocityHeat = ComponentHeatClassifier.GetComponentHeat(typeof(Velocity));
            var nameHeat = ComponentHeatClassifier.GetComponentHeat(typeof(Name));
            var descriptionHeat = ComponentHeatClassifier.GetComponentHeat(typeof(Description));
            
            Console.WriteLine($"Position heat: {positionHeat}");
            Console.WriteLine($"Velocity heat: {velocityHeat}");
            Console.WriteLine($"Name heat: {nameHeat}");
            Console.WriteLine($"Description heat: {descriptionHeat}");
            
            // Test custom registration
            ComponentHeatClassifier.RegisterComponentType<CustomHotComponent>(ComponentHeat.Hot);
            ComponentHeatClassifier.RegisterComponentType<CustomColdComponent>(ComponentHeat.Cold);
            
            var customHotHeat = ComponentHeatClassifier.GetComponentHeat(typeof(CustomHotComponent));
            var customColdHeat = ComponentHeatClassifier.GetComponentHeat(typeof(CustomColdComponent));
            
            Console.WriteLine($"CustomHotComponent heat: {customHotHeat}");
            Console.WriteLine($"CustomColdComponent heat: {customColdHeat}");
        }
        
        private static void TestSimdAlignment()
        {
            Console.WriteLine("\n--- Testing SIMD Alignment ---");
            
            // Test built-in types
            var vector3Alignment = SimdAlignmentUtility.GetComponentAlignment(typeof(Vector3));
            var positionAlignment = SimdAlignmentUtility.GetComponentAlignment(typeof(Position));
            var nameAlignment = SimdAlignmentUtility.GetComponentAlignment(typeof(Name));
            
            Console.WriteLine($"Vector3 alignment: {vector3Alignment}");
            Console.WriteLine($"Position alignment: {positionAlignment}");
            Console.WriteLine($"Name alignment: {nameAlignment}");
            
            // Test SIMD optimization detection
            var vector3Simd = SimdAlignmentUtility.IsComponentSimdOptimized(typeof(Vector3));
            var positionSimd = SimdAlignmentUtility.IsComponentSimdOptimized(typeof(Position));
            var nameSimd = SimdAlignmentUtility.IsComponentSimdOptimized(typeof(Name));
            
            Console.WriteLine($"Vector3 SIMD optimized: {vector3Simd}");
            Console.WriteLine($"Position SIMD optimized: {positionSimd}");
            Console.WriteLine($"Name SIMD optimized: {nameSimd}");
            
            // Test custom registration
            SimdAlignmentUtility.RegisterComponentType<CustomSimdComponent>(16, true);
            SimdAlignmentUtility.RegisterComponentType<CustomStandardComponent>(4, false);
            
            var customSimdAlignment = SimdAlignmentUtility.GetComponentAlignment(typeof(CustomSimdComponent));
            var customStandardAlignment = SimdAlignmentUtility.GetComponentAlignment(typeof(CustomStandardComponent));
            
            Console.WriteLine($"CustomSimdComponent alignment: {customSimdAlignment}");
            Console.WriteLine($"CustomStandardComponent alignment: {customStandardAlignment}");
        }
        
        private static void TestJsonConfiguration()
        {
            Console.WriteLine("\n--- Testing JSON Configuration ---");
            
            // Test heat configuration
            ComponentHeatClassifier.LoadHeatConfiguration("component_heat_config.json");
            var heatOverrides = ComponentHeatClassifier.GetHeatOverrides();
            Console.WriteLine($"Loaded {heatOverrides.Count} heat overrides from JSON");
            
            // Test alignment configuration
            SimdAlignmentUtility.LoadAlignmentConfiguration("simd_alignment_config.json");
            var alignmentOverrides = SimdAlignmentUtility.GetAlignmentOverrides();
            Console.WriteLine($"Loaded {alignmentOverrides.Count} alignment overrides from JSON");
            
            // Test adding new overrides
            ComponentHeatClassifier.AddHeatOverride("NewCustomComponent", ComponentHeat.Hot);
            SimdAlignmentUtility.AddAlignmentOverride("NewCustomComponent", 16);
            
            Console.WriteLine("Added new overrides via JSON configuration");
        }
        
        private static void TestEnhancedComponentType()
        {
            Console.WriteLine("\n--- Testing Enhanced Component Type ---");
            
            // Create enhanced component types
            var positionType = new EnhancedComponentType(1, typeof(Position));
            var velocityType = new EnhancedComponentType(2, typeof(Velocity));
            var nameType = new EnhancedComponentType(3, typeof(Name));
            
            Console.WriteLine($"Position: Heat={positionType.Heat}, Alignment={positionType.Alignment}, SIMD={positionType.IsSimdOptimized}");
            Console.WriteLine($"Velocity: Heat={velocityType.Heat}, Alignment={velocityType.Alignment}, SIMD={velocityType.IsSimdOptimized}");
            Console.WriteLine($"Name: Heat={nameType.Heat}, Alignment={nameType.Alignment}, SIMD={nameType.IsSimdOptimized}");
            
            // Test custom component types
            var customHotType = new EnhancedComponentType(4, typeof(CustomHotComponent));
            var customColdType = new EnhancedComponentType(5, typeof(CustomColdComponent));
            
            Console.WriteLine($"CustomHot: Heat={customHotType.Heat}, Alignment={customHotType.Alignment}, SIMD={customHotType.IsSimdOptimized}");
            Console.WriteLine($"CustomCold: Heat={customColdType.Heat}, Alignment={customColdType.Alignment}, SIMD={customColdType.IsSimdOptimized}");
        }
        
        private static void TestStatistics()
        {
            Console.WriteLine("\n--- Testing Statistics ---");
            
            // Get heat classification statistics
            var heatStats = ComponentHeatClassifier.GetStatistics();
            Console.WriteLine($"Heat Classification Stats: Total={heatStats.totalRegistered}, Hot={heatStats.hotComponents}, Cold={heatStats.coldComponents}, Overrides={heatStats.jsonOverrides}");
            
            // Get alignment statistics
            var alignmentStats = SimdAlignmentUtility.GetStatistics();
            Console.WriteLine($"Alignment Stats: Total={alignmentStats.totalRegistered}, SIMD={alignmentStats.simdOptimized}, Overrides={alignmentStats.jsonOverrides}, MaxAlignment={alignmentStats.maxAlignment}");
            
            // Validate configurations
            var heatValid = ComponentHeatClassifier.GetRegisteredHeatClassifications().Count > 0;
            var alignmentValid = SimdAlignmentUtility.ValidateAlignmentConfiguration();
            
            Console.WriteLine($"Heat configuration valid: {heatValid}");
            Console.WriteLine($"Alignment configuration valid: {alignmentValid}");
        }
        
        // Test component types
        [HotComponent]
        [SimdLayout(16)]
        public struct CustomHotComponent
        {
            public Vector3 Value;
        }
        
        [ColdComponent]
        public struct CustomColdComponent
        {
            public string Value;
        }
        
        [SimdLayout(16)]
        public struct CustomSimdComponent
        {
            public Vector3 Position;
            public Vector3 Velocity;
        }
        
        public struct CustomStandardComponent
        {
            public float Value;
        }
    }
} 