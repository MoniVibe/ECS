using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.IO;

namespace ECS
{
    /// <summary>
    /// Utility for detecting and managing SIMD alignment requirements for components
    /// </summary>
    public static class SimdAlignmentUtility
    {
        // Precomputed alignment table for O(1) lookup
        private static readonly Dictionary<Type, int> _alignmentTable = new();
        
        // JSON configuration for alignment overrides
        private static Dictionary<string, int> _jsonAlignmentOverrides = new();
        private static string _configPath = "simd_alignment_config.json";
        private static bool _configLoaded = false;
        
        // Common SIMD-optimized types
        private static readonly HashSet<Type> _simdOptimizedTypes = new();
        
        static SimdAlignmentUtility()
        {
            // Pre-register common types
            PreRegisterCommonTypes();
        }
        
        /// <summary>
        /// Get the alignment requirement for a component type
        /// </summary>
        public static int GetComponentAlignment(Type type)
        {
            // Load config if not already loaded
            if (!_configLoaded)
            {
                try
                {
                    LoadAlignmentConfiguration("simd_alignment_config.json");
                }
                catch
                {
                    // Ignore if file doesn't exist
                }
                _configLoaded = true;
            }
            
            // Check precomputed alignment table first (O(1) lookup)
            if (_alignmentTable.TryGetValue(type, out var alignment))
                return alignment;
            
            // Check for explicit SIMD layout attribute (fallback)
            var simdAttr = type.GetCustomAttribute<SimdLayoutAttribute>();
            if (simdAttr != null)
                return simdAttr.Alignment;
            
            // Check JSON overrides
            var typeName = type.Name;
            if (_jsonAlignmentOverrides.TryGetValue(typeName, out var jsonAlignment))
                return jsonAlignment;
            
            // Fallback to type-based detection
            return DetectAlignmentByType(type);
        }
        
        /// <summary>
        /// Detect alignment requirement based on type characteristics
        /// </summary>
        private static int DetectAlignmentByType(Type type)
        {
            // Check if type contains SIMD-optimized fields
            if (IsSimdOptimizedType(type))
                return 16; // 16-byte alignment for SIMD types
            
            // Check field types for alignment requirements
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            int maxAlignment = 1;
            
            foreach (var field in fields)
            {
                var fieldAlignment = GetFieldAlignment(field.FieldType);
                maxAlignment = Math.Max(maxAlignment, fieldAlignment);
            }
            
            return maxAlignment;
        }
        
        /// <summary>
        /// Get alignment requirement for a specific field type
        /// </summary>
        private static int GetFieldAlignment(Type fieldType)
        {
            // Handle primitive types
            if (fieldType == typeof(float) || fieldType == typeof(int))
                return 4;
            if (fieldType == typeof(double) || fieldType == typeof(long))
                return 8;
            
            // Handle SIMD types
            if (IsSimdOptimizedType(fieldType))
                return 16;
            
            // Handle arrays
            if (fieldType.IsArray)
            {
                var elementType = fieldType.GetElementType();
                return GetFieldAlignment(elementType);
            }
            
            // Handle structs
            if (fieldType.IsValueType && !fieldType.IsPrimitive)
            {
                var fields = fieldType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                int maxAlignment = 1;
                
                foreach (var field in fields)
                {
                    var fieldAlignment = GetFieldAlignment(field.FieldType);
                    maxAlignment = Math.Max(maxAlignment, fieldAlignment);
                }
                
                return maxAlignment;
            }
            
            return 1; // Default alignment
        }
        
        /// <summary>
        /// Check if a type is SIMD-optimized
        /// </summary>
        public static bool IsComponentSimdOptimized(Type type)
        {
            // Check precomputed SIMD table first (O(1) lookup)
            if (_simdOptimizedTypes.Contains(type))
                return true;
            
            // Check for explicit SIMD layout attribute (fallback)
            if (type.GetCustomAttribute<SimdLayoutAttribute>() != null)
                return true;
            
            // Check if type contains SIMD-optimized fields
            return IsSimdOptimizedType(type);
        }
        
        /// <summary>
        /// Check if a type is inherently SIMD-optimized
        /// </summary>
        private static bool IsSimdOptimizedType(Type type)
        {
            return IsSimdOptimizedType(type, new HashSet<Type>());
        }
        
        /// <summary>
        /// Check if a type is inherently SIMD-optimized (with visited set to prevent recursion)
        /// </summary>
        private static bool IsSimdOptimizedType(Type type, HashSet<Type> visited)
        {
            // Prevent infinite recursion
            if (visited.Contains(type))
                return false;
            
            visited.Add(type);
            
            // Check for common SIMD types
            if (type == typeof(Vector3) || type == typeof(Vector4) || 
                type == typeof(Quaternion))
                return true;
            
            // Check if type contains SIMD-optimized fields
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (IsSimdOptimizedType(field.FieldType, visited))
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Register a component type with explicit alignment
        /// </summary>
        public static void RegisterComponentType<T>(int alignment = 4, bool isSimdOptimized = false)
        {
            var type = typeof(T);
            _alignmentTable[type] = alignment;
            
            if (isSimdOptimized)
                _simdOptimizedTypes.Add(type);
        }
        
        /// <summary>
        /// Pre-register common component types for optimal performance
        /// </summary>
        public static void PreRegisterCommonTypes()
        {
            // Register common SIMD-optimized types
            RegisterComponentType<Vector3>(16, true);
            RegisterComponentType<Vector4>(16, true);
            RegisterComponentType<Quaternion>(16, true);
            RegisterComponentType<Transform>(16, true);
            RegisterComponentType<Physics>(16, true);
            
            // Register common non-SIMD types
            RegisterComponentType<Position>(16, true);
            RegisterComponentType<Velocity>(16, true);
            RegisterComponentType<Name>(4, false);
            RegisterComponentType<Description>(4, false);
            RegisterComponentType<Health>(4, false);
            RegisterComponentType<Tag>(4, false);
            RegisterComponentType<UI>(4, false);
            RegisterComponentType<Audio>(4, false);
            RegisterComponentType<Metadata>(4, false);
            
            // Load configuration if available
            try
            {
                LoadAlignmentConfiguration("simd_alignment_config.json");
            }
            catch
            {
                // Ignore if file doesn't exist
            }
        }
        
        /// <summary>
        /// Load alignment configuration from JSON file
        /// </summary>
        public static void LoadAlignmentConfiguration(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    
                    if (config != null)
                    {
                        foreach (var kvp in config)
                        {
                            var typeName = kvp.Key;
                            var configValue = kvp.Value;
                            
                            if (configValue is System.Text.Json.JsonElement element)
                            {
                                if (element.TryGetProperty("alignment", out var alignmentElement) && 
                                    alignmentElement.TryGetInt32(out var alignment))
                                {
                                    _alignmentTable[Type.GetType(typeName)] = alignment;
                                }
                                
                                if (element.TryGetProperty("simdOptimized", out var simdElement))
                                {
                                    if (simdElement.ValueKind == System.Text.Json.JsonValueKind.True)
                                    {
                                        _simdOptimizedTypes.Add(Type.GetType(typeName));
                                    }
                                    else if (simdElement.ValueKind == System.Text.Json.JsonValueKind.False)
                                    {
                                        // Explicitly not SIMD optimized
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not load alignment configuration from {filePath}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Save current alignment configuration to JSON file
        /// </summary>
        public static void SaveAlignmentConfiguration(string? configPath = null)
        {
            if (configPath != null)
                _configPath = configPath;
            
            try
            {
                var config = new Dictionary<string, int>();
                
                // Add registered types
                foreach (var kvp in _alignmentTable)
                {
                    config[kvp.Key.Name] = kvp.Value;
                }
                
                // Add JSON overrides
                foreach (var kvp in _jsonAlignmentOverrides)
                {
                    config[kvp.Key] = kvp.Value;
                }
                
                var jsonContent = JsonSerializer.Serialize(config, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(_configPath, jsonContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not save alignment configuration to {_configPath}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Add an alignment override via JSON configuration
        /// </summary>
        public static void AddAlignmentOverride(string typeName, int alignment)
        {
            _jsonAlignmentOverrides[typeName] = alignment;
        }
        
        /// <summary>
        /// Remove an alignment override
        /// </summary>
        public static void RemoveAlignmentOverride(string typeName)
        {
            _jsonAlignmentOverrides.Remove(typeName);
        }
        
        /// <summary>
        /// Calculate aligned size for a given size and alignment
        /// </summary>
        public static int AlignSize(int size, int alignment)
        {
            return (size + alignment - 1) & ~(alignment - 1);
        }
        
        /// <summary>
        /// Get all registered alignments
        /// </summary>
        public static Dictionary<Type, int> GetRegisteredAlignments()
        {
            return new Dictionary<Type, int>(_alignmentTable);
        }
        
        /// <summary>
        /// Get all JSON alignment overrides
        /// </summary>
        public static Dictionary<string, int> GetAlignmentOverrides()
        {
            return new Dictionary<string, int>(_jsonAlignmentOverrides);
        }
        
        /// <summary>
        /// Get all SIMD-optimized types
        /// </summary>
        public static HashSet<Type> GetSimdOptimizedTypes()
        {
            return new HashSet<Type>(_simdOptimizedTypes);
        }
        
        /// <summary>
        /// Clear all alignments (useful for testing)
        /// </summary>
        public static void ClearAllAlignments()
        {
            _alignmentTable.Clear();
            _simdOptimizedTypes.Clear();
            _jsonAlignmentOverrides.Clear();
            _configLoaded = false;
        }
        
        /// <summary>
        /// Get statistics about alignments
        /// </summary>
        public static (int totalRegistered, int simdOptimized, int jsonOverrides, int maxAlignment) GetStatistics()
        {
            var maxAlignment = _alignmentTable.Values.Count > 0 ? _alignmentTable.Values.Max() : 0;
            
            return (_alignmentTable.Count, _simdOptimizedTypes.Count, _jsonAlignmentOverrides.Count, maxAlignment);
        }
        
        /// <summary>
        /// Validate alignment configuration
        /// </summary>
        public static bool ValidateAlignmentConfiguration()
        {
            foreach (var kvp in _alignmentTable)
            {
                var type = kvp.Key;
                var alignment = kvp.Value;
                
                // Check if alignment is a power of 2
                if ((alignment & (alignment - 1)) != 0)
                {
                    Console.WriteLine($"Warning: Invalid alignment {alignment} for type {type.Name}");
                    return false;
                }
                
                // Check if alignment is reasonable (1-64 bytes)
                if (alignment < 1 || alignment > 64)
                {
                    Console.WriteLine($"Warning: Unreasonable alignment {alignment} for type {type.Name}");
                    return false;
                }
            }
            
            return true;
        }
    }
} 