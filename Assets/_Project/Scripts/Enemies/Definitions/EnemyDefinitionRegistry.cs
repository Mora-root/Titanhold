using System;
using System.Collections.Generic;

namespace Titanhold.Enemies
{
    public sealed class EnemyDefinitionRegistry : IEnemyDefinitionResolver
    {
        private readonly Dictionary<string, EnemyArchetype> byId;

        private EnemyDefinitionRegistry(
            Dictionary<string, EnemyArchetype> byId)
        {
            this.byId = byId;
        }

        public int Count => byId.Count;

        public static bool TryCreate(
            IReadOnlyList<EnemyArchetype> source,
            out EnemyDefinitionRegistry registry,
            out string error)
        {
            registry = null;
            error = string.Empty;
            if (source == null || source.Count == 0)
            {
                error = "At least one enemy archetype is required.";
                return false;
            }

            Dictionary<string, EnemyArchetype> candidates =
                new(StringComparer.Ordinal);
            for (int i = 0; i < source.Count; i++)
            {
                EnemyArchetype archetype = source[i];
                if (archetype == null)
                {
                    error = $"Enemy archetype {i} is missing.";
                    return false;
                }

                if (!HasStrictId(archetype.EnemyId))
                {
                    error = $"Enemy archetype {i} has an invalid stable id.";
                    return false;
                }

                if (!archetype.BaseStats.TryValidate(out string statsError))
                {
                    error =
                        $"Enemy archetype '{archetype.EnemyId}' is invalid: {statsError}";
                    return false;
                }

                if (!candidates.TryAdd(archetype.EnemyId, archetype))
                {
                    error =
                        $"Enemy id '{archetype.EnemyId}' occurs more than once.";
                    return false;
                }
            }

            registry = new EnemyDefinitionRegistry(candidates);
            return true;
        }

        public bool TryResolve(
            string enemyId,
            out EnemyArchetype archetype)
        {
            archetype = null;
            string normalizedId = enemyId?.Trim() ?? string.Empty;
            return normalizedId.Length > 0 &&
                   byId.TryGetValue(normalizedId, out archetype);
        }

        private static bool HasStrictId(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   string.Equals(value, value.Trim(), StringComparison.Ordinal);
        }
    }
}
