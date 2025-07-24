using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace ECS
{
    /// <summary>
    /// Handles batch range scheduling for parallel processing operations
    /// </summary>
    public static class BatchRangeScheduler
    {
        private static readonly int MaxDegreeOfParallelism = Environment.ProcessorCount;
        
        /// <summary>
        /// Creates batch ranges for parallel processing
        /// </summary>
        /// <param name="totalCount">Total number of items to process</param>
        /// <param name="batchSize">Size of each batch</param>
        /// <returns>List of batch ranges</returns>
        public static List<(int start, int end)> CreateBatchRanges(int totalCount, int batchSize)
        {
            if (totalCount <= 0)
                throw new ArgumentException("Total count must be positive", nameof(totalCount));
            
            if (batchSize <= 0)
                throw new ArgumentException("Batch size must be positive", nameof(batchSize));
            
            var ranges = new List<(int start, int end)>();
            
            for (int i = 0; i < totalCount; i += batchSize)
            {
                ranges.Add((i, Math.Min(i + batchSize, totalCount)));
            }
            
            return ranges;
        }
        
        /// <summary>
        /// Processes batches in parallel with validation
        /// </summary>
        /// <param name="totalCount">Total number of items</param>
        /// <param name="batchSize">Size of each batch</param>
        /// <param name="processor">Action to process each batch range</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        public static void ProcessBatchesParallel(int totalCount, int batchSize, 
            Action<(int start, int end)> processor, CancellationToken cancellationToken = default)
        {
            ValidateInputs(totalCount, batchSize, processor);
            
            if (totalCount == 0) return;
            
            var ranges = CreateBatchRanges(totalCount, batchSize);
            
            Parallel.ForEach(ranges, new ParallelOptions 
            { 
                MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                CancellationToken = cancellationToken
            }, range =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                processor(range);
            });
        }
        
        /// <summary>
        /// Processes batches in parallel with async support
        /// </summary>
        /// <param name="totalCount">Total number of items</param>
        /// <param name="batchSize">Size of each batch</param>
        /// <param name="processor">Action to process each batch range</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task ProcessBatchesParallelAsync(int totalCount, int batchSize, 
            Action<(int start, int end)> processor, CancellationToken cancellationToken = default)
        {
            ValidateInputs(totalCount, batchSize, processor);
            
            if (totalCount == 0) return;
            
            var ranges = CreateBatchRanges(totalCount, batchSize);
            
            await Task.Run(() =>
            {
                Parallel.ForEach(ranges, new ParallelOptions 
                { 
                    MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                    CancellationToken = cancellationToken
                }, range =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    processor(range);
                });
            }, cancellationToken);
        }
        
        /// <summary>
        /// Processes batches with custom parallelism settings
        /// </summary>
        /// <param name="totalCount">Total number of items</param>
        /// <param name="batchSize">Size of each batch</param>
        /// <param name="processor">Action to process each batch range</param>
        /// <param name="maxDegreeOfParallelism">Maximum degree of parallelism</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        public static void ProcessBatchesParallel(int totalCount, int batchSize, 
            Action<(int start, int end)> processor, int maxDegreeOfParallelism, 
            CancellationToken cancellationToken = default)
        {
            ValidateInputs(totalCount, batchSize, processor);
            
            if (maxDegreeOfParallelism <= 0)
                throw new ArgumentException("Max degree of parallelism must be positive", nameof(maxDegreeOfParallelism));
            
            if (totalCount == 0) return;
            
            var ranges = CreateBatchRanges(totalCount, batchSize);
            
            Parallel.ForEach(ranges, new ParallelOptions 
            { 
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                CancellationToken = cancellationToken
            }, range =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                processor(range);
            });
        }
        
        /// <summary>
        /// Validates input parameters for batch processing
        /// </summary>
        private static void ValidateInputs(int totalCount, int batchSize, Action<(int start, int end)> processor)
        {
            if (totalCount < 0)
                throw new ArgumentException("Total count cannot be negative", nameof(totalCount));
            
            if (batchSize <= 0)
                throw new ArgumentException("Batch size must be positive", nameof(batchSize));
            
            if (processor == null)
                throw new ArgumentNullException(nameof(processor));
        }
        
        /// <summary>
        /// Gets the optimal batch size based on total count and processor count
        /// </summary>
        /// <param name="totalCount">Total number of items</param>
        /// <param name="targetBatchesPerThread">Target number of batches per thread</param>
        /// <returns>Optimal batch size</returns>
        public static int GetOptimalBatchSize(int totalCount, int targetBatchesPerThread = 4)
        {
            if (totalCount <= 0)
                throw new ArgumentException("Total count must be positive", nameof(totalCount));
            
            if (targetBatchesPerThread <= 0)
                throw new ArgumentException("Target batches per thread must be positive", nameof(targetBatchesPerThread));
            
            var totalBatches = MaxDegreeOfParallelism * targetBatchesPerThread;
            return Math.Max(1, totalCount / totalBatches);
        }
        
        /// <summary>
        /// Gets the current maximum degree of parallelism
        /// </summary>
        public static int GetMaxDegreeOfParallelism() => MaxDegreeOfParallelism;
    }
} 