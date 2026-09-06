using System;
using System.Collections.Generic;
using Titanhold.Combat.Abilities;

namespace Titanhold.Run
{
    public sealed class RunStartingAbilityPoolRegistry :
        IRunStartingAbilityPoolResolver
    {
        private readonly Dictionary<string, RunStartingAbilityPool>
            poolsByArchetype;

        private RunStartingAbilityPoolRegistry(
            Dictionary<string, RunStartingAbilityPool> poolsByArchetype)
        {
            this.poolsByArchetype = poolsByArchetype;
        }

        public int Count => poolsByArchetype.Count;

        public static bool TryCreate(
            IReadOnlyList<RunStartingAbilityPool> source,
            IAbilityDefinitionResolver abilityDefinitions,
            out RunStartingAbilityPoolRegistry registry,
            out string error)
        {
            registry = null;
            error = string.Empty;
            if (source == null || source.Count == 0)
            {
                error = "At least one starting ability pool is required.";
                return false;
            }

            if (abilityDefinitions == null)
            {
                error = "An ability definition resolver is required.";
                return false;
            }

            Dictionary<string, RunStartingAbilityPool> byArchetype =
                new(StringComparer.Ordinal);
            HashSet<string> poolIds = new(StringComparer.Ordinal);
            for (int i = 0; i < source.Count; i++)
            {
                RunStartingAbilityPool pool = source[i];
                if (!TryValidatePool(
                        pool,
                        i,
                        abilityDefinitions,
                        out error))
                {
                    return false;
                }

                if (!poolIds.Add(pool.PoolId))
                {
                    error =
                        $"Starting ability pool id '{pool.PoolId}' occurs more than once.";
                    return false;
                }

                if (!byArchetype.TryAdd(pool.CharacterArchetypeId, pool))
                {
                    error =
                        $"Character archetype '{pool.CharacterArchetypeId}' has more than one starting ability pool.";
                    return false;
                }
            }

            registry = new RunStartingAbilityPoolRegistry(byArchetype);
            return true;
        }

        public bool TryResolve(
            string characterArchetypeId,
            out RunStartingAbilityPool pool)
        {
            pool = null;
            string normalizedId =
                characterArchetypeId?.Trim() ?? string.Empty;
            return normalizedId.Length > 0 &&
                   poolsByArchetype.TryGetValue(normalizedId, out pool);
        }

        private static bool TryValidatePool(
            RunStartingAbilityPool pool,
            int index,
            IAbilityDefinitionResolver abilityDefinitions,
            out string error)
        {
            error = string.Empty;
            if (pool == null)
            {
                error = $"Starting ability pool {index} is missing.";
                return false;
            }

            if (!HasStrictId(pool.PoolId))
            {
                error = $"Starting ability pool {index} has an invalid stable id.";
                return false;
            }

            if (!HasStrictId(pool.CharacterArchetypeId))
            {
                error =
                    $"Starting ability pool '{pool.PoolId}' has an invalid character archetype id.";
                return false;
            }

            if (pool.AbilityIds.Count !=
                RunStartingAbilitySelectionService.StartingAbilityOptionCount)
            {
                error =
                    $"Starting ability pool '{pool.PoolId}' must contain exactly " +
                    $"{RunStartingAbilitySelectionService.StartingAbilityOptionCount} abilities.";
                return false;
            }

            HashSet<string> abilityIds = new(StringComparer.Ordinal);
            for (int i = 0; i < pool.AbilityIds.Count; i++)
            {
                string abilityId = pool.AbilityIds[i];
                if (!HasStrictId(abilityId))
                {
                    error =
                        $"Ability {i} in starting pool '{pool.PoolId}' has an invalid stable id.";
                    return false;
                }

                if (!abilityIds.Add(abilityId))
                {
                    error =
                        $"Ability id '{abilityId}' occurs more than once in starting pool '{pool.PoolId}'.";
                    return false;
                }

                if (!abilityDefinitions.TryResolve(
                        abilityId,
                        out IAbilityDefinition definition) ||
                    definition == null ||
                    !string.Equals(
                        definition.AbilityId,
                        abilityId,
                        StringComparison.Ordinal))
                {
                    error =
                        $"Ability id '{abilityId}' in starting pool '{pool.PoolId}' is not available in the ability catalog.";
                    return false;
                }
            }

            return true;
        }

        private static bool HasStrictId(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   string.Equals(value, value.Trim(), StringComparison.Ordinal);
        }
    }
}
