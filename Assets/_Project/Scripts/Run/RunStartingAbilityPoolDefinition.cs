using System;
using System.Collections.Generic;
using Titanhold.Combat.Abilities;
using UnityEngine;

namespace Titanhold.Run
{
    [CreateAssetMenu(
        fileName = "StartingAbilityPool",
        menuName = "Titanhold/Run/Starting Ability Pool")]
    public sealed class RunStartingAbilityPoolDefinition : ScriptableObject
    {
        [SerializeField] private string poolId;
        [SerializeField] private string characterArchetypeId;
        [SerializeField] private ScriptableObject[] abilityDefinitions =
            new ScriptableObject[
                RunStartingAbilitySelectionService.StartingAbilityOptionCount];

        public string PoolId => poolId ?? string.Empty;
        public string CharacterArchetypeId =>
            characterArchetypeId ?? string.Empty;
        public IReadOnlyList<ScriptableObject> AbilityDefinitions =>
            abilityDefinitions ?? Array.Empty<ScriptableObject>();

        public bool TryCreatePool(
            out RunStartingAbilityPool pool,
            out string error)
        {
            pool = null;
            error = string.Empty;
            ScriptableObject[] definitions =
                abilityDefinitions ?? Array.Empty<ScriptableObject>();
            if (definitions.Length !=
                RunStartingAbilitySelectionService.StartingAbilityOptionCount)
            {
                error =
                    $"Starting ability pool '{name}' must reference exactly " +
                    $"{RunStartingAbilitySelectionService.StartingAbilityOptionCount} abilities.";
                return false;
            }

            string[] abilityIds = new string[definitions.Length];
            for (int i = 0; i < definitions.Length; i++)
            {
                ScriptableObject asset = definitions[i];
                if (asset == null)
                {
                    error =
                        $"Starting ability pool '{name}' has no ability at index {i}.";
                    return false;
                }

                if (asset is not IAbilityDefinition definition)
                {
                    error =
                        $"Asset '{asset.name}' in starting ability pool '{name}' " +
                        "does not implement IAbilityDefinition.";
                    return false;
                }

                abilityIds[i] = definition.AbilityId;
            }

            pool = new RunStartingAbilityPool(
                PoolId,
                CharacterArchetypeId,
                abilityIds);
            return true;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string configuredPoolId,
            string configuredCharacterArchetypeId,
            ScriptableObject[] configuredAbilityDefinitions)
        {
            poolId = configuredPoolId;
            characterArchetypeId = configuredCharacterArchetypeId;
            abilityDefinitions = configuredAbilityDefinitions ??
                Array.Empty<ScriptableObject>();
        }
#endif
    }
}
