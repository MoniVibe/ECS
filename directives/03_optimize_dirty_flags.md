# Optimize Dirty Flags for SIMD Access

## Objective
Improve memory locality and SIMD compatibility of dirty flags.

## Steps

1. **Replace BitArray[64]**
   - Use `ulong[]` or fixed-size `Span<byte>` per component.
   - Store dirty flags in contiguous memory.

2. **Implement helper methods**
   - `IsComponentDirty(int index)`
   - `MarkComponentDirty(int index)`

3. **Benchmark**
   - Compare branch performance and memory throughput.

4. **Update all dirty flag checks**
   - Replace calls using `BitArray.Get()` with new memory model.

5. **Test**
   - Validate correctness and memory layout with SIMD analyzer.