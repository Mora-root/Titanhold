using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Titanhold.Run
{
    public readonly struct RunCombatResourceDefinition
    {
        public RunCombatResourceDefinition(
            string resourceId,
            float maximum,
            float initial)
        {
            ResourceId = resourceId ?? string.Empty;
            Maximum = maximum;
            Initial = initial;
        }

        public string ResourceId { get; }
        public float Maximum { get; }
        public float Initial { get; }
        public bool IsValid =>
            ResourceId.Length > 0 &&
            IsFinite(Maximum) &&
            Maximum > 0f &&
            IsFinite(Initial) &&
            Initial >= 0f &&
            Initial <= Maximum;

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public sealed class RunCombatResourceLoadout
    {
        private readonly ReadOnlyCollection<RunCombatResourceDefinition>
            resources;

        public RunCombatResourceLoadout(
            string loadoutId,
            string characterArchetypeId,
            IReadOnlyList<RunCombatResourceDefinition> resources)
        {
            LoadoutId = loadoutId ?? string.Empty;
            CharacterArchetypeId = characterArchetypeId ?? string.Empty;

            int count = resources?.Count ?? 0;
            RunCombatResourceDefinition[] copy =
                new RunCombatResourceDefinition[count];
            for (int i = 0; i < count; i++)
                copy[i] = resources[i];

            this.resources = Array.AsReadOnly(copy);
        }

        public string LoadoutId { get; }
        public string CharacterArchetypeId { get; }
        public IReadOnlyList<RunCombatResourceDefinition> Resources => resources;
    }

    public interface IRunCombatResourceLoadoutResolver
    {
        bool TryResolve(
            string characterArchetypeId,
            out RunCombatResourceLoadout loadout);
    }
}
