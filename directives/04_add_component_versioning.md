# Add Component Versioning for Serialization

## Objective
Support forward-compatible serialization of components.

## Steps

1. **Define version attribute**
   - Add `[ComponentVersion(1)]` attribute.

2. **Extend serializers**
   - Write version into binary/json.
   - On read, compare and adapt.

3. **Create patch system**
   - Add functions to migrate v1 → v2 component layout.

4. **Fallback to default**
   - If version not matched, fall back to default/init.

5. **Test**
   - Simulate loading old save and ensure it migrates correctly.