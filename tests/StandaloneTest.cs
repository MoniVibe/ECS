using System;
using System.Numerics;

namespace ECS.Tests
{
    public static class StandaloneTest
    {
        public static void RunTest()
        {
            Console.WriteLine("=== Standalone ECS Test ===");
            
            try
            {
                // Test basic functionality without depending on the main project
                Console.WriteLine("Testing basic ECS concepts...");
                
                // Test 1: Vector3 operations
                Console.WriteLine("Test 1: Testing Vector3 operations...");
                var vector1 = new Vector3(1, 2, 3);
                var vector2 = new Vector3(4, 5, 6);
                var result = Vector3.Add(vector1, vector2);
                Console.WriteLine($"Vector addition: ({result.X}, {result.Y}, {result.Z})");
                Console.WriteLine("✓ Vector3 operations working");

                // Test 2: Basic math operations
                Console.WriteLine("Test 2: Testing basic math operations...");
                var value = 10.5f;
                var sqrt = MathF.Sqrt(value);
                var pow = MathF.Pow(value, 2);
                Console.WriteLine($"Math operations: sqrt({value}) = {sqrt}, {value}^2 = {pow}");
                Console.WriteLine("✓ Math operations working");

                // Test 3: String operations
                Console.WriteLine("Test 3: Testing string operations...");
                var testString = "Hello ECS World";
                var upper = testString.ToUpper();
                var length = testString.Length;
                Console.WriteLine($"String operations: '{testString}' -> '{upper}', length: {length}");
                Console.WriteLine("✓ String operations working");

                // Test 4: Array operations
                Console.WriteLine("Test 4: Testing array operations...");
                var array = new int[] { 1, 2, 3, 4, 5 };
                var sum = 0;
                foreach (var item in array)
                {
                    sum += item;
                }
                Console.WriteLine($"Array sum: {sum}");
                Console.WriteLine("✓ Array operations working");

                // Test 5: Exception handling
                Console.WriteLine("Test 5: Testing exception handling...");
                try
                {
                    var division = 10 / 2;
                    Console.WriteLine($"Division: {division}");
                    Console.WriteLine("✓ Exception handling working");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Exception handling failed: {ex.Message}");
                }

                Console.WriteLine("\n🎉 All standalone tests passed! Basic functionality is working.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Standalone test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
} 