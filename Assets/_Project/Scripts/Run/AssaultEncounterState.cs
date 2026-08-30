using System.Collections.Generic;
using Titanhold.Combat;

namespace Titanhold.Run
{
    public sealed class AssaultEncounterState
    {
        private readonly HashSet<CombatActorReference> spawnedEnemies =
            new HashSet<CombatActorReference>();
        private readonly HashSet<CombatActorReference> aliveEnemies =
            new HashSet<CombatActorReference>();

        public AssaultEncounterId EncounterId { get; private set; }
        public int RoundNumber { get; private set; }
        public int PlannedEnemyCount { get; private set; }
        public int SpawnedEnemyCount { get; private set; }
        public int DefeatedEnemyCount { get; private set; }
        public int AliveEnemyCount => aliveEnemies.Count;
        public bool IsStarted { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool IsActive => IsStarted && !IsCompleted;
        public bool IsSpawnSequenceCompleted =>
            IsStarted && SpawnedEnemyCount == PlannedEnemyCount;

        internal void Begin(
            AssaultEncounterId encounterId,
            int roundNumber,
            int plannedEnemyCount)
        {
            spawnedEnemies.Clear();
            aliveEnemies.Clear();
            EncounterId = encounterId;
            RoundNumber = roundNumber;
            PlannedEnemyCount = plannedEnemyCount;
            SpawnedEnemyCount = 0;
            DefeatedEnemyCount = 0;
            IsStarted = true;
            IsCompleted = false;
        }

        internal void Reset()
        {
            spawnedEnemies.Clear();
            aliveEnemies.Clear();
            EncounterId = default;
            RoundNumber = 0;
            PlannedEnemyCount = 0;
            SpawnedEnemyCount = 0;
            DefeatedEnemyCount = 0;
            IsStarted = false;
            IsCompleted = false;
        }

        internal bool ContainsSpawnedEnemy(CombatActorReference enemy)
        {
            return spawnedEnemies.Contains(enemy);
        }

        internal bool ContainsAliveEnemy(CombatActorReference enemy)
        {
            return aliveEnemies.Contains(enemy);
        }

        internal void RegisterSpawn(CombatActorReference enemy)
        {
            spawnedEnemies.Add(enemy);
            aliveEnemies.Add(enemy);
            SpawnedEnemyCount++;
        }

        internal void RegisterDefeat(CombatActorReference enemy)
        {
            aliveEnemies.Remove(enemy);
            DefeatedEnemyCount++;
        }

        internal void RollbackDefeat(CombatActorReference enemy)
        {
            aliveEnemies.Add(enemy);
            DefeatedEnemyCount--;
        }

        internal void MarkCompleted()
        {
            IsCompleted = true;
        }
    }
}
