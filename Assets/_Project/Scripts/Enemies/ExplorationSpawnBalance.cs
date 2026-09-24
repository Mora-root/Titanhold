using System;
using System.Collections.Generic;

namespace Titanhold.Enemies
{
    public readonly struct ExplorationEnemyWeight
    {
        public ExplorationEnemyWeight(string enemyId, int weight)
        {
            EnemyId = enemyId;
            Weight = weight;
        }

        public string EnemyId { get; }
        public int Weight { get; }
    }

    public sealed class ExplorationSpawnStage
    {
        private readonly ExplorationEnemyWeight[] enemies;

        private ExplorationSpawnStage(
            int startRound,
            int maximumAlive,
            float respawnDelay,
            ExplorationEnemyWeight[] enemies,
            int totalWeight)
        {
            StartRound = startRound;
            MaximumAlive = maximumAlive;
            RespawnDelay = respawnDelay;
            this.enemies = enemies;
            TotalWeight = totalWeight;
        }

        public int StartRound { get; }
        public int MaximumAlive { get; }
        public float RespawnDelay { get; }
        public IReadOnlyList<ExplorationEnemyWeight> Enemies => enemies;
        public int TotalWeight { get; }

        public static bool TryCreate(
            int startRound,
            int maximumAlive,
            float respawnDelay,
            IReadOnlyList<ExplorationEnemyWeight> enemyWeights,
            out ExplorationSpawnStage stage,
            out string error)
        {
            stage = null;
            error = string.Empty;
            if (startRound <= 0)
            {
                error = "A spawn stage must start on a positive round.";
                return false;
            }

            if (maximumAlive <= 0)
            {
                error = "A spawn stage must allow at least one living enemy.";
                return false;
            }

            if (respawnDelay < 0f ||
                float.IsNaN(respawnDelay) ||
                float.IsInfinity(respawnDelay))
            {
                error = "A spawn stage has an invalid respawn delay.";
                return false;
            }

            if (enemyWeights == null || enemyWeights.Count == 0)
            {
                error = "A spawn stage requires at least one enemy entry.";
                return false;
            }

            ExplorationEnemyWeight[] copied =
                new ExplorationEnemyWeight[enemyWeights.Count];
            HashSet<string> uniqueIds = new(StringComparer.Ordinal);
            long totalWeight = 0;
            for (int i = 0; i < enemyWeights.Count; i++)
            {
                ExplorationEnemyWeight entry = enemyWeights[i];
                if (!HasStrictId(entry.EnemyId))
                {
                    error = $"Spawn enemy entry {i} has an invalid stable id.";
                    return false;
                }

                if (entry.Weight <= 0)
                {
                    error =
                        $"Spawn enemy '{entry.EnemyId}' must have a positive weight.";
                    return false;
                }

                if (!uniqueIds.Add(entry.EnemyId))
                {
                    error =
                        $"Spawn enemy '{entry.EnemyId}' occurs more than once in a stage.";
                    return false;
                }

                totalWeight += entry.Weight;
                if (totalWeight > int.MaxValue)
                {
                    error = "A spawn stage has too much combined weight.";
                    return false;
                }

                copied[i] = entry;
            }

            stage = new ExplorationSpawnStage(
                startRound,
                maximumAlive,
                respawnDelay,
                copied,
                (int)totalWeight);
            return true;
        }

        public bool TrySelectEnemy(int selectionValue, out string enemyId)
        {
            enemyId = string.Empty;
            if (selectionValue < 0 || TotalWeight <= 0)
                return false;

            int resolved = selectionValue % TotalWeight;
            for (int i = 0; i < enemies.Length; i++)
            {
                if (resolved < enemies[i].Weight)
                {
                    enemyId = enemies[i].EnemyId;
                    return true;
                }

                resolved -= enemies[i].Weight;
            }

            return false;
        }

        private static bool HasStrictId(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   string.Equals(value, value.Trim(), StringComparison.Ordinal);
        }
    }

    public sealed class ExplorationSpawnProfile
    {
        private readonly ExplorationSpawnStage[] stages;

        private ExplorationSpawnProfile(
            string profileId,
            ExplorationSpawnStage[] stages)
        {
            ProfileId = profileId;
            this.stages = stages;
        }

        public string ProfileId { get; }
        public IReadOnlyList<ExplorationSpawnStage> Stages => stages;

        public static bool TryCreate(
            string profileId,
            IReadOnlyList<ExplorationSpawnStage> stages,
            out ExplorationSpawnProfile profile,
            out string error)
        {
            profile = null;
            error = string.Empty;
            if (!HasStrictId(profileId))
            {
                error = "An exploration spawn profile has an invalid stable id.";
                return false;
            }

            if (stages == null || stages.Count == 0)
            {
                error = $"Spawn profile '{profileId}' requires at least one stage.";
                return false;
            }

            ExplorationSpawnStage[] copied =
                new ExplorationSpawnStage[stages.Count];
            int previousRound = 0;
            for (int i = 0; i < stages.Count; i++)
            {
                ExplorationSpawnStage stage = stages[i];
                if (stage == null)
                {
                    error = $"Spawn profile '{profileId}' has a null stage at index {i}.";
                    return false;
                }

                if (i == 0 && stage.StartRound != 1)
                {
                    error = $"Spawn profile '{profileId}' must begin on round one.";
                    return false;
                }

                if (stage.StartRound <= previousRound)
                {
                    error =
                        $"Spawn profile '{profileId}' stages must be ordered by unique start round.";
                    return false;
                }

                copied[i] = stage;
                previousRound = stage.StartRound;
            }

            profile = new ExplorationSpawnProfile(profileId, copied);
            return true;
        }

        public bool TryResolveStage(
            int roundNumber,
            out ExplorationSpawnStage stage)
        {
            stage = null;
            if (roundNumber <= 0)
                return false;

            for (int i = stages.Length - 1; i >= 0; i--)
            {
                if (stages[i].StartRound <= roundNumber)
                {
                    stage = stages[i];
                    return true;
                }
            }

            return false;
        }

        private static bool HasStrictId(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   string.Equals(value, value.Trim(), StringComparison.Ordinal);
        }
    }

    public sealed class ExplorationSpawnBalanceTable
    {
        private readonly Dictionary<string, ExplorationSpawnProfile> profiles;

        private ExplorationSpawnBalanceTable(
            Dictionary<string, ExplorationSpawnProfile> profiles)
        {
            this.profiles = profiles;
        }

        public static bool TryCreate(
            IReadOnlyList<ExplorationSpawnProfile> profiles,
            out ExplorationSpawnBalanceTable table,
            out string error)
        {
            table = null;
            error = string.Empty;
            if (profiles == null || profiles.Count == 0)
            {
                error = "Exploration spawn balance requires at least one profile.";
                return false;
            }

            Dictionary<string, ExplorationSpawnProfile> indexed =
                new(StringComparer.Ordinal);
            for (int i = 0; i < profiles.Count; i++)
            {
                ExplorationSpawnProfile profile = profiles[i];
                if (profile == null)
                {
                    error = $"Exploration spawn profile {i} is missing.";
                    return false;
                }

                if (!indexed.TryAdd(profile.ProfileId, profile))
                {
                    error =
                        $"Spawn profile '{profile.ProfileId}' occurs more than once.";
                    return false;
                }
            }

            table = new ExplorationSpawnBalanceTable(indexed);
            return true;
        }

        public bool TryResolve(
            string profileId,
            int roundNumber,
            out ExplorationSpawnStage stage)
        {
            stage = null;
            string normalizedId = profileId?.Trim() ?? string.Empty;
            return normalizedId.Length > 0 &&
                   profiles.TryGetValue(normalizedId, out ExplorationSpawnProfile profile) &&
                   profile.TryResolveStage(roundNumber, out stage);
        }
    }
}
