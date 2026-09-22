using System;
using System.Collections.Generic;
using Titanhold.Combat.Abilities;
using Titanhold.Combat.Effects;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class WarriorChargeVerticalSliceWiringEditor
    {
        private const string PlayerPrefabPath =
            "Assets/_Project/Prefabs/Player.prefab";
        private const string ChargePath =
            "Assets/_Project/ScriptableObjects/Abilities/Charge.asset";
        private const string AbilityCatalogPath =
            "Assets/_Project/ScriptableObjects/Abilities/AbilityDefinitionCatalog.asset";
        private const string SchedulePath =
            "Assets/_Project/ScriptableObjects/Run/WarriorAbilityUnlockSchedule.asset";
        private const string HeavyStrikePath =
            "Assets/_Project/ScriptableObjects/Abilities/HeavyStrike.asset";
        private const string CrushingStrikePath =
            "Assets/_Project/ScriptableObjects/Abilities/CrushingStrike.asset";
        private const string CleavePath =
            "Assets/_Project/ScriptableObjects/Abilities/Cleave.asset";
        private const string WhirlwindPath =
            "Assets/_Project/ScriptableObjects/Abilities/Whirlwind.asset";

        [MenuItem("Tools/Titanhold/Install Warrior Charge Ability Content")]
        public static void Install()
        {
            try
            {
                RequireEditMode();
                TargetedMovementAbilityDefinition charge =
                    CreateOrLoadCharge();
                AbilityDefinitionCatalog abilities =
                    RequireAsset<AbilityDefinitionCatalog>(
                        AbilityCatalogPath);
                RunAbilityUnlockScheduleDefinition schedule =
                    RequireAsset<RunAbilityUnlockScheduleDefinition>(
                        SchedulePath);

                ConfigureCharge(charge);
                EnsureCatalogContains(abilities, charge);
                ConfigureSchedule(schedule, charge);
                ConfigurePlayerEffectReceiver();
                AssetDatabase.SaveAssets();
                ValidateInternal(charge, abilities, schedule);
                Debug.Log("Warrior Charge ability content installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Warrior Charge ability content installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Warrior Charge Ability Content")]
        public static void Validate()
        {
            try
            {
                RequireEditMode();
                ValidateInternal(
                    RequireAsset<TargetedMovementAbilityDefinition>(
                        ChargePath),
                    RequireAsset<AbilityDefinitionCatalog>(
                        AbilityCatalogPath),
                    RequireAsset<RunAbilityUnlockScheduleDefinition>(
                        SchedulePath));
                Debug.Log(
                    "Warrior Charge ability content validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Warrior Charge ability content validation failed: {exception}");
            }
        }

        private static TargetedMovementAbilityDefinition
            CreateOrLoadCharge()
        {
            TargetedMovementAbilityDefinition charge =
                AssetDatabase.LoadAssetAtPath<
                    TargetedMovementAbilityDefinition>(ChargePath);
            if (charge != null)
                return charge;

            if (AssetDatabase.LoadMainAssetAtPath(ChargePath) != null)
            {
                throw new InvalidOperationException(
                    "Charge path is occupied by another asset.");
            }

            charge = ScriptableObject.CreateInstance<
                TargetedMovementAbilityDefinition>();
            AssetDatabase.CreateAsset(charge, ChargePath);
            return charge;
        }

        private static void ConfigureCharge(
            TargetedMovementAbilityDefinition charge)
        {
            SerializedObject data = new(charge);
            data.FindProperty("abilityId").stringValue = "ability:charge";
            data.FindProperty("displayName").stringValue = "Charge";
            data.FindProperty("description").stringValue =
                "Rush to the selected enemy, then gain 20% movement speed for 3 seconds.";
            data.FindProperty("resourceCost").floatValue = 20f;
            data.FindProperty("cooldown").floatValue = 10f;
            data.FindProperty("windUp").floatValue = 0.4f;
            data.FindProperty("recovery").floatValue = 0.2f;
            data.FindProperty("useRange").floatValue = 8f;
            data.FindProperty("obstructionMask").intValue = 1;
            data.FindProperty("maximumUseAngle").floatValue = 45f;
            data.FindProperty("movementSpeedMultiplier").floatValue = 8f;
            data.FindProperty("arrivalDistance").floatValue = 1.25f;
            data.FindProperty("animatorTrigger").stringValue = string.Empty;
            SerializedProperty effect = data.FindProperty("selfEffect");
            effect.FindPropertyRelative("enabled").boolValue = true;
            effect.FindPropertyRelative("effectId").stringValue =
                "effect:charge-momentum";
            effect.FindPropertyRelative("statType").enumValueIndex =
                (int)StatType.MoveSpeed;
            effect.FindPropertyRelative("modifierType").enumValueIndex =
                (int)StatModifierType.Increased;
            effect.FindPropertyRelative("valuePerStack").floatValue = 20f;
            effect.FindPropertyRelative("maximumStacks").intValue = 1;
            effect.FindPropertyRelative("duration").floatValue = 3f;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(charge);
        }

        private static void EnsureCatalogContains(
            AbilityDefinitionCatalog catalog,
            TargetedMovementAbilityDefinition charge)
        {
            List<ScriptableObject> definitions =
                new(catalog.Definitions.Count + 1);
            bool found = false;
            for (int i = 0; i < catalog.Definitions.Count; i++)
            {
                ScriptableObject definition = catalog.Definitions[i];
                definitions.Add(definition);
                if (definition == charge)
                    found = true;
            }

            if (!found)
                definitions.Add(charge);

            catalog.ConfigureForEditor(definitions.ToArray());
            EditorUtility.SetDirty(catalog);
        }

        private static void ConfigureSchedule(
            RunAbilityUnlockScheduleDefinition schedule,
            TargetedMovementAbilityDefinition charge)
        {
            ScriptableObject[] starters =
            {
                RequireAsset<ScriptableObject>(HeavyStrikePath),
                RequireAsset<ScriptableObject>(CrushingStrikePath),
                RequireAsset<ScriptableObject>(CleavePath)
            };
            ScriptableObject whirlwind =
                RequireAsset<ScriptableObject>(WhirlwindPath);
            RunAbilityUnlockMilestoneDefinition[] milestones =
            {
                Milestone(3, 1, 2, starters, true),
                Milestone(
                    7,
                    2,
                    1,
                    new[] { whirlwind },
                    true),
                Milestone(
                    10,
                    3,
                    1,
                    new ScriptableObject[] { charge },
                    true),
                Milestone(
                    15,
                    4,
                    2,
                    Array.Empty<ScriptableObject>(),
                    false)
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

        private static void ConfigurePlayerEffectReceiver()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(
                PlayerPrefabPath);
            try
            {
                CharacterStats stats = root.GetComponent<CharacterStats>();
                if (stats == null)
                {
                    throw new InvalidOperationException(
                        "Player prefab is missing CharacterStats.");
                }

                TimedStackingStatEffectReceiver receiver =
                    root.GetComponent<TimedStackingStatEffectReceiver>();
                if (receiver == null)
                {
                    receiver = root.AddComponent<
                        TimedStackingStatEffectReceiver>();
                }

                SerializedObject data = new(receiver);
                data.FindProperty("characterStats").objectReferenceValue =
                    stats;
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(receiver);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ValidateInternal(
            TargetedMovementAbilityDefinition charge,
            AbilityDefinitionCatalog abilities,
            RunAbilityUnlockScheduleDefinition schedule)
        {
            if (!charge.TryCreateSnapshot(
                    out TargetedMovementAbilitySnapshot snapshot) ||
                charge.DisplayName != "Charge" ||
                snapshot.Execution.AbilityId != "ability:charge" ||
                snapshot.Execution.ResourceCost != 20f ||
                snapshot.Execution.Cooldown != 10d ||
                Math.Abs(snapshot.Execution.WindUp - 0.4d) > 0.000001d ||
                snapshot.UseRange != 8f ||
                snapshot.SpeedMultiplier != 8f ||
                snapshot.ArrivalDistance != 1.25f ||
                snapshot.AnimatorTrigger.Length != 0 ||
                snapshot.SelfEffect?.EffectId !=
                    "effect:charge-momentum" ||
                snapshot.SelfEffect.StatType != StatType.MoveSpeed ||
                snapshot.SelfEffect.ModifierType !=
                    StatModifierType.Increased ||
                snapshot.SelfEffect.ValuePerStack != 20f ||
                snapshot.SelfEffect.MaximumStacks != 1 ||
                snapshot.SelfEffect.Duration != 3d)
            {
                throw new InvalidOperationException(
                    "Charge does not match the approved prototype balance.");
            }

            if (!abilities.IsValid ||
                !abilities.TryResolve(
                    "ability:charge",
                    out IAbilityDefinition resolved) ||
                !ReferenceEquals(resolved, charge))
            {
                throw new InvalidOperationException(
                    "Charge is missing from the ability catalog.");
            }

            if (!schedule.TryCreateSchedule(
                    out RunAbilityUnlockSchedule runtime,
                    out string error) ||
                runtime.Milestones.Count != 3 ||
                runtime.Milestones[2].UnlockLevel != 10 ||
                runtime.Milestones[2].TargetSlotIndex != 3 ||
                runtime.Milestones[2].OptionCount != 1 ||
                runtime.Milestones[2].CandidateAbilityIds.Count != 1 ||
                runtime.Milestones[2].CandidateAbilityIds[0] !=
                    "ability:charge")
            {
                throw new InvalidOperationException(
                    $"Warrior Charge milestone is invalid: {error}");
            }

            GameObject root = PrefabUtility.LoadPrefabContents(
                PlayerPrefabPath);
            try
            {
                CharacterStats stats = root.GetComponent<CharacterStats>();
                TimedStackingStatEffectReceiver receiver =
                    root.GetComponent<TimedStackingStatEffectReceiver>();
                SerializedObject data = receiver != null
                    ? new SerializedObject(receiver)
                    : null;
                if (stats == null || receiver == null ||
                    data.FindProperty("characterStats")
                        .objectReferenceValue != stats)
                {
                    throw new InvalidOperationException(
                        "Player timed stat-effect receiver is not wired.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static T RequireAsset<T>(string path)
            where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException(
                    $"Asset is missing: {path}");
            }

            return asset;
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Exit Play Mode before Charge content wiring.");
            }
        }
    }
}
