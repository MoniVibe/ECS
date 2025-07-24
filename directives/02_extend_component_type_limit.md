# Extend ComponentType Limit Beyond 64

## Objective
Enable support for more than 64 component types using dynamic bit sets.

## Steps

1. **Replace `ulong` masks with `BitSet`**
   - Use a dynamic `BitSet` or custom span-backed type.
   - Replace all `ulong` bitmask uses (e.g., in queries, archetypes).

2. **Update Archetype matching logic**
   - Refactor `HasAllComponents(BitSet)` and `HasAnyComponent(BitSet)`.

3. **Adjust query cache key**
   - Use a hash of the `BitSet` instead of `ulong`.

4. **Update Query APIs**
   - Ensure fluent APIs work with updated mask logic.

5. **Test**
   - Create >64 component types and verify correctness.