using System.Numerics;

namespace ECS
{
    // Basic component types for the main ECS system
    public struct Position
    {
        public Vector3 Value;
    }

    public struct Velocity
    {
        public Vector3 Value;
    }

    public struct Name
    {
        public string Value;
    }

    public struct Health
    {
        public float Value;
    }

    public struct Transform
    {
        public Vector3 Position;
        public Vector3 Scale;
        public Quaternion Rotation;
    }
} 