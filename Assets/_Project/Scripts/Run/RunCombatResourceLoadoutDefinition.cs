using System;
using System.Collections.Generic;
using UnityEngine;

namespace Titanhold.Run
{
    [Serializable]
    public struct RunCombatResourceEntry
    {
        [SerializeField] private string resourceId;
        [SerializeField, Min(0.01f)] private float maximum;
        [SerializeField, Min(0f)] private float initial;

        public RunCombatResourceEntry(
            string resourceId,
            float maximum,
            float initial)
        {
            this.resourceId = resourceId;
            this.maximum = maximum;
            this.initial = initial;
        }

        public RunCombatResourceDefinition ToRuntimeDefinition()
        {
            return new RunCombatResourceDefinition(
                resourceId,
                maximum,
                initial);
        }
    }

    [CreateAssetMenu(
        fileName = "CombatResourceLoadout",
        menuName = "Titanhold/Run/Combat Resource Loadout")]
    public sealed class RunCombatResourceLoadoutDefinition : ScriptableObject
    {
        [SerializeField] private string loadoutId;
        [SerializeField] private string characterArchetypeId;
        [SerializeField] private RunCombatResourceEntry[] resources =
            Array.Empty<RunCombatResourceEntry>();

        public string LoadoutId => loadoutId ?? string.Empty;
        public string CharacterArchetypeId =>
            characterArchetypeId ?? string.Empty;
        public IReadOnlyList<RunCombatResourceEntry> Resources =>
            resources ?? Array.Empty<RunCombatResourceEntry>();

        public bool TryCreateLoadout(
            out RunCombatResourceLoadout loadout,
            out string error)
        {
            loadout = null;
            RunCombatResourceEntry[] source =
                resources ?? Array.Empty<RunCombatResourceEntry>();
            RunCombatResourceDefinition[] runtimeResources =
                new RunCombatResourceDefinition[source.Length];
            for (int i = 0; i < source.Length; i++)
                runtimeResources[i] = source[i].ToRuntimeDefinition();

            RunCombatResourceLoadout candidate = new(
                LoadoutId,
                CharacterArchetypeId,
                runtimeResources);
            if (!RunCombatResourceLoadoutRegistry.TryCreate(
                    new[] { candidate },
                    out _,
                    out error))
            {
                return false;
            }

            loadout = candidate;
            return true;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string configuredLoadoutId,
            string configuredCharacterArchetypeId,
            RunCombatResourceEntry[] configuredResources)
        {
            loadoutId = configuredLoadoutId;
            characterArchetypeId = configuredCharacterArchetypeId;
            resources = configuredResources ??
                Array.Empty<RunCombatResourceEntry>();
        }
#endif
    }
}
