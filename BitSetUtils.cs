using System;
using System.Collections.Generic;
using System.Reflection;

namespace ECS
{
    /// <summary>
    /// Centralized utility for bit operations and component type management
    /// </summary>
    public static class BitUtils
    {
        private static readonly Dictionary<Type, System.Reflection.MethodInfo> _cachedMethods = new();

        /// <summary>
        /// Calculate bitmask from component types (legacy ulong version)
        /// </summary>
        public static ulong CalculateBitmask(ComponentType[] types)
        {
            ulong mask = 0;
            foreach (var type in types)
            {
                if (type.Id < 64) // Support up to 64 component types
                    mask |= 1UL << type.Id;
            }
            return mask;
        }

        /// <summary>
        /// Calculate BitSet from component types (scalable version)
        /// </summary>
        public static BitSet CalculateBitSet(ComponentType[] types)
        {
            var bitSet = new BitSet(256); // Support up to 256 component types
            foreach (var type in types)
            {
                if (type.Id < 256)
                    bitSet.Set(type.Id);
            }
            return bitSet;
        }

        /// <summary>
        /// Convert ulong bitmask to BitSet for backward compatibility
        /// </summary>
        public static BitSet UlongToBitSet(ulong mask)
        {
            var bitSet = new BitSet(64);
            for (int i = 0; i < 64; i++)
            {
                if ((mask & (1UL << i)) != 0)
                    bitSet.Set(i);
            }
            return bitSet;
        }

        /// <summary>
        /// Convert BitSet to ulong for backward compatibility
        /// </summary>
        public static ulong BitSetToUlong(BitSet bitSet)
        {
            ulong mask = 0;
            for (int i = 0; i < 64; i++)
            {
                if (bitSet.IsSet(i))
                    mask |= 1UL << i;
            }
            return mask;
        }

        /// <summary>
        /// Get cached MethodInfo for reflection operations
        /// </summary>
        public static System.Reflection.MethodInfo? GetCachedMethod(Type type, string methodName, System.Reflection.BindingFlags bindingFlags)
        {
            if (!_cachedMethods.TryGetValue(type, out var methodInfo))
            {
                methodInfo = type.GetMethod(methodName, bindingFlags);
                if (methodInfo != null)
                    _cachedMethods[type] = methodInfo;
            }
            return methodInfo;
        }

        /// <summary>
        /// Clear cached methods (useful for testing or memory management)
        /// </summary>
        public static void ClearMethodCache()
        {
            _cachedMethods.Clear();
        }
    }

    /// <summary>
    /// Flexible bit set supporting more than 64 component types
    /// </summary>
    public class BitSet
    {
        private readonly ulong[] _bits;
        private readonly int _maxBits;

        public BitSet(int maxBits = 256)
        {
            _maxBits = maxBits;
            _bits = new ulong[(maxBits + 63) / 64];
        }

        public void Set(int bitIndex)
        {
            if (bitIndex < 0 || bitIndex >= _maxBits)
                throw new ArgumentOutOfRangeException(nameof(bitIndex));

            var arrayIndex = bitIndex / 64;
            var bitOffset = bitIndex % 64;
            _bits[arrayIndex] |= 1UL << bitOffset;
        }

        public void Clear(int bitIndex)
        {
            if (bitIndex < 0 || bitIndex >= _maxBits)
                throw new ArgumentOutOfRangeException(nameof(bitIndex));

            var arrayIndex = bitIndex / 64;
            var bitOffset = bitIndex % 64;
            _bits[arrayIndex] &= ~(1UL << bitOffset);
        }

        public bool IsSet(int bitIndex)
        {
            if (bitIndex < 0 || bitIndex >= _maxBits)
                return false;

            var arrayIndex = bitIndex / 64;
            var bitOffset = bitIndex % 64;
            return (_bits[arrayIndex] & (1UL << bitOffset)) != 0;
        }

        public bool HasAll(BitSet other)
        {
            for (int i = 0; i < _bits.Length && i < other._bits.Length; i++)
            {
                if ((_bits[i] & other._bits[i]) != other._bits[i])
                    return false;
            }
            return true;
        }

        public bool HasAny(BitSet other)
        {
            for (int i = 0; i < _bits.Length && i < other._bits.Length; i++)
            {
                if ((_bits[i] & other._bits[i]) != 0)
                    return true;
            }
            return false;
        }

        public void ClearAll()
        {
            Array.Clear(_bits, 0, _bits.Length);
        }

        public BitSet Clone()
        {
            var clone = new BitSet(_maxBits);
            Array.Copy(_bits, clone._bits, _bits.Length);
            return clone;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not BitSet other)
                return false;

            if (_maxBits != other._maxBits)
                return false;

            for (int i = 0; i < _bits.Length; i++)
            {
                if (_bits[i] != other._bits[i])
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(_maxBits);
            for (int i = 0; i < _bits.Length; i++)
            {
                hash.Add(_bits[i]);
            }
            return hash.ToHashCode();
        }
    }
} 