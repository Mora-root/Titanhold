using System;
using System.Collections.Generic;
using Titanhold.Enemies;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public readonly struct BalanceRoundRow
    {
        public BalanceRoundRow(RunRoundBalanceSnapshot snapshot)
        {
            RoundNumber = snapshot.RoundNumber;
            MaxThreat = snapshot.MaxThreat;
            HealthMultiplier = snapshot.EnemyScaling.HealthMultiplier;
            DamageMultiplier = snapshot.EnemyScaling.DamageMultiplier;
            ExperienceMultiplier = snapshot.ExperienceMultiplier;
        }

        public int RoundNumber { get; }
        public float MaxThreat { get; }
        public float HealthMultiplier { get; }
        public float DamageMultiplier { get; }
        public float ExperienceMultiplier { get; }
    }

    public readonly struct BalanceLevelRow
    {
        public BalanceLevelRow(
            int currentLevel,
            int experienceToNext,
            long cumulativeExperience)
        {
            CurrentLevel = currentLevel;
            ExperienceToNext = experienceToNext;
            CumulativeExperience = cumulativeExperience;
        }

        public int CurrentLevel { get; }
        public int ExperienceToNext { get; }
        public long CumulativeExperience { get; }
    }

    public readonly struct BalanceEnemyRow
    {
        public BalanceEnemyRow(
            string enemyId,
            string prefabName,
            EnemyBaseStats baseStats,
            RunRoundBalanceSnapshot round,
            int baseRunExperience,
            int scaledRunExperience,
            float threatAmount,
            int instabilityPoints,
            int killsToFillMeter,
            bool hasLootTable)
        {
            EnemyId = enemyId ?? string.Empty;
            PrefabName = prefabName ?? string.Empty;
            BaseStats = baseStats;
            EffectiveHealth = baseStats.MaximumHealth *
                              round.EnemyScaling.HealthMultiplier;
            EffectiveDamage = baseStats.BaseDamage *
                              round.EnemyScaling.DamageMultiplier;
            EffectiveDps = EffectiveDamage * baseStats.AttacksPerSecond;
            BaseRunExperience = baseRunExperience;
            ScaledRunExperience = scaledRunExperience;
            ThreatAmount = threatAmount;
            InstabilityPoints = instabilityPoints;
            KillsToFillMeter = killsToFillMeter;
            HasLootTable = hasLootTable;
        }

        public string EnemyId { get; }
        public string PrefabName { get; }
        public EnemyBaseStats BaseStats { get; }
        public float EffectiveHealth { get; }
        public float EffectiveDamage { get; }
        public float EffectiveDps { get; }
        public int BaseRunExperience { get; }
        public int ScaledRunExperience { get; }
        public float ThreatAmount { get; }
        public int InstabilityPoints { get; }
        public int KillsToFillMeter { get; }
        public bool HasLootTable { get; }
    }

    public sealed class BalanceOverviewReport
    {
        public BalanceOverviewReport(
            RunRoundBalanceSnapshot selectedRound,
            IReadOnlyList<BalanceRoundRow> rounds,
            IReadOnlyList<BalanceLevelRow> levels,
            IReadOnlyList<BalanceEnemyRow> enemies,
            long totalExperienceToMaximumLevel)
        {
            SelectedRound = selectedRound;
            Rounds = rounds ?? throw new ArgumentNullException(nameof(rounds));
            Levels = levels ?? throw new ArgumentNullException(nameof(levels));
            Enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
            TotalExperienceToMaximumLevel = totalExperienceToMaximumLevel;
        }

        public RunRoundBalanceSnapshot SelectedRound { get; }
        public IReadOnlyList<BalanceRoundRow> Rounds { get; }
        public IReadOnlyList<BalanceLevelRow> Levels { get; }
        public IReadOnlyList<BalanceEnemyRow> Enemies { get; }
        public long TotalExperienceToMaximumLevel { get; }
    }

    public static class BalanceOverviewReportBuilder
    {
        public static bool TryBuild(
            EnemyDefinitionCatalog enemies,
            RunRoundBalanceDefinition roundBalance,
            RunProgressionDefinition progression,
            int selectedRoundNumber,
            out BalanceOverviewReport report,
            out string error)
        {
            report = null;
            error = string.Empty;
            if (enemies == null || roundBalance == null || progression == null)
            {
                error = "Enemy, round, and progression definitions are required.";
                return false;
            }

            if (!enemies.IsValid)
            {
                error = enemies.ValidationError;
                return false;
            }

            if (!roundBalance.TryCreateTable(
                    out RunRoundBalanceTable roundTable,
                    out error))
            {
                return false;
            }

            if (!roundTable.TryResolve(
                    selectedRoundNumber,
                    out RunRoundBalanceSnapshot selectedRound))
            {
                error = $"Round {selectedRoundNumber} is absent from the balance table.";
                return false;
            }

            if (!progression.TryBuildCurve(
                    out RunExperienceCurve experienceCurve,
                    out error))
            {
                return false;
            }

            List<BalanceRoundRow> roundRows = new(roundBalance.Rounds.Count);
            for (int i = 0; i < roundBalance.Rounds.Count; i++)
            {
                RunRoundBalanceEntryDefinition definition =
                    roundBalance.Rounds[i];
                string snapshotError = string.Empty;
                if (definition == null ||
                    !definition.TryCreateSnapshot(
                        out RunRoundBalanceSnapshot snapshot,
                        out snapshotError))
                {
                    error = !string.IsNullOrEmpty(snapshotError)
                        ? snapshotError
                        : $"Round entry {i} is missing.";
                    return false;
                }

                roundRows.Add(new BalanceRoundRow(snapshot));
            }

            List<BalanceLevelRow> levelRows =
                new(Math.Max(0, experienceCurve.MaximumLevel - 1));
            long cumulativeExperience = 0;
            for (int level = 1;
                 level < experienceCurve.MaximumLevel;
                 level++)
            {
                if (!experienceCurve.TryGetRequirement(
                        level,
                        out int requirement))
                {
                    error = $"RunXP requirement for level {level} is missing.";
                    return false;
                }

                cumulativeExperience += requirement;
                levelRows.Add(new BalanceLevelRow(
                    level,
                    requirement,
                    cumulativeExperience));
            }

            List<BalanceEnemyRow> enemyRows =
                new(enemies.Definitions.Count);
            for (int i = 0; i < enemies.Definitions.Count; i++)
            {
                EnemyDefinition definition = enemies.Definitions[i];
                string definitionError = string.Empty;
                if (definition == null ||
                    !definition.TryCreateArchetype(
                        out EnemyArchetype archetype,
                        out definitionError))
                {
                    error = !string.IsNullOrEmpty(definitionError)
                        ? definitionError
                        : $"Enemy definition {i} is missing.";
                    return false;
                }

                GameObject prefab = definition.Prefab;
                EnemyRewardSource reward =
                    prefab.GetComponentInChildren<EnemyRewardSource>(true);
                EnemyRunContributionSource contribution =
                    prefab.GetComponentInChildren<EnemyRunContributionSource>(true);
                int baseExperience = reward != null
                    ? reward.RunExperienceAmount
                    : 0;
                int scaledExperience = 0;
                if (baseExperience > 0 &&
                    !RunExperienceRewardCalculator.TryCalculate(
                        baseExperience,
                        selectedRound.ExperienceMultiplier,
                        out scaledExperience))
                {
                    error =
                        $"RunXP scaling failed for '{archetype.EnemyId}'.";
                    return false;
                }

                float threat = contribution != null
                    ? contribution.ThreatAmount
                    : 0f;
                int killsToFill = threat > 0f
                    ? Mathf.CeilToInt(selectedRound.MaxThreat / threat)
                    : 0;
                bool hasLootTable =
                    prefab.GetComponentInChildren<EnemyLootTableDropper>(true) != null;
                enemyRows.Add(new BalanceEnemyRow(
                    archetype.EnemyId,
                    prefab.name,
                    archetype.BaseStats,
                    selectedRound,
                    baseExperience,
                    scaledExperience,
                    threat,
                    contribution != null ? contribution.InstabilityPoints : 0,
                    killsToFill,
                    hasLootTable));
            }

            report = new BalanceOverviewReport(
                selectedRound,
                roundRows,
                levelRows,
                enemyRows,
                cumulativeExperience);
            return true;
        }
    }
}
