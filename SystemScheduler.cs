using System;
using System.Collections.Generic;

namespace ECS
{
    /// <summary>
    /// Manages structural change queue and batch processing
    /// Single responsibility: Structural change scheduling and processing
    /// </summary>
    public class StructuralChangeScheduler
    {
        private readonly Queue<StructuralChange> _structuralChanges;
        private readonly EntityManager _entityManager;

        public StructuralChangeScheduler(EntityManager entityManager)
        {
            _structuralChanges = new Queue<StructuralChange>();
            _entityManager = entityManager;
        }

        /// <summary>
        /// Queues an add component operation
        /// </summary>
        public void QueueAddComponent<T>(EntityId entityId, T component)
        {
            var componentType = ComponentTypeRegistry.Get<T>();
            _structuralChanges.Enqueue(new StructuralChange(entityId, ActionType.AddComponent, componentType, component));
        }

        /// <summary>
        /// Queues a remove component operation
        /// </summary>
        public void QueueRemoveComponent<T>(EntityId entityId)
        {
            var componentType = ComponentTypeRegistry.Get<T>();
            _structuralChanges.Enqueue(new StructuralChange(entityId, ActionType.RemoveComponent, componentType));
        }

        /// <summary>
        /// Queues a destroy entity operation
        /// </summary>
        public void QueueDestroyEntity(EntityId entityId)
        {
            _structuralChanges.Enqueue(new StructuralChange(entityId, ActionType.DestroyEntity));
        }

        /// <summary>
        /// Processes all queued structural changes
        /// </summary>
        public void ProcessStructuralChanges()
        {
            while (_structuralChanges.Count > 0)
            {
                var change = _structuralChanges.Dequeue();
                
                switch (change.Type)
                {
                    case ActionType.AddComponent:
                        _entityManager.AddComponent(change.Entity, change.Component, change.Data);
                        break;
                    case ActionType.RemoveComponent:
                        _entityManager.RemoveComponent(change.Entity, change.Component);
                        break;
                    case ActionType.DestroyEntity:
                        _entityManager.DestroyEntity(change.Entity);
                        break;
                }
            }
        }

        /// <summary>
        /// Clears all queued structural changes
        /// </summary>
        public void ClearStructuralChanges()
        {
            _structuralChanges.Clear();
        }

        /// <summary>
        /// Gets the number of pending structural changes
        /// </summary>
        public int GetPendingChangeCount()
        {
            return _structuralChanges.Count;
        }

        /// <summary>
        /// Gets all pending structural changes (for debugging/monitoring)
        /// </summary>
        public IEnumerable<StructuralChange> GetPendingChanges()
        {
            return _structuralChanges.ToArray();
        }

        /// <summary>
        /// Processes structural changes in batches
        /// </summary>
        public void ProcessStructuralChangesBatch(int batchSize = 100)
        {
            var processed = 0;
            while (_structuralChanges.Count > 0 && processed < batchSize)
            {
                var change = _structuralChanges.Dequeue();
                
                switch (change.Type)
                {
                    case ActionType.AddComponent:
                        _entityManager.AddComponent(change.Entity, change.Component, change.Data);
                        break;
                    case ActionType.RemoveComponent:
                        _entityManager.RemoveComponent(change.Entity, change.Component);
                        break;
                    case ActionType.DestroyEntity:
                        _entityManager.DestroyEntity(change.Entity);
                        break;
                }
                
                processed++;
            }
        }

        /// <summary>
        /// Gets statistics about the scheduler
        /// </summary>
        public (int pendingChanges, int totalProcessed) GetStatistics()
        {
            return (_structuralChanges.Count, 0); // totalProcessed would need to be tracked separately
        }
    }
} 