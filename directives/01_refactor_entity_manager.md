# Refactor EntityManager into Subsystems

## Objective
Modularize the `EntityManager` into smaller, single-responsibility systems to improve maintainability and clarity.

## Steps

1. **Create `EntityAllocator.cs`**
   - Move entity ID management (ID reuse, generation).
   - Functions: `CreateEntityId()`, `ReleaseEntityId()`, etc.

2. **Create `ArchetypeStore.cs`**
   - Handle archetype lookup, creation, and chunk associations.
   - Migrate chunk pools and component layouts here.

3. **Create `SystemScheduler.cs`**
   - Move structural change queue and batch processing from `EntityManager`.

4. **Update `EntityManager.cs`**
   - Keep it as a facade, calling into the above subsystems.
   - Ensure tests/examples still work.

5. **Test**
   - Run unit tests and verify simulation output is unchanged.