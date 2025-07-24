# Cell Hash Function

## Rule
Map 3D integer coordinates to a single integer hash key.

## Formula
```csharp
int Hash(int x, int y, int z)
{
    return ((x * 73856093) ^ (y * 19349663) ^ (z * 83492791)) & mask;
}
```
- `mask = tableSize - 1` (table size is power of 2)

## Example
```csharp
var key = Hash(3, -2, 1); // e.g., 784232919
```