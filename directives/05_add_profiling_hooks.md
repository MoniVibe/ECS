# Add Profiling Hooks for ECS Performance

## Objective
Track system, archetype, and component performance for optimization.

## Steps

1. **Create `Profiler.cs`**
   - Define `StartTimer(string label)`, `StopTimer(string label)`

2. **Wrap system execution**
   - Call profiler at start/end of each system or batch.

3. **Track per-component metrics**
   - Count entities, time per chunk update.

4. **Expose results**
   - Dump to console, file, or debug overlay.

5. **Validate**
   - Simulate high load and analyze timings.