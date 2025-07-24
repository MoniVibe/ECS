using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.IO;

namespace ECS
{
    /// <summary>
    /// Classifies components based on their heat characteristics (hot/cold) for optimization
    /// </summary>
    public static class ComponentHeatClassifier
    {
        // Precomputed heat classification table for O(1) lookup
        private static readonly Dictionary<Type, ComponentHeat> _heatTable = new();
        
        // JSON configuration for heat classification overrides
        private static Dictionary<string, ComponentHeat> _jsonHeatOverrides = new();
        private static string _configPath = "component_heat_config.json";
        private static bool _configLoaded = false;
        
        static ComponentHeatClassifier()
        {
            // Pre-register common component types
            PreRegisterCommonTypes();
        }
        
        /// <summary>
        /// Get the heat classification for a component type
        /// </summary>
        public static ComponentHeat GetComponentHeat(Type type)
        {
            // Load config if not already loaded
            if (!_configLoaded)
            {
                try
                {
                    LoadHeatConfiguration("component_heat_config.json");
                }
                catch
                {
                    // Ignore if file doesn't exist
                }
                _configLoaded = true;
            }
            
            // Check precomputed table first (O(1) lookup)
            if (_heatTable.TryGetValue(type, out var heat))
                return heat;
            
            // Check for explicit attributes (fallback)
            if (type.GetCustomAttribute<HotComponentAttribute>() != null)
                return ComponentHeat.Hot;
            if (type.GetCustomAttribute<ColdComponentAttribute>() != null)
                return ComponentHeat.Cold;
            
            // Check JSON overrides
            var typeName = type.Name;
            if (_jsonHeatOverrides.TryGetValue(typeName, out var jsonHeat))
                return jsonHeat;
            
            // Fallback to name-based classification
            return ClassifyByTypeName(type);
        }
        
        /// <summary>
        /// Classify component heat based on type name patterns
        /// </summary>
        private static ComponentHeat ClassifyByTypeName(Type type)
        {
            var typeName = type.Name.ToLower();
            
            // Hot components (frequently accessed)
            if (typeName.Contains("position") || typeName.Contains("velocity") || 
                typeName.Contains("transform") || typeName.Contains("physics") ||
                typeName.Contains("rotation") || typeName.Contains("scale") ||
                typeName.Contains("force") || typeName.Contains("acceleration") ||
                typeName.Contains("momentum") || typeName.Contains("energy"))
                return ComponentHeat.Hot;
            
            // Cold components (rarely accessed)
            if (typeName.Contains("name") || typeName.Contains("description") || 
                typeName.Contains("metadata") || typeName.Contains("tag") ||
                typeName.Contains("ui") || typeName.Contains("audio") ||
                typeName.Contains("texture") || typeName.Contains("material") ||
                typeName.Contains("script") || typeName.Contains("config"))
                return ComponentHeat.Cold;
            
            return ComponentHeat.Hot; // Default to hot for performance
        }
        
        /// <summary>
        /// Register a component type with explicit heat classification
        /// </summary>
        public static void RegisterComponentType<T>(ComponentHeat heat = ComponentHeat.Hot)
        {
            var type = typeof(T);
            _heatTable[type] = heat;
        }
        
        /// <summary>
        /// Pre-register common component types for optimal performance
        /// </summary>
        public static void PreRegisterCommonTypes()
        {
            // Register common hot components
            RegisterComponentType<Position>(ComponentHeat.Hot);
            RegisterComponentType<Velocity>(ComponentHeat.Hot);
            RegisterComponentType<Transform>(ComponentHeat.Hot);
            RegisterComponentType<Physics>(ComponentHeat.Hot);
            
            // Register common cold components
            RegisterComponentType<Name>(ComponentHeat.Cold);
            RegisterComponentType<Description>(ComponentHeat.Cold);
            RegisterComponentType<Health>(ComponentHeat.Cold);
            RegisterComponentType<Tag>(ComponentHeat.Cold);
            RegisterComponentType<UI>(ComponentHeat.Cold);
            RegisterComponentType<Audio>(ComponentHeat.Cold);
            RegisterComponentType<Metadata>(ComponentHeat.Cold);
            
            // Load configuration if available
            try
            {
                LoadHeatConfiguration("component_heat_config.json");
            }
            catch
            {
                // Ignore if file doesn't exist
            }
        }
        
        /// <summary>
        /// Load heat classification configuration from JSON file
        /// </summary>
        public static void LoadHeatConfiguration(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    
                    if (config != null)
                    {
                        foreach (var kvp in config)
                        {
                            var typeName = kvp.Key;
                            var heatStr = kvp.Value;
                            
                            if (Enum.TryParse<ComponentHeat>(heatStr, true, out var heat))
                            {
                                _jsonHeatOverrides[typeName] = heat;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not load heat configuration from {filePath}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Save current heat classification configuration to JSON file
        /// </summary>
        public static void SaveHeatConfiguration(string? configPath = null)
        {
            if (configPath != null)
                _configPath = configPath;
            
            try
            {
                var config = new Dictionary<string, string>();
                
                // Add registered types
                foreach (var kvp in _heatTable)
                {
                    config[kvp.Key.Name] = kvp.Value.ToString();
                }
                
                // Add JSON overrides
                foreach (var kvp in _jsonHeatOverrides)
                {
                    config[kvp.Key] = kvp.Value.ToString();
                }
                
                var jsonContent = JsonSerializer.Serialize(config, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(_configPath, jsonContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not save heat configuration to {_configPath}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Add a heat classification override via JSON configuration
        /// </summary>
        public static void AddHeatOverride(string typeName, ComponentHeat heat)
        {
            _jsonHeatOverrides[typeName] = heat;
        }
        
        /// <summary>
        /// Remove a heat classification override
        /// </summary>
        public static void RemoveHeatOverride(string typeName)
        {
            _jsonHeatOverrides.Remove(typeName);
        }
        
        /// <summary>
        /// Get all registered heat classifications
        /// </summary>
        public static Dictionary<Type, ComponentHeat> GetRegisteredHeatClassifications()
        {
            return new Dictionary<Type, ComponentHeat>(_heatTable);
        }
        
        /// <summary>
        /// Get all JSON heat overrides
        /// </summary>
        public static Dictionary<string, ComponentHeat> GetHeatOverrides()
        {
            return new Dictionary<string, ComponentHeat>(_jsonHeatOverrides);
        }
        
        /// <summary>
        /// Clear all heat classifications (useful for testing)
        /// </summary>
        public static void ClearAllClassifications()
        {
            _heatTable.Clear();
            _jsonHeatOverrides.Clear();
            _configLoaded = false;
        }
        
        /// <summary>
        /// Get statistics about heat classifications
        /// </summary>
        public static (int totalRegistered, int hotComponents, int coldComponents, int jsonOverrides) GetStatistics()
        {
            var hotCount = _heatTable.Values.Count(h => h == ComponentHeat.Hot);
            var coldCount = _heatTable.Values.Count(h => h == ComponentHeat.Cold);
            
            return (_heatTable.Count, hotCount, coldCount, _jsonHeatOverrides.Count);
        }
    }
    
    /// <summary>
    /// Heat classification for components
    /// </summary>
    public enum ComponentHeat
    {
        Hot,    // Frequently accessed, optimize for speed
        Cold     // Rarely accessed, optimize for memory
    }
} 