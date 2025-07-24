# Position to Cell Mapping

## Rule
Divide world position by cell size, then floor to integer.

## Formula
```csharp
int3 CellFromPosition(Vector3 pos, float cellSize)
{
    return new int3(
        (int)MathF.Floor(pos.X / cellSize),
        (int)MathF.Floor(pos.Y / cellSize),
        (int)MathF.Floor(pos.Z / cellSize));
}
```

## Example
```csharp
Vector3 pos = new Vector3(12.5f, -4.3f, 7.9f);
var cell = CellFromPosition(pos, 4.0f); // => (3, -2, 1)
```