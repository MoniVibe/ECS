# Spatial Grid System Purpose

## Goal
Use a spatial grid (hash grid) to:
- Broadly detect which entities might interact
- Optimize collision checks, queries, and area effects

## Why
Avoid O(n^2) comparisons by grouping entities by space.

## Example Use Case
- Player moves into an area
- Query all entities in nearby cells
- Only test those for collision or logic

## Characteristics
- Works on `Position` component
- Fixed-size cubic or square cells
- Fast: Hash-based lookups
- ECS-friendly, memory-efficient