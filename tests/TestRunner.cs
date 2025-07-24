using System;

namespace ECS.Tests
{
    public static class TestRunner
    {
        public static void RunAllTests()
        {
            Console.WriteLine("ECS Test Runner");
            Console.WriteLine("===============");
            
            // Run comprehensive ECS test
            Console.WriteLine("\n=== Running Comprehensive ECS Test ===");
            ComprehensiveECSTest.RunTest();
            
            // Run scene test
            Console.WriteLine("\n=== Running Scene Test ===");
            var scene = new TestScene();
            scene.RunSceneTest();
            
            // Run 3D visual demo
            Console.WriteLine("\n=== Running 3D Visual Demo ===");
            var visualScene = new VisualScene();
            visualScene.RunVisualDemo();
            
            // Run real 3D demo
            Console.WriteLine("\n=== Running Real 3D Demo ===");
            var real3DDemo = new Real3DDemo();
            real3DDemo.RunReal3DDemo();
            
            // Run console 3D demo
            Console.WriteLine("\n=== Running Console 3D Demo ===");
            var console3DDemo = new Console3DDemo();
            console3DDemo.RunConsole3DDemo();
            
            // Run real 3D visual demo
            Console.WriteLine("\n=== Running Real 3D Visual Demo ===");
            var visual3DDemo = new Visual3DDemo();
            visual3DDemo.RunVisual3DDemo();
            
            // Run component type limit test (temporarily disabled)
            // Console.WriteLine("\n=== Running Component Type Limit Test ===");
            // var componentTypeLimitTest = new ComponentTypeLimitTest();
            // componentTypeLimitTest.RunTest();
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        
        public static void RunComprehensiveECSTest()
        {
            Console.WriteLine("ECS Comprehensive Test Runner");
            Console.WriteLine("=============================");
            
            ComprehensiveECSTest.RunTest();
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
} 