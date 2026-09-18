using System;
using System.Collections.Generic;
using UnityEngine;

namespace Titanhold.Run
{
    [Serializable]
    public sealed class RunRoundBalanceEntryDefinition
    {
        [SerializeField, Min(1)] private int roundNumber = 1;
        [SerializeField, Min(0.01f)] private float maxThreat = 100f;
        [SerializeField, Min(0.01f)]
        private float enemyHealthMultiplier = 1f;
        [SerializeField, Min(0.01f)]
        private float enemyDamageMultiplier = 1f;
        [SerializeField, Min(0.01f)]
        private float experienceMultiplier = 1f;

        public int RoundNumber => roundNumber;
        public float MaxThreat => maxThreat;
        public float EnemyHealthMultiplier => enemyHealthMultiplier;
        public float EnemyDamageMultiplier => enemyDamageMultiplier;
        public float ExperienceMultiplier => experienceMultiplier;

        public bool TryCreateSnapshot(
            out RunRoundBalanceSnapshot snapshot,
            out string error)
        {
            snapshot = default;
            error = string.Empty;
            try
            {
                snapshot = new RunRoundBalanceSnapshot(
                    roundNumber,
                    maxThreat,
                    enemyHealthMultiplier,
                    enemyDamageMultiplier,
                    experienceMultiplier);
                return true;
            }
            catch (ArgumentOutOfRangeException exception)
            {
                error =
                    $"Round balance entry for round {roundNumber} is invalid: " +
                    $"{exception.ParamName}.";
                return false;
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int configuredRoundNumber,
            float configuredMaxThreat,
            float configuredEnemyHealthMultiplier,
            float configuredEnemyDamageMultiplier,
            float configuredExperienceMultiplier)
        {
            roundNumber = configuredRoundNumber;
            maxThreat = configuredMaxThreat;
            enemyHealthMultiplier = configuredEnemyHealthMultiplier;
            enemyDamageMultiplier = configuredEnemyDamageMultiplier;
            experienceMultiplier = configuredExperienceMultiplier;
        }
#endif
    }

    [CreateAssetMenu(
        fileName = "RunRoundBalance",
        menuName = "Titanhold/Run/Round Balance")]
    public sealed class RunRoundBalanceDefinition : ScriptableObject
    {
        [SerializeField] private RunRoundBalanceEntryDefinition[] rounds =
            Array.Empty<RunRoundBalanceEntryDefinition>();

        public IReadOnlyList<RunRoundBalanceEntryDefinition> Rounds =>
            rounds ?? Array.Empty<RunRoundBalanceEntryDefinition>();
        public bool IsValid => TryCreateTable(out _, out _);

        public bool TryCreateTable(
            out RunRoundBalanceTable table,
            out string error)
        {
            table = null;
            error = string.Empty;
            RunRoundBalanceEntryDefinition[] definitions =
                rounds ?? Array.Empty<RunRoundBalanceEntryDefinition>();
            RunRoundBalanceSnapshot[] snapshots =
                new RunRoundBalanceSnapshot[definitions.Length];
            for (int i = 0; i < definitions.Length; i++)
            {
                RunRoundBalanceEntryDefinition definition = definitions[i];
                if (definition == null)
                {
                    error =
                        $"Round balance '{name}' has a null entry at index {i}.";
                    return false;
                }

                if (!definition.TryCreateSnapshot(
                        out snapshots[i],
                        out string entryError))
                {
                    error =
                        $"Round balance '{name}' is invalid: {entryError}";
                    return false;
                }
            }

            if (!RunRoundBalanceTable.TryCreate(
                    snapshots,
                    out table,
                    out string tableError))
            {
                error =
                    $"Round balance '{name}' is invalid: {tableError}";
                return false;
            }

            return true;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            RunRoundBalanceEntryDefinition[] configuredRounds)
        {
            rounds = configuredRounds ??
                Array.Empty<RunRoundBalanceEntryDefinition>();
        }
#endif
    }
}
