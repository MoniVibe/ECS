using System;
using System.Collections.Generic;
using System.Linq;

namespace ECS
{
    /// <summary>
    /// Advanced memory pooling coordinator with separate generic and ECS-specific pools
    /// </summary>
    public static class MemoryPoolOptimizer
    {
        /// <summary>
        /// Get memory statistics for all pools
        /// </summary>
        public static PoolStatistics GetComprehensivePoolStatistics()
        {
            var (typeSpecificPools, sizeSpecificPools, totalPooledArrays) = GenericArrayPool.GetPoolStatistics();
            var (archetypePools, totalPooledChunks, chunkMemorySaved) = ChunkPooling.GetChunkPoolStatistics();
            
            var totalMemorySaved = chunkMemorySaved + (totalPooledArrays * 64); // Estimate 64 bytes per array
            
            return new PoolStatistics(
                typeSpecificPools,
                sizeSpecificPools + archetypePools,
                totalPooledArrays + totalPooledChunks,
                totalMemorySaved
            );
        }
        
        /// <summary>
        /// Get detailed statistics for each pool type
        /// </summary>
        public static Dictionary<string, object> GetDetailedPoolStatistics()
        {
            var stats = new Dictionary<string, object>();
            
            // Generic array pool stats
            var (typeSpecific, sizeSpecific, totalArrays) = GenericArrayPool.GetPoolStatistics();
            stats["GenericArrayPool"] = new
            {
                TypeSpecificPools = typeSpecific,
                SizeSpecificPools = sizeSpecific,
                TotalPooledArrays = totalArrays
            };
            
            // Chunk pool stats
            var (archetypePools, totalChunks, memorySaved) = ChunkPooling.GetChunkPoolStatistics();
            stats["ChunkPool"] = new
            {
                ArchetypePools = archetypePools,
                TotalPooledChunks = totalChunks,
                EstimatedMemorySaved = memorySaved
            };
            
            return stats;
        }
        
        /// <summary>
        /// Clear all pools (useful for testing or memory management)
        /// </summary>
        public static void ClearAllPools()
        {
            GenericArrayPool.ClearAllPools();
            ChunkPooling.ClearAllChunkPools();
        }
        
        /// <summary>
        /// Get memory usage statistics per pool type
        /// </summary>
        public static Dictionary<string, long> GetMemoryUsagePerPool()
        {
            var memoryUsage = new Dictionary<string, long>();
            
            // Calculate memory usage for generic array pool
            var (_, sizeSpecific, totalArrays) = GenericArrayPool.GetPoolStatistics();
            var genericArrayMemory = totalArrays * 64; // Rough estimate
            memoryUsage["GenericArrayPool"] = genericArrayMemory;
            
            // Calculate memory usage for chunk pool
            var (_, totalChunks, chunkMemory) = ChunkPooling.GetChunkPoolStatistics();
            memoryUsage["ChunkPool"] = chunkMemory;
            
            return memoryUsage;
        }
    }
    
    /// <summary>
    /// Memory pool statistics for monitoring
    /// </summary>
    public readonly struct PoolStatistics
    {
        public readonly int TypeSpecificPools;
        public readonly int SizeSpecificPools;
        public readonly int TotalPooledArrays;
        public readonly long EstimatedMemorySaved;
        
        public PoolStatistics(int typeSpecificPools, int sizeSpecificPools, int totalPooledArrays, long estimatedMemorySaved)
        {
            TypeSpecificPools = typeSpecificPools;
            SizeSpecificPools = sizeSpecificPools;
            TotalPooledArrays = totalPooledArrays;
            EstimatedMemorySaved = estimatedMemorySaved;
        }
        
        public override string ToString()
        {
            return $"Pools: {TypeSpecificPools} type-specific, {SizeSpecificPools} size-specific, {TotalPooledArrays} total arrays, ~{EstimatedMemorySaved} bytes saved";
        }
    }
} 