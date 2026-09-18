using System;
using Titanhold.Combat.Abilities;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class RunAbilityUnlockScheduleValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Run Ability Unlock Schedules")]
        public static void Validate()
        {
            AbilityDefinitionCatalog abilities = null;
            RunAbilityUnlockScheduleDefinition definition = null;
            RunAbilityUnlockScheduleCatalog catalog = null;
            ValidationAbilityDefinition[] abilityAssets = null;
            try
            {
                abilityAssets = new[]
                {
                    CreateAbility("ability:a"),
                    CreateAbility("ability:b"),
                    CreateAbility("ability:c")
                };
                abilities = ScriptableObject.CreateInstance<
                    AbilityDefinitionCatalog>();
                abilities.ConfigureForEditor(abilityAssets);

                RunAbilityUnlockMilestoneDefinition levelTwo = new();
                levelTwo.ConfigureForEditor(
                    2,
                    1,
                    2,
                    abilityAssets);
                definition = ScriptableObject.CreateInstance<
                    RunAbilityUnlockScheduleDefinition>();
                definition.ConfigureForEditor(
                    "ability-schedule:warrior",
                    "archetype:warrior",
                    new[] { levelTwo });
                catalog = ScriptableObject.CreateInstance<
                    RunAbilityUnlockScheduleCatalog>();
                catalog.ConfigureForEditor(
                    abilities,
                    new[] { definition });

                Assert(catalog.IsValid &&
                       catalog.AbilityCatalog == abilities &&
                       catalog.TryResolve(
                           "archetype:warrior",
                           out RunAbilityUnlockSchedule schedule) &&
                       schedule.ScheduleId ==
                           "ability-schedule:warrior" &&
                       schedule.Milestones.Count == 1 &&
                       schedule.Milestones[0].UnlockLevel == 2 &&
                       schedule.Milestones[0].TargetSlotIndex == 1 &&
                       schedule.Milestones[0].OptionCount == 2,
                    "Valid ability unlock catalog did not resolve its schedule.");

                catalog.ConfigureForEditor(
                    abilities,
                    new[] { definition, definition });
                Assert(!catalog.IsValid &&
                       !catalog.TryResolve(
                           "archetype:warrior",
                           out _),
                    "Duplicate schedules exposed a partial catalog.");

                RunAbilityUnlockMilestoneDefinition invalidSlot = new();
                invalidSlot.ConfigureForEditor(
                    2,
                    RunAbilityLoadoutService.DefaultAbilitySlotCount,
                    1,
                    new[] { abilityAssets[0] });
                definition.ConfigureForEditor(
                    "ability-schedule:warrior",
                    "archetype:warrior",
                    new[] { invalidSlot });
                catalog.ConfigureForEditor(
                    abilities,
                    new[] { definition });
                Assert(!catalog.IsValid,
                    "A schedule targeting a missing loadout slot was accepted.");

                Debug.Log(
                    "Run Ability Unlock Schedules validation passed (3 scenarios).");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Ability Unlock Schedules validation failed: {exception}");
            }
            finally
            {
                if (abilityAssets != null)
                {
                    for (int i = 0; i < abilityAssets.Length; i++)
                        Destroy(abilityAssets[i]);
                }

                Destroy(catalog);
                Destroy(definition);
                Destroy(abilities);
            }
        }

        private static ValidationAbilityDefinition CreateAbility(string id)
        {
            ValidationAbilityDefinition definition =
                ScriptableObject.CreateInstance<
                    ValidationAbilityDefinition>();
            definition.Initialize(id);
            return definition;
        }

        private static void Destroy(UnityEngine.Object instance)
        {
            if (instance != null)
                UnityEngine.Object.DestroyImmediate(instance);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class ValidationAbilityDefinition :
            ScriptableObject,
            IAbilityDefinition
        {
            public string AbilityId { get; private set; }

            public void Initialize(string abilityId)
            {
                AbilityId = abilityId;
            }
        }
    }
}
