using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Titanhold.Run
{
    public sealed class RunStartingAbilityPool
    {
        private readonly ReadOnlyCollection<string> abilityIds;

        public RunStartingAbilityPool(
            string poolId,
            string characterArchetypeId,
            IReadOnlyList<string> abilityIds)
        {
            PoolId = poolId ?? string.Empty;
            CharacterArchetypeId = characterArchetypeId ?? string.Empty;

            int count = abilityIds?.Count ?? 0;
            string[] copy = new string[count];
            for (int i = 0; i < count; i++)
                copy[i] = abilityIds[i] ?? string.Empty;

            this.abilityIds = Array.AsReadOnly(copy);
        }

        public string PoolId { get; }
        public string CharacterArchetypeId { get; }
        public IReadOnlyList<string> AbilityIds => abilityIds;
    }

    public interface IRunStartingAbilityPoolResolver
    {
        bool TryResolve(
            string characterArchetypeId,
            out RunStartingAbilityPool pool);
    }
}
