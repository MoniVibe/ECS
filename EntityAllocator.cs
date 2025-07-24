using System;
using System.Collections.Generic;

namespace ECS
{
    /// <summary>
    /// Manages entity ID allocation, reuse, and generation tracking
    /// Single responsibility: Entity ID lifecycle management
    /// </summary>
    public class EntityAllocator
    {
        private readonly Queue<int> _reusableIds;
        private readonly Dictionary<int, int> _idGenerations;
        private int _nextEntityId;

        public EntityAllocator()
        {
            _reusableIds = new Queue<int>();
            _idGenerations = new Dictionary<int, int>();
            _nextEntityId = 0;
        }

        /// <summary>
        /// Creates a new entity ID with generation tracking
        /// </summary>
        public EntityId CreateEntityId()
        {
            int id;
            if (_reusableIds.Count > 0)
            {
                id = _reusableIds.Dequeue();
            }
            else
            {
                id = _nextEntityId++;
            }

            if (!_idGenerations.TryGetValue(id, out int generation))
            {
                generation = 0;
            }
            else
            {
                generation++;
            }

            _idGenerations[id] = generation;
            return new EntityId(id, generation);
        }

        /// <summary>
        /// Releases an entity ID for reuse
        /// </summary>
        public void ReleaseEntityId(int id)
        {
            _reusableIds.Enqueue(id);
        }

        /// <summary>
        /// Gets the current generation for an entity ID
        /// </summary>
        public int GetEntityGeneration(int id)
        {
            return _idGenerations.TryGetValue(id, out int generation) ? generation : 0;
        }

        /// <summary>
        /// Gets statistics about entity allocation
        /// </summary>
        public (int totalAllocated, int reusableCount, int nextId) GetStatistics()
        {
            return (_nextEntityId, _reusableIds.Count, _nextEntityId);
        }

        /// <summary>
        /// Clears all reusable IDs (for testing/reset purposes)
        /// </summary>
        public void ClearReusableIds()
        {
            _reusableIds.Clear();
        }

        /// <summary>
        /// Resets the allocator to initial state
        /// </summary>
        public void Reset()
        {
            _reusableIds.Clear();
            _idGenerations.Clear();
            _nextEntityId = 0;
        }
    }
} 