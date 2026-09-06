using System;
using System.Collections.Generic;

namespace Titanhold.Combat.Abilities
{
    public interface IAbilityDefinition
    {
        string AbilityId { get; }
    }

    public interface IAbilityDefinitionResolver
    {
        bool TryResolve(
            string abilityId,
            out IAbilityDefinition definition);
    }

    public sealed class AbilityDefinitionRegistry : IAbilityDefinitionResolver
    {
        private readonly Dictionary<string, IAbilityDefinition> definitions;

        private AbilityDefinitionRegistry(
            Dictionary<string, IAbilityDefinition> definitions)
        {
            this.definitions = definitions;
        }

        public int Count => definitions.Count;

        public static bool TryCreate(
            IReadOnlyList<IAbilityDefinition> source,
            out AbilityDefinitionRegistry registry,
            out string error)
        {
            registry = null;
            error = string.Empty;
            if (source == null)
            {
                error = "Ability definitions are missing.";
                return false;
            }

            if (source.Count == 0)
            {
                error = "At least one ability definition is required.";
                return false;
            }

            Dictionary<string, IAbilityDefinition> index =
                new(StringComparer.Ordinal);
            for (int i = 0; i < source.Count; i++)
            {
                IAbilityDefinition definition = source[i];
                if (definition == null)
                {
                    error = $"Ability definition {i} is missing.";
                    return false;
                }

                string abilityId = definition.AbilityId?.Trim() ?? string.Empty;
                if (abilityId.Length == 0)
                {
                    error = $"Ability definition {i} has no stable id.";
                    return false;
                }

                if (!string.Equals(
                        abilityId,
                        definition.AbilityId,
                        StringComparison.Ordinal))
                {
                    error =
                        $"Ability definition '{definition.AbilityId}' has surrounding whitespace.";
                    return false;
                }

                if (!index.TryAdd(abilityId, definition))
                {
                    error = $"Ability id '{abilityId}' occurs more than once.";
                    return false;
                }
            }

            registry = new AbilityDefinitionRegistry(index);
            return true;
        }

        public bool TryResolve(
            string abilityId,
            out IAbilityDefinition definition)
        {
            definition = null;
            string normalizedId = abilityId?.Trim() ?? string.Empty;
            return normalizedId.Length > 0 &&
                   definitions.TryGetValue(normalizedId, out definition);
        }
    }
}
