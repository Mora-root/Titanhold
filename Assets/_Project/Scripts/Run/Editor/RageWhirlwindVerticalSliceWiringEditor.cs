using System;
using System.Collections.Generic;
using Titanhold.Combat.Abilities;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class RageWhirlwindVerticalSliceWiringEditor
    {
        private const string SpinFallbackPath =
            "Assets/_Project/ScriptableObjects/Configs/SpinAbility.asset";
        private const string WhirlwindPath =
            "Assets/_Project/ScriptableObjects/Abilities/Whirlwind.asset";
        private const string AbilityCatalogPath =
            "Assets/_Project/ScriptableObjects/Abilities/AbilityDefinitionCatalog.asset";
        private const string SchedulePath =
            "Assets/_Project/ScriptableObjects/Run/WarriorAbilityUnlockSchedule.asset";
        private const string ChargePath =
            "Assets/_Project/ScriptableObjects/Abilities/Charge.asset";
        private const string IronGuardPath =
            "Assets/_Project/ScriptableObjects/Abilities/IronGuard.asset";
        private const string BattleCryPath =
            "Assets/_Project/ScriptableObjects/Abilities/BattleCry.asset";

        [MenuItem("Tools/Titanhold/Install Rage Whirlwind Ability Content")]
        public static void Install()
        {
            try
            {
                RequireEditMode();
                AreaDamageAbilityDefinition spinFallback =
                    RequireAsset<AreaDamageAbilityDefinition>(SpinFallbackPath);
                AreaDamageAbilityDefinition whirlwind =
                    CreateOrLoadWhirlwind();
                AbilityDefinitionCatalog abilities =
                    RequireAsset<AbilityDefinitionCatalog>(AbilityCatalogPath);
                RunAbilityUnlockScheduleDefinition schedule =
                    RequireAsset<RunAbilityUnlockScheduleDefinition>(
                        SchedulePath);

                SpinAbilityWiringEditor.ConfigureApprovedDefinition(
                    spinFallback);
                ConfigureWhirlwind(whirlwind);
                EnsureCatalogContains(abilities, whirlwind);
                ConfigureSchedule(schedule, whirlwind);
                AssetDatabase.SaveAssets();
                ValidateInternal(
                    spinFallback,
                    whirlwind,
                    abilities,
                    schedule);
                Debug.Log("Rage Whirlwind ability content installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Rage Whirlwind ability content installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Rage Whirlwind Ability Content")]
        public static void Validate()
        {
            try
            {
                RequireEditMode();
                ValidateInternal(
                    RequireAsset<AreaDamageAbilityDefinition>(
                        SpinFallbackPath),
                    RequireAsset<AreaDamageAbilityDefinition>(WhirlwindPath),
                    RequireAsset<AbilityDefinitionCatalog>(
                        AbilityCatalogPath),
                    RequireAsset<RunAbilityUnlockScheduleDefinition>(
                        SchedulePath));
                Debug.Log(
                    "Rage Whirlwind ability content validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Rage Whirlwind ability content validation failed: {exception}");
            }
        }

        private static AreaDamageAbilityDefinition CreateOrLoadWhirlwind()
        {
            AreaDamageAbilityDefinition whirlwind =
                AssetDatabase.LoadAssetAtPath<AreaDamageAbilityDefinition>(
                    WhirlwindPath);
            if (whirlwind != null)
                return whirlwind;

            if (AssetDatabase.LoadMainAssetAtPath(WhirlwindPath) != null)
            {
                throw new InvalidOperationException(
                    "Whirlwind path is occupied by another asset.");
            }

            whirlwind = ScriptableObject.CreateInstance<
                AreaDamageAbilityDefinition>();
            AssetDatabase.CreateAsset(whirlwind, WhirlwindPath);
            return whirlwind;
        }

        private static void ConfigureWhirlwind(
            AreaDamageAbilityDefinition whirlwind)
        {
            SerializedObject data = new(whirlwind);
            data.FindProperty("abilityId").stringValue =
                "ability:whirlwind";
            data.FindProperty("displayName").stringValue = "Whirlwind";
            data.FindProperty("description").stringValue =
                "Strike all nearby enemies. Costs 2 Rage.";
            data.FindProperty("resourceCost").floatValue = 20f;
            data.FindProperty("cooldown").floatValue = 8f;
            data.FindProperty("windUp").floatValue = 0.23333333f;
            data.FindProperty("recovery").floatValue = 0.30000003f;
            data.FindProperty("damageMultiplier").floatValue = 2f;
            data.FindProperty("radius").floatValue = 2.5f;
            data.FindProperty("targetMask").intValue = 64;
            data.FindProperty("animatorTrigger").stringValue = "Spin";
            SerializedProperty cost =
                data.FindProperty("combatResourceCost");
            cost.FindPropertyRelative("enabled").boolValue = true;
            cost.FindPropertyRelative("resourceId").stringValue =
                "resource:rage";
            cost.FindPropertyRelative("amount").floatValue = 2f;
            SerializedProperty gain =
                data.FindProperty("sourceResourceGain");
            gain.FindPropertyRelative("enabled").boolValue = false;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(whirlwind);
        }

        private static void EnsureCatalogContains(
            AbilityDefinitionCatalog catalog,
            AreaDamageAbilityDefinition whirlwind)
        {
            List<ScriptableObject> definitions =
                new(catalog.Definitions.Count + 1);
            bool found = false;
            for (int i = 0; i < catalog.Definitions.Count; i++)
            {
                ScriptableObject definition = catalog.Definitions[i];
                definitions.Add(definition);
                if (definition == whirlwind)
                    found = true;
            }

            if (!found)
                definitions.Add(whirlwind);

            catalog.ConfigureForEditor(definitions.ToArray());
            EditorUtility.SetDirty(catalog);
        }

        private static void ConfigureSchedule(
            RunAbilityUnlockScheduleDefinition schedule,
            AreaDamageAbilityDefinition whirlwind)
        {
            RunAbilityUnlockMilestoneDefinition[] milestones =
            {
                Milestone(
                    3,
                    1,
                    1,
                    new ScriptableObject[] { whirlwind },
                    true),
                Milestone(
                    7,
                    2,
                    1,
                    new[]
                    {
                        RequireAsset<ScriptableObject>(ChargePath)
                    },
                    true),
                Milestone(
                    10,
                    3,
                    1,
                    new[]
                    {
                        RequireAsset<ScriptableObject>(IronGuardPath)
                    },
                    true),
                Milestone(
                    15,
                    4,
                    1,
                    new[]
                    {
                        RequireAsset<ScriptableObject>(BattleCryPath)
                    },
                    true)
            };
            schedule.ConfigureForEditor(
                "ability-schedule:warrior",
                "archetype:warrior",
                milestones);
            EditorUtility.SetDirty(schedule);
        }

        private static RunAbilityUnlockMilestoneDefinition Milestone(
            int level,
            int slotIndex,
            int optionCount,
            ScriptableObject[] abilities,
            bool enabled)
        {
            RunAbilityUnlockMilestoneDefinition milestone = new();
            milestone.ConfigureForEditor(
                level,
                slotIndex,
                optionCount,
                abilities,
                enabled);
            return milestone;
        }

        private static void ValidateInternal(
            AreaDamageAbilityDefinition spinFallback,
            AreaDamageAbilityDefinition whirlwind,
            AbilityDefinitionCatalog abilities,
            RunAbilityUnlockScheduleDefinition schedule)
        {
            if (!spinFallback.TryCreateSnapshot(
                    20f,
                    out AreaDamageAbilitySnapshot fallbackSnapshot) ||
                fallbackSnapshot.Execution.AbilityId != "ability:spin" ||
                fallbackSnapshot.Execution.Cooldown != 3d ||
                fallbackSnapshot.Execution.CombatResourceCost.IsConfigured ||
                fallbackSnapshot.Damage != 30f)
            {
                throw new InvalidOperationException(
                    "Direct-scene Spin fallback was changed by run content.");
            }

            if (!whirlwind.TryCreateSnapshot(
                    20f,
                    out AreaDamageAbilitySnapshot snapshot) ||
                whirlwind.DisplayName != "Whirlwind" ||
                snapshot.Execution.AbilityId != "ability:whirlwind" ||
                snapshot.Execution.ResourceCost != 20f ||
                snapshot.Execution.Cooldown != 8d ||
                snapshot.Execution.CombatResourceCost.ResourceId !=
                    "resource:rage" ||
                snapshot.Execution.CombatResourceCost.Amount != 2f ||
                snapshot.Damage != 40f ||
                snapshot.Radius != 2.5f)
            {
                throw new InvalidOperationException(
                    "Rage Whirlwind does not match the approved prototype balance.");
            }

            if (!abilities.IsValid ||
                !abilities.TryResolve(
                    "ability:whirlwind",
                    out IAbilityDefinition resolved) ||
                !ReferenceEquals(resolved, whirlwind))
            {
                throw new InvalidOperationException(
                    "Rage Whirlwind is missing from the ability catalog.");
            }

            if (!schedule.TryCreateSchedule(
                    out RunAbilityUnlockSchedule runtime,
                    out string error) ||
                runtime.Milestones.Count != 4 ||
                runtime.Milestones[0].UnlockLevel != 3 ||
                runtime.Milestones[0].TargetSlotIndex != 1 ||
                runtime.Milestones[0].OptionCount != 1 ||
                runtime.Milestones[0].CandidateAbilityIds.Count != 1 ||
                runtime.Milestones[0].CandidateAbilityIds[0] !=
                    "ability:whirlwind")
            {
                throw new InvalidOperationException(
                    $"Warrior Rage Whirlwind milestone is invalid: {error}");
            }
        }

        private static T RequireAsset<T>(string path)
            where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException($"Asset is missing: {path}");

            return asset;
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Exit Play Mode before Rage Whirlwind content wiring.");
            }
        }
    }
}
